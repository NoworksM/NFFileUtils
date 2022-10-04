open System
open FShorter.Core.Main

async {
    do! Async.SwitchToThreadPool()
    return! mainAsync(Environment.GetCommandLineArgs(), "fshorter", true)
} |> Async.RunSynchronously