using System.Text.Json;
using System.Text.Json.Serialization;

namespace Antelcat.MediasoupSharp.Internals.Converters;

internal interface IEnumStringConverter
{
    public string? Convert(Enum value);

    public Enum? ConvertBack(string? value);

    public static JsonSerializerOptions JsonSerializerOptions
    {
        get
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
            };
            foreach (var converter in JsonConverters)
            {
                options.Converters.Add(converter);
            }

            return options;
        }
    }

    public static IReadOnlyCollection<JsonConverter> JsonConverters { get; set; } = [..Converters()];
    
    private static IEnumerable<JsonConverter> Converters()
    {
        yield return new BweTypeConverter();
        yield return new ConsumerTraceEventTypeConverter();
        yield return new DataProducerTypeConverter();
        yield return new DtlsRoleConverter();
        yield return new DtlsStateConverter();
        yield return new FingerprintAlgorithmConverter();
        yield return new IceCandidateTcpTypeConverter();
        yield return new IceCandidateTypeConverter();
        yield return new IceRoleConverter();
        yield return new IceStateConverter();
        yield return new MediaKindConverter();
        yield return new MethodConverter();
        yield return new ProducerTraceEventTypeConverter();
        yield return new ProtocolConverter();
        yield return new RtpHeaderExtensionDirectionConverter();
        yield return new RtpHeaderExtensionUriConverter();
        yield return new RtpParameterTypeConverter();
        yield return new SctpStateConverter();
        yield return new SrtpCryptoSuiteConverter();
        yield return new TraceDirectionConverter();
        yield return new TransportTraceEventTypeConverter();
        yield return new WorkerLogLevelConverter();
        yield return new WorkerLogTagConverter();
    }
}

internal abstract class EnumStringConverter<TEnum> : JsonConverter<TEnum>, IEnumStringConverter where TEnum : Enum
{
    #region JSON

    public override TEnum? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        ConvertBack(reader.GetString()) is TEnum @enum
            ? @enum
            : default;

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) =>
        writer.WriteStringValue(Convert(value));

    #endregion

    #region Convert
    public string? Convert(Enum value) => value is TEnum @enum && Pairs.TryGetValue(@enum, out var ret) ? ret : null;
    public Enum?   ConvertBack(string? value) => value is null ? null : Pairs.FirstOrDefault(x => x.Value == value).Key;
    #endregion

    #region Cache
    private IReadOnlyDictionary<TEnum, string> Pairs => pairs ??= Map().ToDictionary(x => x.Enum, x => x.Text);

    private static IReadOnlyDictionary<TEnum, string>? pairs;
    #endregion
    
    protected abstract IEnumerable<(TEnum Enum, string Text)> Map();
    
}