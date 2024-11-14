using Antelcat.MediasoupSharp.FBS.SrtpParameters;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal class SrtpCryptoSuiteConverter : EnumStringConverter<SrtpCryptoSuite>
{
    protected override IEnumerable<(SrtpCryptoSuite Enum, string Text)> Map()
    {
        yield return (SrtpCryptoSuite.AEAD_AES_256_GCM, "AEAD_AES_256_GCM");
        yield return (SrtpCryptoSuite.AEAD_AES_128_GCM, "AEAD_AES_128_GCM");
        yield return (SrtpCryptoSuite.AES_CM_128_HMAC_SHA1_80, "AES_CM_128_HMAC_SHA1_80");
        yield return (SrtpCryptoSuite.AES_CM_128_HMAC_SHA1_32, "AES_CM_128_HMAC_SHA1_32");
    }
}