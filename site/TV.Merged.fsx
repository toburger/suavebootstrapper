#load "Utils.fsx"
#load "Types.fsx"
#load "TV.fsx"
#load "TV.Spaetfruehstuecken.fsx"
#load "TV.Pro.fsx"

open Utils

let map =
    [ TV.Spaetfruehstuecken.channelsOfInterest, TV.Spaetfruehstuecken.tv
      TV.Pro.channelsOfInterest, TV.Pro.tv ]

module Map =
    let concat maps =
        maps
        |> Array.map Map.toSeq
        |> Seq.concat
        |> Map.ofSeq

let apply e map =
    map
    |> Seq.map (fun (c, f) -> (e f) c)
    |> Async.Parallel
    |> Async.map Map.concat

let getProgram _ =
    map |> apply (fun p -> p.getProgram)

let nowPlaying _ =
    map |> apply (fun p -> p.nowPlaying)

let nowPlayingWithMinEndTime _ = async {
    let! live = nowPlaying ()
    let minEndTime = getMinEndTime live
    return minEndTime, live
}

let tv =
    { getProgram = getProgram
      nowPlaying = nowPlaying
      nowPlayingWithMinEndTime = nowPlayingWithMinEndTime }


