namespace MediasoupSharp;

public record SrtpParameters(SrtpCryptoSuite CryptoSuite, string KeyBase64);

public enum SrtpCryptoSuite
{
    AeadAes256Gcm,
    AeadAes128Gcm,
    AesCm128HmacSha180,
    AesCm128HmacSha132,
}
