using System.Collections;
using System.Runtime.InteropServices.JavaScript;

namespace MediasoupSharp;

public enum WorkerLogLevel
{
    Debug,
    Warn,
    Error,
    None
}

public enum WorkerLogTag
{
    Info,
    Ice,
    Dtls,
    Rtp,
    Srtp,
    Rtcp,
    Rtx,
    Bwe,
    Score,
    Simulcast,
    Svc,
    Sctp,
    Message,
}

public record WorkerSettings<TWorkerAppData>(
    WorkerLogLevel? LogLevel,
    List<WorkerLogTag>? LogTags,
    Number? RtcMinPort,
    Number? RtcMaxPort,
    string? DtlsCertificateFile,
    string? DtlsPrivateKeyFile,
    string? LibwebrtcFieldTrials,
    TWorkerAppData? AppData) where TWorkerAppData : AppData;

public class WorkerResourceUsage
{
    public Number RuUtime;

    public Number RuStime;

    public Number RuMaxrss;

    public Number RuIxrss;

    public Number RuIdrss;

    public Number RuIsrss;

    public Number RuMinflt;

    public Number RuMajflt;

    public Number RuNswap;

    public Number RuInblock;

    public Number RuOublock;

    public Number RuMsgsnd;

    public Number RuMsgrcv;

    public Number RuNsignals;

    public Number RuNvcsw;

    public Number RuNivcsw;
}

public record WorkerEvents(
    List<Exception> Died,
    ArrayList Success,
    List<Exception> Failure);

public class Worker<TWorkerAppData> 
    : EnhancedEventEmitter<WorkerEvents> 
    where TWorkerAppData : AppData
{
    
}