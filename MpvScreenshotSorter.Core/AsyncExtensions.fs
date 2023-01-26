module NFFileUtils.MpvScreenshotSorter.Core.AsyncExtensions

open System
open System.ComponentModel
open System.Runtime.CompilerServices
open System.Threading

/// <summary>
/// The awaiter for cancellation tokens.
/// </summary>
[<EditorBrowsable(EditorBrowsableState.Never)>]
type CancellationTokenAwaiter(cancellationToken: CancellationToken) =

    interface INotifyCompletion with
        member this.OnCompleted(continuation) =
            cancellationToken.Register(continuation) |> ignore

            ()

    interface ICriticalNotifyCompletion with
        member this.UnsafeOnCompleted(continuation) =
            cancellationToken.Register(continuation) |> ignore

    // called by compiler generated/.net internals to check
    // if the task has completed.
    member this.IsCompleted =
        cancellationToken.IsCancellationRequested

    // this is called by compiler generated methods when the
    // task has completed. Instead of returning a result, we
    // just throw an exception.
    member this.GetResult =
        if this.IsCompleted then
            raise (OperationCanceledException())
        else
            raise (InvalidOperationException("The cancellation token has not yet been cancelled."))
