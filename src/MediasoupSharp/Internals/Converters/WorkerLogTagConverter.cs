using MediasoupSharp.Constants;

namespace MediasoupSharp.Internals.Converters;

internal class WorkerLogTagConverter : EnumStringConverter<WorkerLogTag>
{
    protected override IEnumerable<(WorkerLogTag Enum, string Text)> Map()
    {
        yield return (WorkerLogTag.Info, "info");
        yield return (WorkerLogTag.Ice, "ice");
        yield return (WorkerLogTag.Dtls, "dtls");
        yield return (WorkerLogTag.Rtp, "rtp");
        yield return (WorkerLogTag.Srtp, "srtp");
        yield return (WorkerLogTag.Rtcp, "rtcp");
        yield return (WorkerLogTag.Rtx, "rtx");
        yield return (WorkerLogTag.Bwe, "bwe");
        yield return (WorkerLogTag.Score, "score");
        yield return (WorkerLogTag.Simulcast, "simulcast");
        yield return (WorkerLogTag.Svc, "svc");
        yield return (WorkerLogTag.Sctp, "sctp");
        yield return (WorkerLogTag.Message, "message");
    }
}