#r "System.Xml.Linq"
#r "System.Net.Http"
#r "System.IO.Compression"
#r "../packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#load "Utils.fsx"
#load "Types.fsx"
#load "TV.fsx"

open System
open System.Net.Http
open System.IO
open System.IO.Compression
open System.Xml
open System.Xml.Linq
open FSharp.Data
open Utils
open Types

module TV =

    let [<Literal>] tvsample = __SOURCE_DIRECTORY__ + "/samples/sample.xml"

    type TVProgramm = XmlProvider<tvsample>

    let parseFragment (stream: Stream) =
        use reader =
            XmlReader.Create(
                stream,
                XmlReaderSettings(ConformanceLevel = ConformanceLevel.Fragment)
            )
        let rec read eof = [
            if not eof then
                let node = XNode.ReadFrom(reader)
                if node <> null then
                    yield node
                    yield! read reader.EOF
        ]
        do reader.MoveToContent() |> ignore
        read reader.EOF

    let getZipStream url = async {
        use client = new HttpClient()
        use! stream = client.GetStreamAsync(url: string) |> Async.AwaitTask
        let archive = new ZipArchive(stream, ZipArchiveMode.Read)
        let entry = archive.Entries |> Seq.head
        return entry.Open()
    }

    let getFragments fragments = async {
        let xn = XName.Get

        let el = XElement(xn "root", Seq.ofList fragments)

        let mem = new System.IO.MemoryStream()
        el.Save(mem)
        mem.Position <- 0L

        return TVProgramm.Load(mem)
    }

    let (|FormatDate|) =
        let fmtdate (date: DateTimeOffset) =
            sprintf "%04i-%d" date.Year date.DayOfYear
        fun date -> fmtdate date

    let url (FormatDate date) ({ Channel.name = channel }) =
        sprintf "http://tvpro.blob.core.windows.net/tvprogramm/%s%s.zip"
                date
                channel

    let program url =
        getZipStream url
        <!> parseFragment
        >>= getFragments

    let date (s: string) =
        match DateTimeOffset
            .TryParseExact(s,
                           "dd.MM.yyyy HH:mm:ss",
                           Globalization.CultureInfo.InvariantCulture,
                           Globalization.DateTimeStyles.None) with
        | true, d -> d
        | false, _ -> failwithf "invalid date: %s" s

    let mapSendung (s: TVProgramm.S0) =
        { Titel = s.S1
          Untertitel = if s.S1 = s.S2 then None else Some s.S2
          Beschreibung = s.S3
          Beginn = date s.S
          Ende = date s.E
          Url = None }

    let mapSendungen (tv: TVProgramm.Root) =
        Seq.map mapSendung tv.S0s

    let getTVData channel date =
        url date channel |> program

let channel name = { Channel.name = name; logoUrl = None }

let channelsOfInterest =
    [ "SF 1"; "SF 2" ]
    |> List.map channel

let getProgram = TV.getProgram TV.getTVData TV.mapSendungen

let getMoviesNowPlaying () = TV.getMoviesNowPlaying () TV.getTVData TV.mapSendungen

let nowPlaying = TV.nowPlaying getMoviesNowPlaying

let nowPlayingWithMinEndTime = TV.nowPlayingWithMinEndTime getMoviesNowPlaying

let tv =
    { getProgram = getProgram
      nowPlaying = nowPlaying
      nowPlayingWithMinEndTime = nowPlayingWithMinEndTime }
