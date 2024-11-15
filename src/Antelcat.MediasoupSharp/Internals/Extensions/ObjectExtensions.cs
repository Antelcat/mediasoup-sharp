using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Antelcat.AutoGen.ComponentModel;
using Antelcat.MediasoupSharp.FBS.RtpParameters;
using Antelcat.MediasoupSharp.FBS.SctpParameters;
using Antelcat.MediasoupSharp.FBS.Transport;

namespace Antelcat.MediasoupSharp.Internals.Extensions;

[AutoObjectClone]
internal static partial class ObjectExtensions
{
    public const DynamicallyAccessedMemberTypes CloneMemberTypes =
        DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors |
        DynamicallyAccessedMemberTypes.PublicFields       | DynamicallyAccessedMemberTypes.NonPublicFields       |
        DynamicallyAccessedMemberTypes.PublicNestedTypes  | DynamicallyAccessedMemberTypes.NonPublicNestedTypes;

        
    static ObjectExtensions()
    {
        Register(typeof(List<>));
        Register(typeof(Dictionary<,>));
        Register(typeof(KeyValuePair<,>));
        Register(typeof(EqualityComparer<>));

        Register(typeof(RtpCapabilities));
        Register(typeof(RtpCodecCapability));
        Register(typeof(RtpCodecShared));
        Register(typeof(RtpHeaderExtension));
            
        Register(typeof(RtpCodecParameters));
        Register(typeof(RtpCodecParameters));
        Register(typeof(RtcpFeedbackT));
            
            
        Register(typeof(ListenInfoT));
        Register(typeof(Protocol));
        Register(typeof(PortRangeT));
        Register(typeof(SocketFlagsT));
            
        Register(typeof(SctpStreamParametersT));
        Register(typeof(RtpEncodingParametersT));
        Register(typeof(RtxT));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Sure<T>(this object obj) where T : class => obj as T ?? throw new InvalidCastException();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T NotNull<T>(this T? obj) =>
        obj ?? throw new NullReferenceException($"{typeof(T)} is null in {nameof(NotNull)}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T NotNull<T>(this T? obj) where T : struct =>
        obj ?? throw new NullReferenceException($"{typeof(T)} is null in {nameof(NotNull)}");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsStringType(this object o) =>
        o is JsonElement jsonElement
            ? jsonElement.ValueKind == JsonValueKind.String
            : o is string;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNumericType(this object o) =>
        o is JsonElement jsonElement
            ? jsonElement.ValueKind == JsonValueKind.Number
            : o
                is byte or sbyte
                or short or ushort
                or int or uint
                or long or ulong
                or decimal
                or float or double;
}