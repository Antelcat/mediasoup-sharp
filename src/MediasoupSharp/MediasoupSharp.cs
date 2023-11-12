using MediasoupSharp.RtpParameters;
using MediasoupSharp.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.CompilerServices;

namespace MediasoupSharp;

public static partial class MediasoupSharp
{
    public static string WorkerBin { get; set; }

    internal static string? Env(string key) => Environment.GetEnvironmentVariable(key);

    public static string Version = "__MEDIASOUP_VERSION__";
    
    public static ScalabilityMode ParseScalabilityMode(string? scalabilityMode) => ScalabilityMode.Parse(scalabilityMode);
    
    private static ILogger? logger;

    public static readonly IEnhancedEventEmitter<ObserverEvents> Observer = new EnhancedEventEmitter<ObserverEvents>();

    public static async Task<IWorker> CreateWorker(WorkerSettings<object>? settings = null,
        ILoggerFactory? loggerFactory = null) =>
        await CreateWorker<object>(settings, loggerFactory);

    public static async Task<IWorker<TWorkerAppData>> CreateWorker<TWorkerAppData>(
        WorkerSettings<TWorkerAppData>? settings = null,
        ILoggerFactory? loggerFactory = null)
    {
        logger = loggerFactory?.CreateLogger($"{nameof(MediasoupSharp)}");

        var logLevel             = settings?.LogLevel ?? WorkerLogLevel.error;
        var logTags              = settings?.LogTags;
        var rtcMinPort           = settings?.RtcMinPort ?? 10000;
        var rtcMaxPort           = settings?.RtcMaxPort ?? 59999;
        var dtlsCertificateFile  = settings?.DtlsCertificateFile;
        var dtlsPrivateKeyFile   = settings?.DtlsPrivateKeyFile;
        var libwebrtcFieldTrials = settings?.LibwebrtcFieldTrials;
        var appData              = settings == null ? typeof(TWorkerAppData).New<TWorkerAppData>() : settings.AppData;

        logger?.LogDebug("CreateWorker()");

        var worker = new Worker<TWorkerAppData>(
            new WorkerSettings<TWorkerAppData>
            {
                LogLevel             = logLevel,
                LogTags              = logTags,
                RtcMinPort           = rtcMinPort,
                RtcMaxPort           = rtcMaxPort,
                DtlsCertificateFile  = dtlsCertificateFile,
                DtlsPrivateKeyFile   = dtlsPrivateKeyFile,
                LibwebrtcFieldTrials = libwebrtcFieldTrials,
                AppData              = appData
            }, loggerFactory);
        var taskSource = new TaskCompletionSource<IWorker<TWorkerAppData>>();
        worker.On("@success", async _ =>
        {
            // Emit observer event.
            await Observer.SafeEmit("newworker", worker);

            taskSource.SetResult(worker);
        });
        worker.On("@failure", async _ => { taskSource.SetException(new Exception()); });
        return await taskSource.Task;
    }

    public static RtpCapabilities GetSupportedRtpCapabilities() => SupportedRtpCapabilities.DeepClone();
}