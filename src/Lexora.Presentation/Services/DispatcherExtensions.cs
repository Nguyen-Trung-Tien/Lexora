using Microsoft.UI.Dispatching;

namespace Lexora.Presentation.Services;

public static class DispatcherExtensions
{
    /// <summary>
    /// Ensures execution continues on the UI thread associated with the given DispatcherQueue.
    /// If the current thread is already the UI thread, this completes synchronously without dispatching.
    /// Otherwise, enqueues the continuation directly onto the DispatcherQueue message loop.
    /// </summary>
    public static DispatcherQueueAwaitable EnsureOnUIThreadAsync(this DispatcherQueue? dispatcherQueue)
    {
        var dq = dispatcherQueue 
            ?? App.Current?.UIDispatcherQueue 
            ?? App.Current?.MainWindow?.DispatcherQueue;

        return new DispatcherQueueAwaitable(dq);
    }
}

/// <summary>
/// Awaitable class that transfers async continuation directly into the target DispatcherQueue,
/// avoiding TaskScheduler.Default ThreadPool rescheduling.
/// </summary>
public sealed class DispatcherQueueAwaitable : System.Runtime.CompilerServices.ICriticalNotifyCompletion
{
    private readonly DispatcherQueue? _dispatcherQueue;

    public DispatcherQueueAwaitable(DispatcherQueue? dispatcherQueue)
    {
        _dispatcherQueue = dispatcherQueue;
    }

    public DispatcherQueueAwaitable GetAwaiter() => this;

    public bool IsCompleted
    {
        get
        {
            if (_dispatcherQueue == null) return true;
            if (_dispatcherQueue.HasThreadAccess)
            {
                if (SynchronizationContext.Current == null)
                {
                    SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(_dispatcherQueue));
                }
                return true;
            }
            return false;
        }
    }

    public void OnCompleted(Action continuation) => UnsafeOnCompleted(continuation);

    public void UnsafeOnCompleted(Action continuation)
    {
        if (_dispatcherQueue == null)
        {
            continuation();
            return;
        }

        if (!_dispatcherQueue.TryEnqueue(() =>
        {
            if (SynchronizationContext.Current == null)
            {
                SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(_dispatcherQueue));
            }
            continuation();
        }))
        {
            continuation();
        }
    }

    public void GetResult() { }

    public Task AsTask()
    {
        if (IsCompleted) return Task.CompletedTask;
        var tcs = new TaskCompletionSource();
        UnsafeOnCompleted(() => tcs.TrySetResult());
        return tcs.Task;
    }

    public static implicit operator Task(DispatcherQueueAwaitable awaitable) => awaitable.AsTask();
}
