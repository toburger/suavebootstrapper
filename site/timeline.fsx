#r @"../packages/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"
#r @"../packages/Google.DataTable.Net.Wrapper/lib/Google.DataTable.Net.Wrapper.dll"
#r @"../packages/XPlot.GoogleCharts/lib/net45/XPlot.GoogleCharts.dll"
#r @"../packages/XPlot.GoogleCharts.WPF/lib/net45/XPlot.GoogleCharts.WPF.dll"

open System
open XPlot.GoogleCharts

#load "Utils.fsx"
#load "Types.fsx"
#load "TV.fsx"
#load "TV.fsx"
#load "TV.Spaetfruehstuecken.fsx"

open Types
open TV.Spaetfruehstuecken
module TV = TV.Spaetfruehstuecken

let labels = [ "Title"; "Begin"; "End" ]
let toTimeline channel (movie: Sendung) =
    string channel, movie.Titel, movie.Beginn.LocalDateTime, movie.Ende.LocalDateTime

let channels = TV.nowPlaying (TV.channelsOfInterest) |> Async.RunSynchronously
channels
|> Seq.choose (fun (KeyValue(channel, movie)) -> movie |> Option.map (fun movie -> toTimeline channel movie))
|> Chart.Timeline
|> Chart.WithLabels labels
|> Chart.Show

let channelsOfInterest = TV.getProgram (TV.channelsOfInterest) |> Async.RunSynchronously
channelsOfInterest
|> Seq.collect (fun (KeyValue(channel, movies))-> movies |> Seq.map (toTimeline channel))
|> Chart.Timeline
|> Chart.WithLabels labels
|> Chart.Show
