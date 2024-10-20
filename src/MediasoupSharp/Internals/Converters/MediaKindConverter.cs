using FBS.RtpParameters;

namespace MediasoupSharp.Internals.Converters;

internal class MediaKindConverter : EnumStringConverter<MediaKind> 
{
    protected override IEnumerable<(MediaKind Enum, string Text)> Map()
    {
        yield return (MediaKind.VIDEO, "video");
        yield return (MediaKind.AUDIO, "audio");
    }
}

