#load "Utils.fsx"
#load "Types.fsx"

open System
open Utils
open Types

type TV = {
    getProgram : Channel list -> Async<Map<Channel, Sendung seq>>
    nowPlaying : Channel list -> Async<Map<Channel, Sendung option>>
    nowPlayingWithMinEndTime: Channel list -> Async<DateTimeOffset * Map<Channel, Sendung option>>
}

let getProgram getTVData mapSendungen channels =
    channels
    |> List.map (fun channel -> async {
        let! tv = getTVData channel DateTimeOffset.Now
        let movies = mapSendungen tv
        return channel, movies
    })
    |> Async.Parallel
    |> Async.map Map.ofArray

let getMoviesOnTime date getTVData (mapSendungen: 'a -> seq<Sendung>) sender = async {
    let! tv = getTVData sender date
    return
        query {
            for m in mapSendungen tv do
            where (m.Beginn < date && m.Ende > date)
        }
        |> Seq.toList
}

let getMoviesNowPlaying () =
    getMoviesOnTime DateTimeOffset.Now

let nowPlaying getMoviesNowPlaying channels =
    channels
    |> List.map (fun channel -> async {
        let! movies = getMoviesNowPlaying () channel
        return channel, movies |> List.tryPick Some
    })
    |> Async.Parallel
    |> Async.map Map.ofArray

let getMinEndTime channels =
    channels
    |> Seq.choose (fun (KeyValue(_, m)) -> m)
    |> Seq.map (fun (m: Sendung) -> m.Ende)
    |> Seq.min

let nowPlayingWithMinEndTime getMoviewsNowPlaying channels = async {
    let! live = nowPlaying getMoviewsNowPlaying channels
    let minEndTime = getMinEndTime live
    return minEndTime, live
}
