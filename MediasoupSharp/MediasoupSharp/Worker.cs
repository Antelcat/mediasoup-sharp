using System.Runtime.InteropServices.JavaScript;

namespace MediasoupSharp;

public enum WorkerLogLevel
{
    debug,
    warn,
    error,
    none
}

public enum WorkerLogTag
{
    info,
    ice,
    dtls,
    rtp,
    srtp,
    rtcp,
    rtx,
    bwe,
    score,
    simulcast,
    svc,
    sctp,
    message,
}

public record WorkerSettings<TWorkerAppData>(
    WorkerLogLevel? logLevel,
    List<WorkerLogTag>? logTags,
    Number? rtcMinPort,
    Number? rtcMaxPort,
    string? dtlsCertificateFile,
    string? dtlsPrivateKeyFile,
    string? libwebrtcFieldTrials,
    TWorkerAppData? appData) where TWorkerAppData : AppData;