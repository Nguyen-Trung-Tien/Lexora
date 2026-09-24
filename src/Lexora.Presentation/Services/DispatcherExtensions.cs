using Microsoft.UI.Dispatching;

namespace Lexora.Presentation.Services;

public static class DispatcherExtensions
{
    /// <summary>
    /// Ensures execution continues on the UI thread associated with the given DispatcherQueue.
    /// If the current thread is already the UI thread, this completes synchronously.
    /// </summary>
    public static Task EnsureOnUIThreadAsync(this DispatcherQueue? dispatcherQueue)
    {
        if (dispatcherQueue == null || dispatcherQueue.HasThreadAccess)
        {
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource();
        if (!dispatcherQueue.TryEnqueue(() => tcs.TrySetResult()))
        {
            tcs.TrySetCanceled();
        }
        return tcs.Task;
    }
}
