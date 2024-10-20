namespace MediasoupSharp.RtpParameters;

/// <summary>
/// Direction of RTP header extension.
/// </summary>
public enum RtpHeaderExtensionDirection
{
    SendReceive,
    SendOnly,
    ReceiveOnly,
    Inactive
}