namespace MediasoupSharp;

public record SrtpParameters(SrtpCryptoSuite cryptoSuite, string keyBase64);

public enum SrtpCryptoSuite
{
    AEAD_AES_256_GCM,
    AEAD_AES_128_GCM,
    AES_CM_128_HMAC_SHA1_80,
    AES_CM_128_HMAC_SHA1_32,
}
