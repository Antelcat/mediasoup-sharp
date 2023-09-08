﻿namespace MediasoupSharp.Settings;

public class WorkerUpdateableSettings
{
    /// <summary>
    /// Logging level for logs generated by the media worker subprocesses (check the Debugging documentation). Valid values are “debug”, “warn”, “error” and “none”.
    /// </summary>
    public WorkerLogLevel? LogLevel { get; set; } = WorkerLogLevel.Error;

    /// <summary>
    /// Log tags for debugging. Check the list of available tags in Debugging documentation.
    /// </summary>
    public WorkerLogTag[]? LogTags { get; set; }
}