using Antelcat.MediasoupSharp.FBS.WebRtcTransport;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class FingerprintAlgorithmConverter : EnumStringConverter<FingerprintAlgorithm>
{
    protected override IEnumerable<(FingerprintAlgorithm Enum, string Text)> Map()
    {
        yield return (FingerprintAlgorithm.SHA1, "sha-1");
        yield return (FingerprintAlgorithm.SHA224, "sha-224");
        yield return (FingerprintAlgorithm.SHA256, "sha-256");
        yield return (FingerprintAlgorithm.SHA384, "sha-384");
        yield return (FingerprintAlgorithm.SHA512, "sha-512");
    }
}