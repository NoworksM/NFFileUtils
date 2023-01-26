open System
open NFFileUtils.MpvScreenshotSorter.Core.Main

async {
    do! Async.SwitchToThreadPool()
    return! mainAsync(Environment.GetCommandLineArgs(), "mpvshotsort", true)
} |> Async.RunSynchronously