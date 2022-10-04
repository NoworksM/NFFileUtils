namespace FShorter.Core

open System
open System.IO
open System.Text.RegularExpressions

module Matchers =
    type ImageInfo =
        { Path: string
          Show: string
          SubGroup: Option<string>
          Episode: Option<int> }

    type ImageSorter = string -> Result<ImageInfo, string>

    let createRegexImageSorter (regex: Regex, showGroup: int, subGroupGroup: Option<int>, episodeGroup: Option<int>) =
        fun (path: string) ->
            let filename = Path.GetFileName(path)
            
            let rMatch = regex.Match(filename)

            if rMatch.Success then
                Ok
                    { Path = path
                      Show = rMatch.Groups[showGroup].Value
                      SubGroup =
                        match subGroupGroup with
                        | Some subGroupGroup -> Some(rMatch.Groups[subGroupGroup].Value)
                        | None -> None
                      Episode =
                        match episodeGroup with
                        | Some episodeGroup -> Some(rMatch.Groups[episodeGroup].Value |> int)
                        | None -> None }
                
            else
                Error path

    let defaultFanSubEpisodeImageSorter: ImageSorter =
        createRegexImageSorter (
            Regex(
                @"^\[([^\]]+\])\s*(.*)\s+-\s+(\d+).*$",
                RegexOptions.Compiled
                ||| RegexOptions.Multiline
                ||| RegexOptions.IgnoreCase
            ),
            2,
            Some(1),
            Some(3)
        )

    let defaultFanSubMovieImageSorter: ImageSorter =
        createRegexImageSorter (
            Regex(
                @"^\[([^\]]+\])\s*(.*)\s*\[[a-f0-9]{8}\].*$",
                RegexOptions.Compiled
                ||| RegexOptions.Multiline
                ||| RegexOptions.IgnoreCase
            ),
            2,
            Some(1),
            None
        )

    let defaultSortedImageSorter: ImageSorter =
        createRegexImageSorter (
            Regex(
                @"^(.*)\s-\s(\d+)\s-\s([^\[]+)\s+\[([^\]]+)\]\[(\d+)x(\d+)\]\[([^\]]+)\]\[([a-f0-9]{8})\].*$",
                RegexOptions.Compiled
                ||| RegexOptions.Multiline
                ||| RegexOptions.IgnoreCase
            ),
            1,
            Some(3),
            Some(2)
        )

    let compositePathMatcher matchers =
        fun path ->
            match matchers |> Array.tryPick (fun x -> x path |> Result.toOption) with
            | Some(y) -> Ok y
            | None -> Error path
