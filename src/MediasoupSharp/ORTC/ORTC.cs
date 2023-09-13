using System.Diagnostics;
using System.Dynamic;
using System.Text.RegularExpressions;
using MediasoupSharp.RtpParameters;
using MediasoupSharp.SctpParameters;

namespace MediasoupSharp.ORTC;

internal static partial class Ortc
{
    private static readonly Regex MimeTypeRegex    = MimeRegex();
    private static readonly Regex RtxMimeTypeRegex = RtxMimeRegex();

    [GeneratedRegex("^(audio|video)/(.+)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "zh-CN")]
    private static partial Regex MimeRegex();

    [GeneratedRegex("^.+/rtx$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "zh-CN")]
    private static partial Regex RtxMimeRegex();
    

    public static readonly int[] DynamicPayloadTypes = new[]
    {
        100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110,
        111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121,
        122, 123, 124, 125, 126, 127, 96, 97, 98, 99
    };

    /// <summary>
    /// Validates RtpCapabilities. It may modify given data by adding missing
    /// fields with default values.
    /// It throws if invalid.
    /// </summary>
    public static void ValidateRtpCapabilities(RtpCapabilities caps)
    {
        if (caps == null)
        {
            throw new ArgumentNullException(nameof(caps));
        }

        caps.Codecs ??= new();

        foreach (var codec in caps.Codecs)
        {
            ValidateRtpCodecCapability(codec);
        }

        // headerExtensions is optional. If unset, fill with an empty array.
        caps.HeaderExtensions ??= new();

        foreach (var ext in caps.HeaderExtensions)
        {
            ValidateRtpHeaderExtension(ext);
        }
    }

    /// <summary>
    /// Validates RtpCodecCapability. It may modify given data by adding missing
    /// fields with default values.
    /// It throws if invalid.
    /// </summary>
    public static void ValidateRtpCodecCapability(RtpCodecCapability codec)
    {
        // mimeType is mandatory.
        if (codec.MimeType.IsNullOrEmpty())
        {
            throw new TypeError("missing codec.mimeType");
        }

        if (!MimeTypeRegex.IsMatch(codec.MimeType))
        {
            throw new ArgumentException("invalid codec.mimeType");
        }

        // Just override kind with media component in mimeType.
        codec.Kind = codec.MimeType.ToLower().StartsWith(nameof(MediaKind.video))
            ? MediaKind.video
            : MediaKind.audio;

        // preferredPayloadType is optional.
        // 在 Node.js 实现中，判断了 preferredPayloadType 在有值的情况下的数据类型。在强类型语言中不需要。

        // clockRate is mandatory.
        // 在 Node.js 实现中，判断了 mandatory 的数据类型。在强类型语言中不需要。

        // channels is optional. If unset, set it to 1 (just if audio).
        if (codec is { Kind: MediaKind.audio })
        {
            codec.Channels ??= 1;
        }
        else
        {
            codec.Channels = null;
        }

        // parameters is optional. If unset, set it to an empty object.
        codec.Parameters ??= new ExpandoObject();

        foreach (var (key, val) in codec.Parameters)
        {
            var value = val;

            if (value == null)
            {
                codec.Parameters[key] = string.Empty;
                value                 = string.Empty;
            }

            if (value is not (string or uint or int or ulong or long or byte or sbyte))
            {
                throw new TypeError($"invalid codec parameter [key:{key}, value:{value}]");
            }

            // Specific parameters validation.
            if (key == "apt")
            {
                if (value is not (uint or int or ulong or long or byte or sbyte))
                {
                    throw new TypeError("invalid codec apt parameter");
                }
            }
        }

        // rtcpFeedback is optional. If unset, set it to an empty array.
        codec.RtcpFeedback ??= new();

        foreach (var fb in codec.RtcpFeedback)
        {
            ValidateRtcpFeedback(fb);
        }
    }

    /// <summary>
    /// Validates RtcpFeedback. It may modify given data by adding missing
    /// fields with default values.
    /// It throws if invalid.
    /// </summary>
    public static void ValidateRtcpFeedback(RtcpFeedback fb)
    {
        if (fb == null)
        {
            throw new TypeError(nameof(fb));
        }

        // type is mandatory.
        if (fb.Type.IsNullOrEmpty())
        {
            throw new TypeError("missing fb.type");
        }

        // parameter is optional. If unset set it to an empty string.
        if (fb.Parameter.IsNullOrEmpty())
        {
            fb.Parameter = string.Empty;
        }
    }

    /// <summary>
    /// Validates RtpHeaderExtension. It may modify given data by adding missing
    /// fields with default values.
    /// It throws if invalid.
    /// </summary>
    public static void ValidateRtpHeaderExtension(RtpHeaderExtension ext)
    {
        if (ext == null)
        {
            throw new TypeError("ext is not an object");
        }

        // 在 Node.js 实现中，判断了 kind 的值。在强类型语言中不需要。

        // uri is mandatory.
        if (ext.Uri.IsNullOrEmpty())
        {
            throw new TypeError($"missing ext.uri");
        }

        // preferredId is mandatory.
        // 在 Node.js 实现中，判断了 preferredId 的数据类型。在强类型语言中不需要。

        // preferredEncrypt is optional. If unset set it to false.
        ext.PreferredEncrypt ??= false;

        // direction is optional. If unset set it to sendrecv.
        ext.Direction ??= RtpHeaderExtensionDirection.sendrecv;
    }

    /// <summary>
    /// Validates RtpParameters. It may modify given data by adding missing
    /// fields with default values.
    /// It throws if invalid.
    /// </summary>
    public static void ValidateRtpParameters(RtpParameters.RtpParameters parameters)
    {
        if (parameters == null)
        {
            throw new TypeError("params is not an object");
        }
        // mid is optional.
        // 在 Node.js 实现中，判断了 mid 的数据类型。在强类型语言中不需要。

        // codecs is mandatory.
        if (parameters.Codecs == null)
        {
            throw new TypeError("missing params.codecs");
        }

        foreach (var codec in parameters.Codecs)
        {
            ValidateRtpCodecParameters(codec);
        }

        // headerExtensions is optional. If unset, fill with an empty array.
        parameters.HeaderExtensions ??= new();

        foreach (var ext in parameters.HeaderExtensions)
        {
            ValidateRtpHeaderExtensionParameters(ext);
        }

        // encodings is optional. If unset, fill with an empty array.
        parameters.Encodings ??= new();

        foreach (var encoding in parameters.Encodings)
        {
            ValidateRtpEncodingParameters(encoding);
        }

        // rtcp is optional. If unset, fill with an empty object.
        // 对 RtcpParameters 序列化时，CNAME 为 null 会忽略，因为客户端库对其有校验。
        parameters.Rtcp ??= new RtcpParameters();
        ValidateRtcpParameters(parameters.Rtcp);
    }

    /// <summary>
    /// Validates RtpCodecParameters. It may modify given data by adding missing
    /// fields with default values.
    /// It throws if invalid.
    /// </summary>
    public static void ValidateRtpCodecParameters(RtpCodecParameters codec)
    {
        if (codec == null)
        {
            throw new TypeError("codec is not an object");
        }

        // mimeType is mandatory.
        if (codec.MimeType.IsNullOrEmpty())
        {
            throw new TypeError("missing codec.mimeType");
        }

        var mimeTypeMatch = MimeTypeRegex.Matches(codec.MimeType);
        if (mimeTypeMatch.Count == 0)
        {
            throw new TypeError("invalid codec.mimeType");
        }

        // payloadType is mandatory.
        // 在 Node.js 实现中，判断了 payloadType 的数据类型。在强类型语言中不需要。

        // clockRate is mandatory.
        // 在 Node.js 实现中，判断了 clockRate 的数据类型。在强类型语言中不需要。

        var kind = mimeTypeMatch[1].Value.ToLower().StartsWith("audio")
            ? MediaKind.audio
            : MediaKind.video;

        // channels is optional. If unset, set it to 1 (just if audio).
        // 在 Node.js 实现中，如果是 `video` 会 delete 掉 Channels 。

        if (kind is MediaKind.audio)
        {
            codec.Channels ??= 1;
        }
        else
        {
            codec.Channels = null;
        }

        // parameters is optional. If unset, set it to an empty object.
        codec.Parameters ??= new ExpandoObject();

        foreach (var (key, _) in codec.Parameters)
        {
            object? value = codec.Parameters[key];

            if (value == null)
            {
                codec.Parameters[key] = string.Empty;
                value                 = string.Empty;
            }

            if (value is not (string or uint or int or ulong or long or byte or sbyte))
            {
                throw new TypeError($"invalid codec parameter[key:{key}, value:{value}]");
            }

            // Specific parameters validation.
            if (key == "apt")
            {
                if (value is not (uint or int or ulong or long or byte or sbyte))
                {
                    throw new TypeError("invalid codec apt parameter");
                }
            }
        }

        // rtcpFeedback is optional. If unset, set it to an empty array.
        codec.RtcpFeedback ??= new();

        foreach (var fb in codec.RtcpFeedback)
        {
            ValidateRtcpFeedback(fb);
        }
    }

    /// <summary>
    /// Validates RtpHeaderExtensionParameteters. It may modify given data by adding missing
    /// fields with default values.
    /// It throws if invalid.
    /// </summary>
    public static void ValidateRtpHeaderExtensionParameters(RtpHeaderExtensionParameters ext)
    {
        if (ext == null)
        {
            throw new ArgumentNullException(nameof(ext));
        }

        // uri is mandatory.
        if (ext.Uri.IsNullOrEmpty())
        {
            throw new TypeError($"missing ext.uri");
        }

        // id is mandatory.
        // 在 Node.js 实现中，判断了 id 的数据类型。在强类型语言中不需要。

        // encrypt is optional. If unset set it to false.
        ext.Encrypt ??= false;

        // parameters is optional. If unset, set it to an empty object.
        ext.Parameters ??= new ExpandoObject();

        foreach (var (key, _) in (ext.Parameters as ExpandoObject)!)
        {
            var value = ext.Parameters[key];

            if (value == null)
            {
                ext.Parameters[key] = string.Empty;
                value               = string.Empty;
            }

            if (value is not (uint or int or ulong or long or byte or sbyte))
            {
                throw new TypeError($"invalid codec parameter[key:{key}, value:{value}]");
            }
        }
    }

    /// <summary>
    /// Validates RtpEncodingParameters. It may modify given data by adding missing
    /// fields with default values.
    /// It throws if invalid.
    /// </summary>
    public static void ValidateRtpEncodingParameters(RtpEncodingParameters encoding)
    {
        if (encoding == null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        // ssrc is optional.
        // 在 Node.js 实现中，判断了 ssrc 的数据类型。在强类型语言中不需要。

        // rid is optional.
        // 在 Node.js 实现中，判断了 rid 的数据类型。在强类型语言中不需要。

        // rtx is optional.
        // 在 Node.js 实现中，判断了 rtx 的数据类型。在强类型语言中不需要。
        if (encoding.Rtx != null)
        {
            // RTX ssrc is mandatory if rtx is present.
            // 在 Node.js 实现中，判断了 rtx.ssrc 的数据类型。在强类型语言中不需要。
        }

        // dtx is optional. If unset set it to false.
        encoding.Dtx ??= false;

        // scalabilityMode is optional.
        // 在 Node.js 实现中，判断了 scalabilityMode 的数据类型。在强类型语言中不需要。
    }

    /// <summary>
    /// Validates RtcpParameters. It may modify given data by adding missing
    /// fields with default values.
    /// It throws if invalid.
    /// </summary>
    public static void ValidateRtcpParameters(RtcpParameters rtcp)
    {
        if (rtcp == null)
        {
            throw new ArgumentNullException(nameof(rtcp));
        }

        // cname is optional.
        // 在 Node.js 实现中，判断了 cname 的数据类型。在强类型语言中不需要。

        // reducedSize is optional. If unset set it to true.
        rtcp.ReducedSize ??= true;
    }

    /// <summary>
    /// Validates SctpCapabilities. It may modify given data by adding missing
    /// fields with default values.
    /// It throws if invalid.
    /// </summary>
    public static void ValidateSctpCapabilities(SctpCapabilities caps)
    {
        if (caps == null)
        {
            throw new ArgumentNullException(nameof(caps));
        }

        // numStreams is mandatory.
        if (caps.NumStreams == null)
        {
            throw new TypeError("missing caps.numStreams");
        }

        ValidateNumSctpStreams(caps.NumStreams);
    }

    /// <summary>
    /// Validates NumSctpStreams. It may modify given data by adding missing
    /// fields with default values.
    /// It throws if invalid.
    /// </summary>
    public static void ValidateNumSctpStreams(NumSctpStreams numStreams)
    {
    }

    /// <summary>
    /// Validates SctpParameters. It may modify given data by adding missing
    /// fields with default values.
    /// It throws if invalid.
    /// </summary>
    public static void ValidateSctpParameters(SctpParameters.SctpParameters parameters)
    {
    }

    /// <summary>
    /// Validates SctpStreamParameters. It may modify given data by adding missing
    /// fields with default values.
    /// It throws if invalid.
    /// </summary>
    public static void ValidateSctpStreamParameters(SctpStreamParameters parameters)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        // streamId is mandatory.
        // 在 Node.js 实现中，判断了 streamId 的数据类型。在强类型语言中不需要。

        // ordered is optional.
        var orderedGiven = false;

        if (parameters.Ordered.HasValue)
        {
            orderedGiven = true;
        }
        else
        {
            parameters.Ordered = true;
        }

        // maxPacketLifeTime is optional.
        // 在 Node.js 实现中，判断了 maxPacketLifeTime 的数据类型。在强类型语言中不需要。

        // maxRetransmits is optional.
        // 在 Node.js 实现中，判断了 maxRetransmits 的数据类型。在强类型语言中不需要。

        if (parameters is { MaxPacketLifeTime: not null, MaxRetransmits: not null })
        {
            throw new TypeError("cannot provide both maxPacketLifeTime and maxRetransmits");
        }

        parameters.Ordered = orderedGiven switch
        {
            true when parameters.Ordered.Value &&
                      (parameters.MaxPacketLifeTime.HasValue || parameters.MaxRetransmits.HasValue) =>
                throw new TypeError("cannot be ordered with maxPacketLifeTime or maxRetransmits"),
            false when parameters.MaxPacketLifeTime.HasValue || parameters.MaxRetransmits.HasValue
                => false,
            _ => parameters.Ordered
        };
    }

    /// <summary>
    /// Generate RTP capabilities for the Router based on the given media codecs and
    /// mediasoup supported RTP capabilities.
    /// </summary>
    public static RtpCapabilities GenerateRouterRtpCapabilities(List<RtpCodecCapability>? mediaCodecs)
    {
        if (mediaCodecs == null)
        {
            throw new ArgumentNullException(nameof(mediaCodecs));
        }

        // Normalize supported RTP capabilities.
        ValidateRtpCapabilities(SupportedRtpCapabilities);

        var clonedSupportedRtpCapabilities = SupportedRtpCapabilities.DeepClone();
        var dynamicPayloadTypes            = DynamicPayloadTypes.DeepClone().ToList();
        var caps = new RtpCapabilities
        {
            Codecs           = new(),
            HeaderExtensions = clonedSupportedRtpCapabilities.HeaderExtensions
        };

        foreach (var mediaCodec in mediaCodecs)
        {
            // This may throw.
            ValidateRtpCodecCapability(mediaCodec);

            var matchedSupportedCodec = clonedSupportedRtpCapabilities
                .Codecs!
                .FirstOrDefault(supportedCodec =>
                    MatchCodecs(mediaCodec, supportedCodec, false));

            if (matchedSupportedCodec == null)
            {
                throw new Exception($"media codec not supported[mimeType:{mediaCodec.MimeType}]");
            }

            // Clone the supported codec.
            var codec = matchedSupportedCodec.DeepClone();

            // If the given media codec has preferredPayloadType, keep it.
            if (mediaCodec.PreferredPayloadType.HasValue)
            {
                codec.PreferredPayloadType = mediaCodec.PreferredPayloadType;

                // Also remove the pt from the list in available dynamic values.
                dynamicPayloadTypes.Remove(codec.PreferredPayloadType.Value);
            }
            // Otherwise if the supported codec has preferredPayloadType, use it.
            else if (codec.PreferredPayloadType.HasValue)
            {
                // No need to remove it from the list since it's not a dynamic value.
            }
            // Otherwise choose a dynamic one.
            else
            {
                // Take the first available pt and remove it from the list.
                var pt = dynamicPayloadTypes.FirstOrDefault();

                if (pt == 0)
                {
                    throw new Exception("cannot allocate more dynamic codec payload types");
                }

                dynamicPayloadTypes.RemoveAt(0);

                codec.PreferredPayloadType = pt;
            }

            // Ensure there is not duplicated preferredPayloadType values.
            if (caps.Codecs.Any(c => c.PreferredPayloadType == codec.PreferredPayloadType))
            {
                throw new TypeError("duplicated codec.preferredPayloadType");
            }

            // Merge the media codec parameters.
            codec.Parameters = codec.Parameters!.Merge(mediaCodec.Parameters!);

            // Append to the codec list.
            caps.Codecs.Add(codec);

            // Add a RTX video codec if video.
            if (codec.Kind == MediaKind.video)
            {
                // Take the first available pt and remove it from the list.
                var pt = dynamicPayloadTypes.FirstOrDefault();

                if (pt == 0)
                {
                    throw new Exception("cannot allocate more dynamic codec payload types");
                }

                dynamicPayloadTypes.RemoveAt(0);

                var rtxCodec = new RtpCodecCapability
                {
                    Kind                 = codec.Kind,
                    MimeType             = $"{codec.Kind}/rtx",
                    PreferredPayloadType = pt,
                    ClockRate            = codec.ClockRate,
                    Parameters = new Dictionary<string, object?>
                    {
                        { "apt", codec.PreferredPayloadType }
                    },
                    RtcpFeedback = new(),
                };

                // Append to the codec list.
                caps.Codecs.Add(rtxCodec);
            }
        }

        return caps;
    }

    /// <summary>
    /// Get a mapping in codec payloads and encodings in the given Producer RTP
    /// parameters as values expected by the Router.
    ///
    /// It may throw if invalid or non supported RTP parameters are given.
    /// </summary>
    public static RtpMapping GetProducerRtpParametersMapping(
        RtpParameters.RtpParameters parameters,
        RtpCapabilities caps)
    {
        var rtpMapping = new RtpMapping
        {
            Codecs    = new(),
            Encodings = new()
        };

        // Match parameters media codecs to capabilities media codecs.
        var codecToCapCodec = new Dictionary<RtpCodecParameters, RtpCodecCapability>();

        foreach (var codec in parameters.Codecs)
        {
            if (IsRtxCodec(codec))
            {
                continue;
            }

            // Search for the same media codec in capabilities.
            var matchedCapCodec = caps.Codecs!
                .FirstOrDefault(capCodec =>
                    MatchCodecs(codec, capCodec, true, true));

            codecToCapCodec[codec] = matchedCapCodec ??
                                     throw new NotSupportedException(
                                         $"Unsupported codec[mimeType:{codec.MimeType}, payloadType:{codec.PayloadType}, Channels:{codec.Channels}]");
        }

        // Match parameters RTX codecs to capabilities RTX codecs.
        foreach (var codec in parameters.Codecs)
        {
            if (!IsRtxCodec(codec))
            {
                continue;
            }

            // Search for the associated media codec.
            var associatedMediaCodec = parameters.Codecs
                .FirstOrDefault(mediaCodec => mediaCodec.PayloadType == (int)codec.Parameters!["apt"]!
                    // MatchCodecsWithPayloadTypeAndApt(mediaCodec.PayloadType, codec.Parameters)
                );

            if (associatedMediaCodec == null)
            {
                throw new TypeError(
                    $"missing media codec found for RTX PT {codec.PayloadType}");
            }

            var capMediaCodec = codecToCapCodec[associatedMediaCodec];

            // Ensure that the capabilities media codec has a RTX codec.
            var associatedCapRtxCodec = caps.Codecs!
                .FirstOrDefault(capCodec =>
                    IsRtxCodec(capCodec) &&
                    (int)codec.Parameters!["apt"]! == capCodec.PreferredPayloadType);

            codecToCapCodec[codec] = associatedCapRtxCodec ??
                                     throw new NotSupportedException(
                                         $"no RTX codec for capability codec PT {capMediaCodec.PreferredPayloadType}");
        }

        // Generate codecs mapping.
        foreach (var (codec, capCodec) in codecToCapCodec)
        {
            rtpMapping.Codecs.Add(new RtpMappingCodec
            {
                PayloadType       = codec.PayloadType,
                MappedPayloadType = capCodec.PreferredPayloadType!.Value,
            });
        }

        ;

        // Generate encodings mapping.
        var mappedSsrc = GenerateRandomNumber();

        foreach (var encoding in parameters.Encodings!)
        {
            var mappedEncoding = new RtpMappingEncoding
            {
                MappedSsrc      = mappedSsrc++,
                Rid             = encoding.Rid,
                Ssrc            = encoding.Ssrc,
                ScalabilityMode = encoding.ScalabilityMode,
            };

            rtpMapping.Encodings.Add(mappedEncoding);
        }

        return rtpMapping;
    }

    /// <summary>
    /// Generate RTP parameters to be internally used by Consumers given the RTP
    /// parameters in a Producer and the RTP capabilities in the Router.
    /// </summary>
    public static RtpParameters.RtpParameters GetConsumableRtpParameters(
        string kind,
        RtpParameters.RtpParameters parameters,
        RtpCapabilities caps,
        RtpMapping rtpMapping)
    {
        var consumableParams = new RtpParameters.RtpParameters
        {
            Codecs           = new(),
            HeaderExtensions = new(),
            Encodings        = new(),
            Rtcp             = new(),
        };

        foreach (var codec in parameters.Codecs)
        {
            if (IsRtxCodec(codec))
            {
                continue;
            }

            var consumableCodecPt = rtpMapping.Codecs
                .FirstOrDefault(entry => entry.PayloadType == codec.PayloadType)!
                .MappedPayloadType;

            var matchedCapCodec = caps.Codecs!
                .FirstOrDefault(capCodec => capCodec.PreferredPayloadType == consumableCodecPt)!;

            var consumableCodec = new RtpCodecParameters
            {
                MimeType     = matchedCapCodec!.MimeType,
                PayloadType  = matchedCapCodec.PreferredPayloadType!.Value,
                ClockRate    = matchedCapCodec.ClockRate,
                Channels     = matchedCapCodec.Channels,
                Parameters   = codec.Parameters, // Keep the Producer codec parameters.
                RtcpFeedback = matchedCapCodec.RtcpFeedback
            };

            consumableParams.Codecs.Add(consumableCodec);

            var consumableCapRtxCodec = caps.Codecs!
                .FirstOrDefault(capRtxCodec =>
                    IsRtxCodec(capRtxCodec) &&
                    (int)capRtxCodec.Parameters!["apt"]! == consumableCodec.PayloadType);

            if (consumableCapRtxCodec != null)
            {
                var consumableRtxCodec = new RtpCodecParameters
                {
                    MimeType     = consumableCapRtxCodec.MimeType,
                    PayloadType  = consumableCapRtxCodec.PreferredPayloadType!.Value,
                    ClockRate    = consumableCapRtxCodec.ClockRate,
                    Parameters   = consumableCapRtxCodec.Parameters, // Keep the Producer codec parameters.
                    RtcpFeedback = consumableCapRtxCodec.RtcpFeedback
                };

                consumableParams.Codecs.Add(consumableRtxCodec);
            }
        }

        foreach (var capExt in caps.HeaderExtensions!)
        {
            // Just take RTP header extension that can be used in Consumers.
            if (capExt.Kind.ToString() == kind ||
                capExt.Direction != RtpHeaderExtensionDirection.sendrecv &&
                capExt.Direction != RtpHeaderExtensionDirection.sendonly)
            {
                continue;
            }

            var consumableExt = new RtpHeaderExtensionParameters
            {
                Uri        = capExt.Uri,
                Id         = capExt.PreferredId,
                Encrypt    = capExt.PreferredEncrypt,
                Parameters = new ExpandoObject(),
            };

            consumableParams.HeaderExtensions.Add(consumableExt);
        }

        // Clone Producer encodings since we'll mangle them.
        var consumableEncodings = parameters.Encodings!.DeepClone();

        for (var i = 0; i < consumableEncodings.Count; ++i)
        {
            var consumableEncoding = consumableEncodings[i];
            var mappedSsrc         = rtpMapping.Encodings[i].MappedSsrc;

            // Remove useless fields.
            // 在 Node.js 实现中，rid, rtx, codecPayloadType 被 delete 了。
            consumableEncoding.Rid              = null;
            consumableEncoding.Rtx              = null;
            consumableEncoding.CodecPayloadType = null;

            // Set the mapped ssrc.
            consumableEncoding.Ssrc = mappedSsrc;

            consumableParams.Encodings.Add(consumableEncoding);
        }

        consumableParams.Rtcp = new RtcpParameters
        {
            Cname       = parameters.Rtcp!.Cname,
            ReducedSize = true,
            Mux         = true,
        };

        return consumableParams;
    }

    /// <summary>
    /// Check whether the given RTP capabilities can consume the given Producer.
    /// </summary>
    public static bool CanConsume(RtpParameters.RtpParameters consumableParams, RtpCapabilities caps)
    {
        // This may throw.
        ValidateRtpCapabilities(caps);

        var matchingCodecs = new List<RtpCodecParameters>();

        foreach (var codec in consumableParams.Codecs)
        {
            var matchedCapCodec = caps.Codecs!
                .FirstOrDefault(capCodec => MatchCodecs(capCodec, codec, true));

            if (matchedCapCodec == null)
            {
                continue;
            }

            matchingCodecs.Add(codec);
        }

        // Ensure there is at least one media codec.
        return matchingCodecs.Count != 0 && !IsRtxCodec(matchingCodecs[0]);
    }

    /// <summary>
    /// Generate RTP parameters for a specific Consumer.
    ///
    /// It reduces encodings to just one and takes into account given RTP capabilities
    /// to reduce codecs, codecs' RTCP feedback and header extensions, and also enables
    /// or disabled RTX.
    /// </summary>
    public static RtpParameters.RtpParameters GetConsumerRtpParameters(
        RtpParameters.RtpParameters consumableParams,
        RtpCapabilities remoteRtpCapabilities,
        bool pipe,
        bool enableRtx)
    {
        var consumerParams = new RtpParameters.RtpParameters
        {
            Codecs           = new(),
            HeaderExtensions = new(),
            Encodings        = new(),
            Rtcp             = consumableParams.Rtcp
        };

        foreach (var capCodec in remoteRtpCapabilities.Codecs!)
        {
            ValidateRtpCodecCapability(capCodec);
        }

        var consumableCodecs = consumableParams.Codecs.DeepClone();

        var rtxSupported = false;

        foreach (var codec in consumableCodecs)
        {
            if (!enableRtx && IsRtxCodec(codec))
            {
                continue;
            }

            var matchedCapCodec = remoteRtpCapabilities.Codecs
                .FirstOrDefault(capCodec => MatchCodecs(capCodec, codec, true));

            if (matchedCapCodec == null)
            {
                continue;
            }

            codec.RtcpFeedback =
                matchedCapCodec.RtcpFeedback!
                    .Where(fb => enableRtx || fb.Type != "nack" || !fb.Parameter.IsNullOrEmpty()).ToList();

            consumerParams.Codecs.Add(codec);
        }

        // Must sanitize the list of matched codecs by removing useless RTX codecs.
        for (var idx = consumerParams.Codecs.Count - 1; idx >= 0; --idx)
        {
            var codec = consumerParams.Codecs[idx];

            if (IsRtxCodec(codec))
            {
                // Search for the associated media codec.
                var associatedMediaCodec = consumerParams.Codecs
                    .FirstOrDefault(mediaCodec => mediaCodec.PayloadType == (int)codec.Parameters!["apt"]!);

                if (associatedMediaCodec != null)
                {
                    rtxSupported = true;
                }
                else
                {
                    consumerParams.Codecs.RemoveRange(idx, 1);
                }
            }
        }

        // Ensure there is at least one media codec.
        if (consumerParams.Codecs.Count == 0 || IsRtxCodec(consumerParams.Codecs[0]))
        {
            throw new NotSupportedException("no compatible media codecs");
        }

        consumerParams.HeaderExtensions = consumableParams.HeaderExtensions!
            .Where(ext =>
                remoteRtpCapabilities.HeaderExtensions!
                    .Any(capExt =>
                        capExt.PreferredId == ext.Id
                        && capExt.Uri      == ext.Uri)
            ).ToList();


        // Reduce codecs' RTCP feedback. Use Transport-CC if available, REMB otherwise.
        if (consumerParams.HeaderExtensions.Any(ext =>
                ext.Uri == "http://www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01"))
        {
            foreach (var codec in consumerParams.Codecs)
            {
                codec.RtcpFeedback = codec.RtcpFeedback!
                    .Where(fb => fb.Type != "goog-remb").ToList();
            }
        }
        else if (consumerParams.HeaderExtensions.Any(ext =>
                     ext.Uri == "http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time"))
        {
            foreach (var codec in consumerParams.Codecs)
            {
                codec.RtcpFeedback = codec.RtcpFeedback!
                    .Where(fb => fb.Type != "transport-cc")
                    .ToList();
            }
        }
        else
        {
            foreach (var codec in consumerParams.Codecs)
            {
                codec.RtcpFeedback = codec.RtcpFeedback!
                    .Where(fb =>
                        fb.Type is not "transport-cc"
                            and not "goog-remb")
                    .ToList();
            }
        }

        if (!pipe)
        {
            var consumerEncoding = new RtpEncodingParameters
            {
                Ssrc = GenerateRandomNumber()
            };

            if (rtxSupported)
            {
                // TODO : Naming
                consumerEncoding.Rtx = new RtpEncodingParameters.RTX(consumerEncoding.Ssrc.Value + 1);
            }

            // If any in the consumableParams.Encodings has scalabilityMode, process it
            // (assume all encodings have the same value).
            var encodingWithScalabilityMode =
                consumableParams.Encodings!.FirstOrDefault(encoding => !encoding.ScalabilityMode.IsNullOrEmpty());

            var scalabilityMode = encodingWithScalabilityMode?.ScalabilityMode;

            // If there is simulast, mangle spatial layers in scalabilityMode.
            if (consumableParams.Encodings!.Count > 1)
            {
                var scalabilityModeObject = ScalabilityMode.Parse(scalabilityMode!);

                scalabilityMode = $"L{consumableParams.Encodings.Count}T{scalabilityModeObject.TemporalLayers}";
            }

            if (!scalabilityMode.IsNullOrEmpty())
            {
                consumerEncoding.ScalabilityMode = scalabilityMode;
            }

            // Use the maximum maxBitrate in any encoding and honor it in the Consumer's
            // encoding.
            var maxEncodingMaxBitrate =
                consumableParams.Encodings!.Aggregate(0, (maxBitrate, encoding) => (
                    encoding.MaxBitrate != null && encoding.MaxBitrate > maxBitrate
                        ? encoding.MaxBitrate.Value
                        : maxBitrate
                ));


            if (maxEncodingMaxBitrate > 0)
            {
                consumerEncoding.MaxBitrate = maxEncodingMaxBitrate;
            }

            // Set a single encoding for the Consumer.
            consumerParams.Encodings.Add(consumerEncoding);
        }
        else
        {
            var consumableEncodings = consumableParams.Encodings.DeepClone();
            var baseSsrc            = GenerateRandomNumber();
            var baseRtxSsrc         = GenerateRandomNumber();

            for (var i = 0; i < consumableEncodings!.Count; ++i)
            {
                var encoding = consumableEncodings[i];
                encoding.Ssrc = baseSsrc + i;

                encoding.Rtx = rtxSupported ? new RtpEncodingParameters.RTX(baseRtxSsrc + i) : null;

                consumerParams.Encodings!.Add(encoding);
            }
        }

        return consumerParams;
    }

    /// <summary>
    /// Generate RTP parameters for a pipe Consumer.
    ///
    /// It keeps all original consumable encodings and removes support for BWE. If
    /// enableRtx is false, it also removes RTX and NACK support.
    /// </summary>
    public static RtpParameters.RtpParameters GetPipeConsumerRtpParameters(
        RtpParameters.RtpParameters consumableParams,
        bool enableRtx = false)
    {
        var consumerParams = new RtpParameters.RtpParameters
        {
            Codecs           = new(),
            HeaderExtensions = new(),
            Encodings        = new(),
            Rtcp             = consumableParams.Rtcp
        };

        var consumableCodecs = consumableParams.Codecs.DeepClone();

        foreach (var codec in consumableCodecs)
        {
            if (!enableRtx && IsRtxCodec(codec))
            {
                continue;
            }

            codec.RtcpFeedback = codec.RtcpFeedback!
                .Where(fb =>
                    fb is { Type: "nack", Parameter: "pli" } ||
                    fb is { Type: "ccm", Parameter : "fir" } ||
                    enableRtx && fb.Type == "nack" && fb.Parameter.IsNullOrEmpty()
                ).ToList();

            consumerParams.Codecs.Add(codec);
        }

        // Reduce RTP extensions by disabling transport MID and BWE related ones.
        consumerParams.HeaderExtensions = consumableParams.HeaderExtensions!
            .Where(ext =>
                ext.Uri is not "urn:ietf:parameters:rtp-hdrext:sdes:mid"
                    and not "http://www.webrtc.org/experiments/rtp-hdrext/abs-send-time"
                    and not "http://www.ietf.org/id/draft-holmer-rmcat-transport-wide-cc-extensions-01"
            ).ToList();

        var consumableEncodings = consumableParams.Encodings!.DeepClone();

        var baseSsrc    = GenerateRandomNumber();
        var baseRtxSsrc = GenerateRandomNumber();

        for (var i = 0; i < consumableEncodings.Count; ++i)
        {
            var encoding = consumableEncodings[i];

            encoding.Ssrc = baseSsrc + i;

            encoding.Rtx = enableRtx ? new RtpEncodingParameters.RTX(baseRtxSsrc + i) : null;

            consumerParams.Encodings.Add(encoding);
        }

        return consumerParams;
    }

    private static bool IsRtxCodec(RtpCodec codec) => RtxMimeTypeRegex.IsMatch(codec.MimeType);


    private static bool MatchCodecs(RtpCodec aCodec, RtpCodec bCodec, bool strict = false, bool modify = false)
    {
        var aMimeType = aCodec.MimeType.ToLower();
        var bMimeType = bCodec.MimeType.ToLower();

        if (aMimeType           != bMimeType
            || aCodec.ClockRate != bCodec.ClockRate
            || aCodec.Channels  != bCodec.Channels)
        {
            return false;
        }

        // Per codec special checks.
        switch (aMimeType)
        {
            case "audio/multiopus":
            {
                var aNumStreams = aCodec.Parameters!["num_streams"];
                var bNumStreams = bCodec.Parameters!["num_streams"];

                if (aNumStreams != bNumStreams)
                {
                    return false;
                }

                var aCoupledStreams = aCodec.Parameters["coupled_streams"];
                var bCoupledStreams = bCodec.Parameters["coupled_streams"];

                if (aCoupledStreams != bCoupledStreams)
                {
                    return false;
                }

                break;
            }
            case "video/h264":
            case "video/h264-svc":
            {
                // If strict matching check profile-level-id.
                if (strict)
                {
                    var aPacketizationMode = aCodec.Parameters!["packetization-mode"] ?? 0;
                    var bPacketizationMode = bCodec.Parameters!["packetization-mode"] ?? 0;


                    if (aPacketizationMode != bPacketizationMode)
                    {
                        return false;
                    }

                    if (!H264ProfileLevelId.H264ProfileLevelId.IsSameProfile(aCodec.Parameters, bCodec.Parameters))
                    {
                        return false;
                    }

                    string? selectedProfileLevelId;

                    try
                    {
                        selectedProfileLevelId =
                            H264ProfileLevelId.H264ProfileLevelId.GenerateProfileLevelIdForAnswer(aCodec.Parameters,
                                bCodec.Parameters);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"MatchCodecs() | {ex.Message}");
                        return false;
                    }

                    if (modify)
                    {
                        if (!selectedProfileLevelId.IsNullOrEmpty())
                        {
                            aCodec.Parameters["profile-level-id"] = selectedProfileLevelId!;
                        }
                        else
                        {
                            aCodec.Parameters.Remove("profile-level-id");
                        }
                    }
                }

                break;
            }
            case "video/vp9":
            {
                if (strict)
                {
                    var aProfileId = aCodec.Parameters!["profile-id"] ?? 0;
                    var bProfileId = bCodec.Parameters!["profile-id"] ?? 0;

                    if (aProfileId != bProfileId)
                    {
                        return false;
                    }
                }

                break;
            }
        }

        return true;
    }
}