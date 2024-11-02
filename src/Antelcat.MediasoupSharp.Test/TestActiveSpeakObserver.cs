using System.Runtime.Serialization.Formatters.Binary;
using FBS.RtpParameters;
using Antelcat.MediasoupSharp.RtpParameters;

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
            Parameters = new Dictionary<string, object>
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