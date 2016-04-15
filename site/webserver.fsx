// --------------------------------------------------------------------------------------
// Start up Suave.io
// --------------------------------------------------------------------------------------

#r "../packages/FAKE/tools/FakeLib.dll"
#r "../packages/Suave/lib/net40/Suave.dll"
#r @"../packages/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"
#r @"../packages/FifteenBelow.Json/lib/net40/FifteenBelow.Json.dll"

#load "Utils.fsx"
#load "Types.fsx"
#load "TV.fsx"
#load "TV.Merged.fsx"
#load "Caching.fsx"

open System
open Fake
open Suave
open Suave.Operators
open Suave.Successful
open Suave.RequestErrors
open Suave.Filters
open Suave.Files
open System.Net
open Newtonsoft.Json
open Newtonsoft.Json.Converters
open Newtonsoft.Json.Serialization
open FifteenBelow.Json

open Types
open Utils

let (@@) a b = System.IO.Path.Combine(a, b)

let serverConfig = 
    let port = getBuildParamOrDefault "port" "8083" |> Sockets.Port.Parse
    { defaultConfig with
        homeFolder = Some(__SOURCE_DIRECTORY__ @@ "public")
        bindings = [ HttpBinding.mk HTTP IPAddress.Loopback port ] }

let xmlMime = Writers.setMimeType "application/xml; charset=utf-8"
let jsonMime = Writers.setMimeType "application/json; charset=utf-8"

let allowCors: WebPart =
    OPTIONS >=>
        Writers.setHeader "Access-Control-Allow-Origin" "*"
        >=> OK "CORS approved"

let jsonSerialize =
    let converters : JsonConverter array =
        [| OptionConverter()
           TupleConverter()
           ListConverter()
           MapConverter()
           BoxedMapConverter()
           UnionConverter() |]
    let settings =
        JsonSerializerSettings(
            ContractResolver = CamelCasePropertyNamesContractResolver(),
            Converters = converters,
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore)
    fun (obj: obj) -> JsonConvert.SerializeObject(obj, settings)

let jsonOK obj =
    jsonMime >=> OK (jsonSerialize obj)

let asyncResponse response workflow =
    fun ctx -> async {
        let! r = workflow
        return! response r ctx
    }

let programCached' channels =
    Caching.MemoryCache.asyncGetOrUpdate
        ("program", channels)
        (TV.Merged.tv.getProgram channels
         |> Async.map (fun channels ->
            DateTimeOffset.Now.AddHours(3.), channels))

type ChannelJ =
    { logoUrl: string option
      sendung: Sendung option }

type M = Map<Channel, seq<Sendung>> -> Map<string, ChannelJ>

let jsonFriendly =
    Map.map (fun (channel: Channel) sendung ->
        { logoUrl = channel.logoUrl
          sendung = sendung })

let programCached channels =
    programCached' channels |> asyncResponse jsonOK

let nowPlayingCached channels =
    Caching.MemoryCache.asyncGetOrUpdate
        ("live", channels)
        (TV.Merged.tv.nowPlayingWithMinEndTime channels)
        |> Async.map jsonFriendly
        |> asyncResponse jsonOK

let extractChannels channels pmap =
    channels
    |> List.fold (fun m c ->
        m |> Map.add c (pmap |> Map.find c)) Map.empty

let program' channels = async {
    let! p = programCached' channels
    return extractChannels channels p
}

let program channels =
    asyncResponse jsonOK <| program' channels

let nowPlaying channels =
    asyncResponse jsonOK <| program' channels

// let channels =
//     asyncResponse jsonOK <| TV.getChannels

let app =
    choose [
        allowCors
        GET >=>
            choose [
                path "/" >=> browseFileHome "index.html"
                // path "/channels" >=> channels
                path "/program" >=> warbler (fun _ -> programCached [])
                path "/live" >=> warbler (fun _ -> nowPlayingCached [])
                pathScan "/program/%s" (fun channel -> program [ { name = channel; logoUrl = None } ])
                pathScan "/live/%s" (fun channel -> nowPlaying [ { name = channel; logoUrl = None } ])
                pathRegex "(.*)\.(js)" >=> browseHome
            ]
        NOT_FOUND "not found"
    ]

startWebServer serverConfig app
