namespace MediasoupSharp.Producer;

public record ProducerScore
{
    /// <summary>
    /// SSRC of the RTP stream.
    /// </summary>
    /// <returns></returns>
    public int Ssrc;

    /// <summary>
    /// RID of the RTP stream.
    /// </summary>
    /// <returns></returns>
    public string? Rid { get; set; }

    /// <summary>
    /// The score of the RTP stream.
    /// </summary>
    /// <returns></returns>
    public int Score { get; set; }
}