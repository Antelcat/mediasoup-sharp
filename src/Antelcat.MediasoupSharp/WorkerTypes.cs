global using WorkerObserver = Antelcat.MediasoupSharp.EnhancedEventEmitter<Antelcat.MediasoupSharp.WorkerObserverEvents>;

namespace Antelcat.MediasoupSharp;

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

public enum WorkerLogTag
{
    /// <summary>
    /// Logs about software/library versions, configuration and process information.
    /// </summary>
    Info,

    /// <summary>
    /// Logs about ICE.
    /// </summary>
    Ice,

    /// <summary>
    /// Logs about DTLS.
    /// </summary>
    Dtls,

    /// <summary>
    /// Logs about RTP.
    /// </summary>
    Rtp,

    /// <summary>
    /// Logs about SRTP encryption/decryption.
    /// </summary>
    Srtp,

    /// <summary>
    /// Logs about RTCP.
    /// </summary>
    Rtcp,

    /// <summary>
    /// Logs about RTP retransmission, including NACK/PLI/FIR.
    /// </summary>
    Rtx,

    /// <summary>
    /// Logs about transport bandwidth estimation.
    /// </summary>
    Bwe,

    /// <summary>
    /// Logs related to the scores of Producers and Consumers.
    /// </summary>
    Score,

    /// <summary>
    /// Logs about video simulcast.
    /// </summary>
    Simulcast,

    /// <summary>
    /// Logs about video SVC.
    /// </summary>
    Svc,

    /// <summary>
    /// Logs about SCTP (DataChannel).
    /// </summary>
    Sctp,

    /// <summary>
    /// Logs about messages (can be SCTP messages or direct messages).
    /// </summary>
    Message
}

public record WorkerSettings<TWorkerAppData>
{
    public string?         WorkerFile          { get; set; }
    public string?         DtlsCertificateFile { get; set; }
    public string?         DtlsPrivateKeyFile  { get; set; }
    public WorkerLogLevel? LogLevel            { get; set; } = WorkerLogLevel.Warn;
    public WorkerLogTag[]  LogTags             { get; set; } = [];

    /// <summary>
    /// Minimum RTC port for ICE, DTLS, RTP, etc. Default 10000.
    /// </summary>
    [Obsolete("Use |PortRange| in TransportListenInfo object instead.")]
    public int? RtcMinPort { get; set; }

    /// <summary>
    /// Maximum RTC port for ICE, DTLS, RTP, etc. Default 59999.
    /// </summary>
    [Obsolete("Use |PortRange| in TransportListenInfo object instead.")]
    public int? RtcMaxPort { get; set; }

    public string? LibwebrtcFieldTrials { get; set; }

    public bool? DisableLiburing { get; set; }

    public TWorkerAppData? AppData { get; set; }
}

public abstract class WorkerEvents
{
    public Exception?           Died;
    public object?              SubprocessClose;
    public (string, Exception)? ListenerError;

    // Private events.
    internal object?    success;
    internal Exception? failure;
}

public abstract class WorkerObserverEvents
{
    public object?       Close;
    public required IWebRtcServer NewWebrtcServer;
    public required IRouter       NewRouter;
}

public interface IWorker<TWorkerAppData> : IEnhancedEventEmitter<WorkerEvents>, IWorker
{
    TWorkerAppData AppData { get; set; }
}