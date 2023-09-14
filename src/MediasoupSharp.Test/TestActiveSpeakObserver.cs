using System.Diagnostics;
using MediasoupSharp.ActiveSpeakerObserver;
using MediasoupSharp.Router;
using MediasoupSharp.RtpParameters;
using MediasoupSharp.Worker;

namespace MediasoupSharp.Test;

public class TestActiveSpeakObserver
{
    private static readonly List<RtpCodecCapability> MediaCodecs = new()
    {
        new RtpCodecCapability
        {
            Kind      = MediaKind.audio,
            MimeType  = "audio/opus",
            ClockRate = 48000,
            Channels  = 2,
            Parameters = new Dictionary<string, object?>
            {
                { "useinbandfec", 1 },
                { "foo", "bar" }
            }
        }
    };

    private IWorker                worker;
    private IRouter                router;
    private IActiveSpeakerObserver activeSpeakerObserver;

    [SetUp]
    public async Task Setup()
    {
        MediasoupSharp.Version   = "3.12.11";
        MediasoupSharp.WorkerBin = "./runtimes/win-x64/native/mediasoup-worker.exe";
        worker                   = await MediasoupSharp.CreateWorker(loggerFactory: new DebugLoggerFactory());
        router                   = await worker.CreateRouter(new RouterOptions<object> { MediaCodecs = MediaCodecs });
    }

    [Test]
    public async Task CreateActiveSpeakerObserver()
    {
        router.Observer.Once("newrtpobserver", async _ => { Debugger.Break(); });
        activeSpeakerObserver = await router.CreateActiveSpeakerObserverAsync();
        worker.Close();
    }
}