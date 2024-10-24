using Antelcat.MediasoupSharp.Constants;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class WorkerLogLevelConverter : EnumStringConverter<WorkerLogLevel>
{
    protected override IEnumerable<(WorkerLogLevel Enum, string Text)> Map()
    {
        yield return (WorkerLogLevel.Debug, "debug");
        yield return (WorkerLogLevel.Warn, "warn");
        yield return (WorkerLogLevel.Error, "error");
        yield return (WorkerLogLevel.None, "none");
    }
}