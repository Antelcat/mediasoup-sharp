﻿namespace MediasoupSharp.PayloadChannel;

public class OngoingNotification
{
    public string TargetId { get; set; }

    public string Event { get; set; }

    public string? Data { get; set; }
}