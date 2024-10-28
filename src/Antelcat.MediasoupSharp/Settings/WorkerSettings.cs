namespace Antelcat.MediasoupSharp.Settings;

public record WorkerSettings
{
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
    
    public Dictionary<string,object>? AppData { get; set; }
}