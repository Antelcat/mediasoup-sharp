using Antelcat.MediasoupSharp.FBS.RtpParameters;

namespace Antelcat.MediasoupSharp.Test;

public class TestActiveSpeakObserver
{
    private static readonly List<RtpCodecCapability> MediaCodecs = new()
    {
        new RtpCodecCapability
        {
            Kind      = MediaKind.AUDIO,
            MimeType  = "audio/opus",
            ClockRate = 48000,
            Channels  = 2,
            Parameters = new ()
            {
                { "useinbandfec", 1 },
                { "foo", "bar" }
            }
        }
    };


    [SetUp]
    public async Task Setup()
    {
    }

    [Test]
    public async Task CreateActiveSpeakerObserver()
    {
    }

  
}