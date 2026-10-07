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
        var dq = dispatcherQueue 
            ?? App.Current?.UIDispatcherQueue 
            ?? App.Current?.MainWindow?.DispatcherQueue;

        if (dq == null || dq.HasThreadAccess)
        {
            if (dq != null && SynchronizationContext.Current == null)
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(dq));
            }
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource();
        if (!dq.TryEnqueue(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(dq));
            tcs.TrySetResult();
        }))
        {
            tcs.TrySetCanceled();
        }
        return tcs.Task;
    }
}
