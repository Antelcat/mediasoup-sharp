﻿using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Antelcat.MediasoupSharp.Internals.Extensions;

/// <summary>
/// Task 扩展方法
/// </summary>
internal static class TaskExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // 造成编译器优化调用
    public static void NoWarning(this Task _)
    {
    }

    public static void ContinueWithOnFaultedLog(this Task task, ILogger logger)
    {
        _ = task.ContinueWith(val =>
        {
            // we need to access val.Exception property otherwise unobserved exception will be thrown
            // ReSharper disable once PossibleNullReferenceException
            foreach(var ex in val.Exception!.Flatten().InnerExceptions)
            {
                logger.LogError(ex, "Task exception");
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    public static void ContinueWithOnFaultedHandleLog(this Task task, ILogger logger)
    {
        _ = task.ContinueWith(val =>
        {
            val.Exception!.Handle(ex =>
            {
                logger.LogError(ex, "Task exception");
                return true;
            });
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    /// <summary>
    /// Returns a task that completes as the original task completes or when a timeout expires,
    /// whichever happens first.
    /// </summary>
    /// <param name="task">The task to wait for.</param>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <param name="cancelled"></param>
    /// <returns>
    /// A task that completes with the result of the specified <paramref name="task"/> or
    /// faults with a <see cref="TimeoutException"/> if <paramref name="timeout"/> elapses first.
    /// </returns>
    public static async Task WithTimeoutAsync(this Task task, TimeSpan timeout, Action? cancelled = null)
    {
        using var timerCancellation  = new CancellationTokenSource();
        var       timeoutTask        = Task.Delay(timeout, timerCancellation.Token);
        var       firstCompletedTask = await Task.WhenAny(task, timeoutTask).ConfigureAwait(false);
        if(firstCompletedTask == timeoutTask)
        {
            if(cancelled == null)
            {
                throw new TimeoutException();
            }

            cancelled();
        }

        // The timeout did not elapse, so cancel the timer to recover system resources.
        timerCancellation.Cancel();

        // re-throw any exceptions from the completed task.
        await task.ConfigureAwait(false);
    }

    /// <summary>
    /// Returns a task that completes as the original task completes or when a timeout expires,
    /// whichever happens first.
    /// </summary>
    /// <typeparam name="T">The type of value returned by the original task.</typeparam>
    /// <param name="task">The task to wait for.</param>
    /// <param name="timeout">The maximum time to wait.</param>
    /// <param name="cancelled"></param>
    /// <returns>
    /// A task that completes with the result of the specified <paramref name="task"/> or
    /// faults with a <see cref="TimeoutException"/> if <paramref name="timeout"/> elapses first.
    /// </returns>
    public static async Task<T> WithTimeoutAsync<T>(this Task<T> task, TimeSpan timeout, Action cancelled = null)
    {
        if (cancelled == null) throw new ArgumentNullException(nameof(cancelled));
        await WithTimeoutAsync((Task)task, timeout, cancelled).ConfigureAwait(false);
        return task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Creates a new Task that mirrors the supplied task but that will be canceled after the specified timeout.
    /// </summary>
    /// <typeparam name="TResult">Specifies the type of data contained in the task.</typeparam>
    /// <param name="task">The task.</param>
    /// <param name="timeout">The timeout.</param>
    /// <returns>The new Task that may time out.</returns>
    public static Task<TResult> WithTimeoutAsync<TResult>(this Task<TResult> task, TimeSpan timeout)
    {
        var result = new TaskCompletionSource<TResult>(task.AsyncState);
        var timer = new Timer(state => ((TaskCompletionSource<TResult>)state!).TrySetCanceled(), result, timeout, TimeSpan.FromMilliseconds(-1));
        _ = task.ContinueWith(t =>
        {
            timer.Dispose();
            result.TrySetFromTask(t);
        }, TaskContinuationOptions.ExecuteSynchronously);
        return result.Task;
    }

    /// <summary>
    /// Attempts to transfer the result of a Task to the TaskCompletionSource.
    /// </summary>
    /// <typeparam name="TResult">Specifies the type of the result.</typeparam>
    /// <param name="resultSetter">The TaskCompletionSource.</param>
    /// <param name="task">The task whose completion results should be transfered.</param>
    /// <returns>Whether the transfer could be completed.</returns>
    public static bool TrySetFromTask<TResult>(this TaskCompletionSource<TResult> resultSetter, Task task)
    {
        switch(task.Status)
        {
            case TaskStatus.RanToCompletion:
#pragma warning disable CS8604 // Possible null reference argument.
                return resultSetter.TrySetResult(task is Task<TResult> taskLocal ? taskLocal.Result : default);
#pragma warning restore CS8604 // Possible null reference argument.
            case TaskStatus.Faulted:
                return resultSetter.TrySetException(task.Exception!.InnerExceptions);
            case TaskStatus.Canceled:
                return resultSetter.TrySetCanceled();
            case TaskStatus.Created:
                break;
            case TaskStatus.WaitingForActivation:
                break;
            case TaskStatus.WaitingToRun:
                break;
            case TaskStatus.Running:
                break;
            case TaskStatus.WaitingForChildrenToComplete:
                break;
        }

        throw new InvalidOperationException("The task was not completed.");
    }

    /// <summary>
    /// Attempts to transfer the result of a Task to the TaskCompletionSource.
    /// </summary>
    /// <typeparam name="TResult">Specifies the type of the result.</typeparam>
    /// <param name="resultSetter">The TaskCompletionSource.</param>
    /// <param name="task">The task whose completion results should be transferred.</param>
    /// <returns>Whether the transfer could be completed.</returns>
    public static bool TrySetFromTask<TResult>(this TaskCompletionSource<TResult> resultSetter, Task<TResult> task)
    {
        return TrySetFromTask(resultSetter, (Task)task);
    }

    public static Task<T> FromResultAsync<T>(this T value)
    {
        var tcs = new TaskCompletionSource<T>();
        tcs.SetResult(value);
        return tcs.Task;
    }

    public static TaskCompletionSource<TResult> WithTimeout<TResult>(this TaskCompletionSource<TResult> taskCompletionSource, TimeSpan timeout, Action? cancelled = null)
    {
        Timer? timer = null;
        timer = new Timer(_ =>
        {
            timer?.Dispose();
            if (taskCompletionSource.Task.Status == TaskStatus.RanToCompletion) return;
            taskCompletionSource.TrySetCanceled();
            cancelled?.Invoke();
        }, null, timeout, TimeSpan.FromMilliseconds(-1));

        return taskCompletionSource;
    }

    /// <summary>
    /// Set the TaskCompletionSource in an async fashion. This prevents the Task Continuation being executed sync on the same thread
    /// This is required otherwise contintinuations will happen on CEF UI threads
    /// </summary>
    /// <typeparam name="TResult">Generic param</typeparam>
    /// <param name="taskCompletionSource">tcs</param>
    /// <param name="result">result</param>
    public static void TrySetResult<TResult>(this TaskCompletionSource<TResult> taskCompletionSource, TResult result)
    {
        _ = Task.Factory.StartNew(() => taskCompletionSource.TrySetResult(result), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
    }

    #region Synchronously await

    /// <summary>
    /// Synchronously await the results of an asynchronous operation without deadlocking; ignoring cancellation.
    /// </summary>
    /// <param name="task">
    /// The <see cref="Task"/> representing the pending operation.
    /// </param>
    public static void AwaitCompletion(this ValueTask task)
    {
        new SynchronousAwaiter(task, true).GetResult();
    }

    /// <summary>
    /// Synchronously await the results of an asynchronous operation without deadlocking; ignoring cancellation.
    /// </summary>
    /// <param name="task">
    /// The <see cref="Task"/> representing the pending operation.
    /// </param>
    public static void AwaitCompletion(this Task task)
    {
        new SynchronousAwaiter(task, true).GetResult();
    }

    /// <summary>
    /// Synchronously await the results of an asynchronous operation without deadlocking.
    /// </summary>
    /// <param name="task">
    /// The <see cref="Task"/> representing the pending operation.
    /// </param>
    /// <typeparam name="T">
    /// The result type of the operation.
    /// </typeparam>
    /// <returns>
    /// The result of the operation.
    /// </returns>
    public static T? AwaitResult<T>(this Task<T> task)
    {
        return new SynchronousAwaiter<T>(task).GetResult();
    }

    /// <summary>
    /// Synchronously await the results of an asynchronous operation without deadlocking.
    /// </summary>
    /// <param name="task">
    /// The <see cref="Task"/> representing the pending operation.
    /// </param>
    public static void AwaitResult(this Task task)
    {
        new SynchronousAwaiter(task).GetResult();
    }

    /// <summary>
    /// Synchronously await the results of an asynchronous operation without deadlocking.
    /// </summary>
    /// <param name="task">
    /// The <see cref="ValueTask"/> representing the pending operation.
    /// </param>
    /// <typeparam name="T">
    /// The result type of the operation.
    /// </typeparam>
    /// <returns>
    /// The result of the operation.
    /// </returns>
    public static T? AwaitResult<T>(this ValueTask<T> task)
    {
        return new SynchronousAwaiter<T>(task).GetResult();
    }

    /// <summary>
    /// Synchronously await the results of an asynchronous operation without deadlocking.
    /// </summary>
    /// <param name="task">
    /// The <see cref="ValueTask"/> representing the pending operation.
    /// </param>
    public static void AwaitResult(this ValueTask task)
    {
        new SynchronousAwaiter(task).GetResult();
    }

    /// <summary>
    /// Ignore the <see cref="OperationCanceledException"/> if the operation is cancelled.
    /// </summary>
    /// <param name="task">
    /// The <see cref="Task"/> representing the asynchronous operation whose cancellation is to be ignored.
    /// </param>
    /// <returns>
    /// The <see cref="Task"/> representing the asynchronous operation whose cancellation is ignored.
    /// </returns>
    public static async Task IgnoreCancellationResultAsync(this Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch(OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// Ignore the <see cref="OperationCanceledException"/> if the operation is cancelled.
    /// </summary>
    /// <param name="task">
    /// The <see cref="ValueTask"/> representing the asynchronous operation whose cancellation is to be ignored.
    /// </param>
    /// <returns>
    /// The <see cref="ValueTask"/> representing the asynchronous operation whose cancellation is ignored.
    /// </returns>
    public static async ValueTask IgnoreCancellationResultAsync(this ValueTask task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch(OperationCanceledException)
        {
        }
    }

    /// <summary>
    /// Ignore the results of an asynchronous operation allowing it to run and die silently in the background.
    /// </summary>
    /// <param name="task">
    /// The <see cref="Task"/> representing the asynchronous operation whose results are to be ignored.
    /// </param>
    public static async void IgnoreResult(this Task task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch
        {
            // ignore exceptions
        }
    }

    /// <summary>
    /// Ignore the results of an asynchronous operation allowing it to run and die silently in the background.
    /// </summary>
    /// <param name="task">
    /// The <see cref="ValueTask"/> representing the asynchronous operation whose results are to be ignored.
    /// </param>
    public static async void IgnoreResult(this ValueTask task)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch
        {
            // ignore exceptions
        }
    }

    #endregion
}

/// <summary>
/// Internal class for waiting for asynchronous operations that have a result.
/// </summary>
/// <typeparam name="TResult">
/// The result type.
/// </typeparam>
internal class SynchronousAwaiter<TResult>
{
    /// <summary>
    /// The manual reset event signaling completion.
    /// </summary>
    private readonly ManualResetEvent manualResetEvent;

    /// <summary>
    /// The exception thrown by the asynchronous operation.
    /// </summary>
    private Exception? exception;

    /// <summary>
    /// The result of the asynchronous operation.
    /// </summary>
    private TResult? result;

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronousAwaiter{TResult}"/> class.
    /// </summary>
    /// <param name="task">
    /// The task representing an asynchronous operation.
    /// </param>
    public SynchronousAwaiter(Task<TResult> task)
    {
        manualResetEvent = new ManualResetEvent(false);
        WaitFor(task);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronousAwaiter{TResult}"/> class.
    /// </summary>
    /// <param name="task">
    /// The task representing an asynchronous operation.
    /// </param>
    public SynchronousAwaiter(ValueTask<TResult> task)
    {
        manualResetEvent = new ManualResetEvent(false);
        WaitFor(task);
    }

    /// <summary>
    /// Gets a value indicating whether the operation is complete.
    /// </summary>
    public bool IsComplete => manualResetEvent.WaitOne(0);

    /// <summary>
    /// Synchronously get the result of an asynchronous operation.
    /// </summary>
    /// <returns>
    /// The result of the asynchronous operation.
    /// </returns>
    public TResult? GetResult()
    {
        manualResetEvent.WaitOne();
        return exception != null ? throw exception : result;
    }

    /// <summary>
    /// Tries to synchronously get the result of an asynchronous operation.
    /// </summary>
    /// <param name="operationResult">
    /// The result of the operation.
    /// </param>
    /// <returns>
    /// The result of the asynchronous operation.
    /// </returns>
    public bool TryGetResult(out TResult? operationResult)
    {
        if(IsComplete)
        {
            operationResult = exception != null ? throw exception : result;
            return true;
        }

        operationResult = default;
        return false;
    }

    /// <summary>
    /// Background "thread" which waits for the specified asynchronous operation to complete.
    /// </summary>
    /// <param name="task">
    /// The task.
    /// </param>
    private async void WaitFor(Task<TResult> task)
    {
        try
        {
            result = await task.ConfigureAwait(false);
        }
        catch(Exception ex)
        {
            exception = ex;
        }
        finally
        {
            manualResetEvent.Set();
        }
    }

    /// <summary>
    /// Background "thread" which waits for the specified asynchronous operation to complete.
    /// </summary>
    /// <param name="task">
    /// The task.
    /// </param>
    private async void WaitFor(ValueTask<TResult> task)
    {
        try
        {
            result = await task.ConfigureAwait(false);
        }
        catch(Exception ex)
        {
            exception = ex;
        }
        finally
        {
            manualResetEvent.Set();
        }
    }
}

/// <summary>
/// Internal class for  waiting for  asynchronous operations that have no result.
/// </summary>
internal class SynchronousAwaiter
{
    /// <summary>
    /// The manual reset event signaling completion.
    /// </summary>
    private readonly ManualResetEvent manualResetEvent;

    /// <summary>
    /// The exception thrown by the asynchronous operation.
    /// </summary>
    private Exception? exception;

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronousAwaiter{TResult}"/> class.
    /// </summary>
    /// <param name="task">
    /// The task representing an asynchronous operation.
    /// </param>
    /// <param name="ignoreCancellation">
    /// Indicates whether to ignore cancellation. Default is false.
    /// </param>
    public SynchronousAwaiter(Task task, bool ignoreCancellation = false)
    {
        manualResetEvent = new ManualResetEvent(false);
        WaitFor(task, ignoreCancellation);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SynchronousAwaiter{TResult}"/> class.
    /// </summary>
    /// <param name="task">
    /// The task representing an asynchronous operation.
    /// </param>
    /// <param name="ignoreCancellation">
    /// Indicates whether to ignore cancellation. Default is false.
    /// </param>
    public SynchronousAwaiter(ValueTask task, bool ignoreCancellation = false)
    {
        manualResetEvent = new ManualResetEvent(false);
        WaitFor(task, ignoreCancellation);
    }

    /// <summary>
    /// Gets a value indicating whether the operation is complete.
    /// </summary>
    public bool IsComplete => manualResetEvent.WaitOne(0);

    /// <summary>
    /// Synchronously get the result of an asynchronous operation.
    /// </summary>
    public void GetResult()
    {
        manualResetEvent.WaitOne();
        if (exception != null)
        {
            throw exception;
        }
    }

    /// <summary>
    /// Background "thread" which waits for the specified asynchronous operation to complete.
    /// </summary>
    /// <param name="task">
    /// The task.
    /// </param>
    /// <param name="ignoreCancellation">
    /// Indicates whether to ignore cancellation. Default is false.
    /// </param>
    private async void WaitFor(Task task, bool ignoreCancellation)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            this.exception = exception;
        }
        finally
        {
            manualResetEvent.Set();
        }
    }

    /// <summary>
    /// Background "thread" which waits for the specified asynchronous operation to complete.
    /// </summary>
    /// <param name="task">
    ///     The task.
    /// </param>
    /// <param name="ignoreCancellation">
    /// Indicates whether to ignore cancellation. Default is false.
    /// </param>
    private async void WaitFor(ValueTask task, bool ignoreCancellation)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        finally
        {
            manualResetEvent.Set();
        }
    }
}