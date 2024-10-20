namespace MediasoupSharp.Constants;

public enum WorkerLogLevel
{
    /// <summary>
    /// Log all severities.
    /// </summary>
    Debug,

    /// <summary>
    /// Log “warn” and “error” severities.
    /// </summary>
    Warn,

    /// <summary>
    /// Log “error” severity.
    /// </summary>
    Error,

    /// <summary>
    /// Do not log anything.
    /// </summary>
    None
}