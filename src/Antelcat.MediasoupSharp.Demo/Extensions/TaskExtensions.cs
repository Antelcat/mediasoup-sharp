﻿using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Antelcat.MediasoupSharp.Demo.Extensions;

[SuppressMessage("Usage", "VSTHRD110:Observe result of async calls")]
[SuppressMessage("Usage", "VSTHRD105:Avoid method overloads that assume TaskScheduler.Current")]
internal static class TaskExtensions
{
#pragma warning disable VSTHRD200
    public static void Catch(this Task task, Action<Exception?> action) =>
        task.ContinueWith(t => action(t.Exception), TaskContinuationOptions.OnlyOnFaulted);

    public static void Catch(this Task task, Action action) =>
        task.ContinueWith(t => action(), TaskContinuationOptions.OnlyOnFaulted);

    public static void Catch(this Task task, Func<Exception?, Task> func) =>
        task.ContinueWith(t => func(t.Exception), TaskContinuationOptions.OnlyOnFaulted);

    public static void Catch(this Task task, Func<Task> func) =>
        task.ContinueWith(t => func(), TaskContinuationOptions.OnlyOnFaulted);
#pragma warning restore VSTHRD200

    public static void AddTo(this Task task, ICollection<Task> tasks) => tasks.Add(task);

    public static TaskAwaiter GetAwaiter(this IEnumerable<Task> tasks) => Task.WhenAll(tasks).GetAwaiter();
}