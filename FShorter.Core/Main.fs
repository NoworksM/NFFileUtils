﻿module FShorter.Core.Main

open System.Collections.Generic
open System.IO
open System.Threading
open System.Threading.Channels
open Argu
open FSharp.Control
open FShorter.Core.Matchers
open Serilog
open Destructurama.FSharp
open FShorter.Core.AsyncExtensions
open FSharp.Control.AsyncSeqExtensions
open Serilog.Events

type PathInfo = { Path: string; IsDirectory: bool }

let sortFile debug (matchInfo: ImageInfo) =
    let root =
        Directory.GetParent(Path.GetDirectoryName(matchInfo.Path))

    let destinationDirectory = Path.Combine(root.FullName, matchInfo.Show)
    let destination = Path.Combine(destinationDirectory, Path.GetFileName(matchInfo.Path))

    Log.Information("Moving file \"{OldPath}\" to \"{NewPath}\"", matchInfo.Path, destination)
    
    if not <| Directory.Exists(destinationDirectory) then
        Log.Debug("Creating directory \"{Directory}\"", destinationDirectory)
        if not debug then
            Directory.CreateDirectory(destinationDirectory) |> ignore
    else
        Log.Verbose("Directory already exists")
    
    if not debug then
        File.Move(matchInfo.Path, destination)


let rec walkPaths(paths: AsyncSeq<string>) =
    asyncSeq {
        for path in paths do
            let pathAttributes = File.GetAttributes path
            
            if pathAttributes.HasFlag FileAttributes.Directory then
                Log.Verbose("Searching directory {Path}", path)
                yield! walkPaths <| AsyncSeq.ofSeq(Directory.GetFiles(path))
            else
                Log.Verbose("Found file {Path}", path)
                yield path
    }

let rec sortFiles debug (paths: AsyncSeq<string>) =
    let matcher = compositePathMatcher [|defaultSortedImageSorter; defaultFanSubEpisodeImageSorter; defaultFanSubMovieImageSorter|]
    
    let sf = sortFile debug
    
    let log (info: Result<ImageInfo, string>) =
        match info with
            | Ok(info) -> Log.Verbose("Found info for path {Path}", info.Path)
            | Error(path) -> Log.Error("No match was found for path {Path}", path)
        info

    walkPaths paths
    |> AsyncSeq.map matcher
    |> AsyncSeq.map log
    |> AsyncSeq.filter Result.isOk
    |> AsyncSeq.map Result.toOption
    |> AsyncSeq.map Option.get
    |> AsyncSeq.iter sf

let rec watchFiles (paths: seq<string>, cancellationToken: CancellationToken) =
    let directories = List<string>()
    let watchers = List<FileSystemWatcher>()
    
    asyncSeq {
        Log.Verbose("Processing initial paths")
        for path in paths do
            Log.Verbose("")
            let fileAttributes =
                File.GetAttributes(path)

            if fileAttributes.HasFlag FileAttributes.Directory then
                Log.Verbose("Searching directory {Path}", path)
                directories.Add path
                yield! walkPaths <| asyncSeq { path }
            else
                Log.Verbose("Found file {Path}", path)
                yield path
        
        let pathChannel = Channel.CreateUnbounded<string>()
        
        let listener (args: FileSystemEventArgs): Unit =
            pathChannel.Writer.WriteAsync(args.FullPath).AsTask() |> Async.AwaitTask |> Async.RunSynchronously 
            ()
        
        for directory in directories do
            let watcher = new FileSystemWatcher(directory)
            watcher.NotifyFilter <- NotifyFilters.LastWrite
            watcher.Created.Add listener
            watcher.Renamed.Add listener
            watchers.Add watcher
        
        while not cancellationToken.IsCancellationRequested do
            let! path = pathChannel.Reader.ReadAsync(cancellationToken).AsTask() |> Async.AwaitTask
            
            if not cancellationToken.IsCancellationRequested then
                Log.Debug("Found new path to sort {Path}", path)
                yield path
        
        Log.Verbose("Exit requested, cleaning up resources")
        for watcher in watchers do
            watcher.Dispose()
    }

let configureLogger isConsoleApplication logLevel =
    let configBuilder = LoggerConfiguration()
                            .Destructure.FSharpTypes()
                            .MinimumLevel.Is(logLevel)
    
    if isConsoleApplication then
        configBuilder.WriteTo.Console().CreateLogger()
    else
        configBuilder.WriteTo.File("", restrictedToMinimumLevel = logLevel, rollingInterval = RollingInterval.Day).CreateLogger()

type Arguments =
    | [<AltCommandLine("-w")>] Watch
    | [<AltCommandLine("-v")>] Verbose
    | [<AltCommandLine("-d")>] Debug
    | [<MainCommand; Last>] Paths of paths: string

    interface IArgParserTemplate with
        member s.Usage =
            match s with
            | Watch -> "watch files in directory"
            | Verbose -> "verbose logging"
            | Debug -> "run fshorter in debug mode (no files will be moved)"
            | Paths _ -> "paths to sort files in"

let mainAsync(args, name, isConsoleApplication) =
    let cts = new CancellationTokenSource()
    
    let parser =
        ArgumentParser.Create<Arguments>(programName = name)

    let argsV = Array.skip 1 args
    
    let parsedArgs =
        parser.ParseCommandLine argsV
    
    let logLevel = if parsedArgs.Contains Verbose then LogEventLevel.Verbose else LogEventLevel.Information
    
    Log.Logger <- configureLogger isConsoleApplication logLevel

    let paths = parsedArgs.GetResults Paths
    
    let debug = parsedArgs.Contains Debug

    async {
        if parsedArgs.Contains Watch then
            do! watchFiles(paths, cts.Token) |> sortFiles debug 
        else
            do! AsyncSeq.ofSeq paths |> walkPaths |> sortFiles debug
        
        do! Log.CloseAndFlushAsync().AsTask() |> Async.AwaitTask
    }