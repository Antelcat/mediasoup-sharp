using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace MediasoupSharp.Constants;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkerLogLevel
{
    /// <summary>
    /// Log all severities.
    /// </summary>
    [EnumMember(Value = nameof(debug))]
    debug,

    /// <summary>
    /// Log “warn” and “error” severities.
    /// </summary>
    [EnumMember(Value = nameof(warn))]
    warn,

    /// <summary>
    /// Log “error” severity.
    /// </summary>
    [EnumMember(Value = nameof(error))]
    error,

    /// <summary>
    /// Do not log anything.
    /// </summary>
    [EnumMember(Value = nameof(none))]
    none
}
