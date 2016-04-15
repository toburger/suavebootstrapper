#r "System.Xml.Linq"
#r "../packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#load "Utils.fsx"
#load "Types.fsx"
#load "TV.fsx"

open System
open System.Globalization
open FSharp.Data
open Utils
open Types

module TV =
    let host = "xmltv.spaetfruehstuecken.org"
    let file sender (date: DateTimeOffset) = sprintf "%s_%s" sender (date.ToString("yyyy-MM-dd"))
    let request date sender = sprintf "http://%s/xmltv/%s.xml.gz" host (file sender date)
    let htmlUrl = sprintf "http://%s/xmltv_compare/00output.html" host
    let logoUrl = sprintf "http://%s/chanlogos/44x44/%s.png" host

    let [<Literal>] xmlsample = __SOURCE_DIRECTORY__ + "/samples/tvprogramm.xml"
    type MovieData = XmlProvider<Sample = xmlsample,
                                 Encoding = "UTF-8", InferTypesFromValues = true >

    let [<Literal>] htmlsample = __SOURCE_DIRECTORY__ + "/samples/00output.html"
    type Channels = HtmlProvider<Sample = htmlsample>

    let date s =
        match DateTimeOffset.TryParseExact(s, "yyyyMMddHHmmss zzz", CultureInfo.InvariantCulture, DateTimeStyles.None) with
        | true, d -> d
        | false, _ -> failwithf "invalid date: %s" s

    let tryUrl s =
        match Uri.TryCreate(s, UriKind.Absolute) with
        | true, uri -> Some uri
        | false, _ -> None

    let mapSendung (p: MovieData.Programme) =
        { Titel = p.Title.Value
          Untertitel = p.SubTitle |> Option.map (fun st -> st.Value)
          Beschreibung = p.Desc |> Option.map (fun d -> d.Value)
          Beginn = date p.Start
          Ende = date p.Stop
          Url = p.Url |> Option.bind tryUrl }

    let mapSendungen (programmes: MovieData.Programme seq) =
        Seq.map mapSendung programmes

    let getTVData ({ Channel.name = name}) date = async {
        try
            let! data = MovieData.AsyncLoad(request date name)
            return data.Programmes :> seq<_>
        with _ -> return Seq.empty
    }

    let getChannels () = async {
        let! html = Channels.AsyncLoad(htmlUrl)
        let rows = html.Tables.Changes.Rows
        return rows |> Array.map (fun row -> row.Channel)
    }

let getProgram = TV.getProgram TV.getTVData TV.mapSendungen

let getChannels () =
    TV.getChannels ()
    |> Async.map List.ofArray

let channel s = { Channel.name = s; logoUrl = Some (TV.logoUrl s) }

let channelsOfInterest =
    [ "hd.daserste.de"
      "hd.zdf.de"
      "hd.orf1.orf.at"
      "hd.orf2.orf.at"
      "orf3.orf.at"
      "hd.3sat.de"
      "hd.arte.de" ]
    |> List.map channel

let getMoviesNowPlaying () = TV.getMoviesNowPlaying () TV.getTVData TV.mapSendungen

let nowPlaying = TV.nowPlaying getMoviesNowPlaying

let nowPlayingWithMinEndTime = TV.nowPlayingWithMinEndTime getMoviesNowPlaying

//TV.getTVData "sr.swr.de" (DateTimeOffset.Now)
//|> Async.RunSynchronously
//|> Array.map (fun p -> p.Title.Value)

//getChannels()
//|> Async.bind nowPlaying
//|> Async.RunSynchronously
//|> Seq.choose (fun p -> p.Value |> Option.bind (fun v -> Some (p.Key, v)))
//|> Seq.iter (fun (c, p) -> printfn "%-26s: %s" c p.Titel)

let tv =
    { getProgram = getProgram
      nowPlaying = nowPlaying
      nowPlayingWithMinEndTime = nowPlayingWithMinEndTime }
