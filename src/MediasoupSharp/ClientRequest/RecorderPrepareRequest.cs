﻿namespace MediasoupSharp.ClientRequest;

public class RecorderPrepareRequest
{
    public string PeerId { get; set; }

    public string RoomId { get; set; }

    public string ProducerPeerId { get; set; }

    public string[] ProducerSources { get; set; }
}