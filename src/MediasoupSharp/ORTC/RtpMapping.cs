﻿namespace MediasoupSharp.ORTC;

public class RtpMapping
{
    public List<RtpMappingCodec> Codecs { get; set; }

    public List<RtpMappingEncoding> Encodings { get; set; }
}

public class RtpMappingCodec
{
    public int PayloadType { get; set; }

    public int MappedPayloadType { get; set; }
}

public class RtpMappingEncoding
{
    public int? Ssrc { get; set; }

    public string? Rid { get; set; }

    public string? ScalabilityMode { get; set; }

    public int MappedSsrc { get; set; }
}