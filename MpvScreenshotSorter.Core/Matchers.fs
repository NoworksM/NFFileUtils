namespace FShorter.Core

open System
open System.IO
open System.Text.RegularExpressions

module Matchers =
    type ImageInfo =
        { Path: string
          Show: string
          SubGroup: Option<string>
          EpisodeNumber: Option<int>
          EpisodeVersion: Option<int>
          EpisodeTitle: Option<int> }

    type ImageSorter = string -> Result<ImageInfo, string>

    type RegexSorterParams =
        { Regex: Regex
          ShowGroup: int
          SubGroupGroup: int option
          EpisodeNumberGroup: int option
          EpisodeVersionGroup: int option
          EpisodeTitleGroup: int option }

    let createRegexImageSorter regexSorterParams =
        fun (path: string) ->
            let filename = Path.GetFileName(path)

            let rMatch =
                regexSorterParams.Regex.Match(filename)

            let optionalMatcher (param: int option) =
                match param with
                | Some g -> Some(rMatch.Groups[g].Value)
                | None -> None

            let tryParse (value: string option) =
                match value with
                | Some value ->
                    match Int32.TryParse value with
                    | true, int -> Some int
                    | _ -> None
                | None -> None

            let intMatcher = optionalMatcher >> tryParse

            if rMatch.Success then
                Ok
                    { Path = path
                      Show = rMatch.Groups[regexSorterParams.ShowGroup].Value
                      SubGroup = optionalMatcher regexSorterParams.SubGroupGroup
                      EpisodeNumber = intMatcher regexSorterParams.EpisodeNumberGroup
                      EpisodeVersion = intMatcher regexSorterParams.EpisodeVersionGroup
                      EpisodeTitle = intMatcher regexSorterParams.EpisodeTitleGroup }

            else
                Error path

    let defaultFanSubEpisodeImageSorter: ImageSorter =
        createRegexImageSorter
            { Regex =
                Regex(
                    @"^\[([^\]]+\])\s*(.*)\s+-\s+(\d+)(v(\d))?.*$",
                    RegexOptions.Compiled
                    ||| RegexOptions.Multiline
                    ||| RegexOptions.IgnoreCase
                )
              ShowGroup = 2
              SubGroupGroup = Some(1)
              EpisodeNumberGroup = Some(3)
              EpisodeVersionGroup = Some(5)
              EpisodeTitleGroup = None }

    let defaultFanSubMovieImageSorter: ImageSorter =
        createRegexImageSorter
            { Regex =
                Regex(
                    @"^\[([^\]]+\])\s*(.*)\s*\[[a-f0-9]{8}\].*$",
                    RegexOptions.Compiled
                    ||| RegexOptions.Multiline
                    ||| RegexOptions.IgnoreCase
                )
              ShowGroup = 2
              SubGroupGroup = Some(1)
              EpisodeNumberGroup = None
              EpisodeVersionGroup = None
              EpisodeTitleGroup = None }

    let defaultSortedEpisodeSorter: ImageSorter =
        createRegexImageSorter
            { Regex =
                Regex(
                    @"^(.*)\s-\s(\d+)(v(\d))?\s-\s([^\[]+)\s+\[([^\]]+)\]\[(\d+)x(\d+)\]\[([^\]]+)\]\[([a-f0-9]{8})\].*$",
                    RegexOptions.Compiled
                    ||| RegexOptions.Multiline
                    ||| RegexOptions.IgnoreCase
                )
              ShowGroup = 1
              SubGroupGroup = Some(6)
              EpisodeNumberGroup = Some(2)
              EpisodeVersionGroup = Some(4)
              EpisodeTitleGroup = None }

    let defaultSortedMovieSorter: ImageSorter =
        createRegexImageSorter
            { Regex =
                Regex(
                    @"^(.*)\s-\s(.*)\s*\[([^\]]*)\]\[([^\]]*)\]\[([^\]]*)\]\[([^\]]*)\]\[([a-f0-9]{8})\].*$",
                    RegexOptions.Compiled
                    ||| RegexOptions.Multiline
                    ||| RegexOptions.IgnoreCase
                )
              ShowGroup = 1
              SubGroupGroup = Some(3)
              EpisodeNumberGroup = None
              EpisodeVersionGroup = None
              EpisodeTitleGroup = Some(2) }

    let compositePathMatcher matchers =
        fun path ->
            match matchers
                  |> Array.tryPick (fun x -> x path |> Result.toOption)
                with
            | Some (y) -> Ok y
            | None -> Error path
