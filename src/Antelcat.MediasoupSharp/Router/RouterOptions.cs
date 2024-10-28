﻿using Antelcat.MediasoupSharp.RtpParameters;

namespace Antelcat.MediasoupSharp.Router;

public class RouterOptions
{
    /// <summary>
    /// Router media codecs.
    /// </summary>
    public RtpCodecCapability[]? MediaCodecs { get; set; }

    /// <summary>
    /// Custom application data.
    /// </summary>
    public Dictionary<string, object>? AppData { get; set; }
}