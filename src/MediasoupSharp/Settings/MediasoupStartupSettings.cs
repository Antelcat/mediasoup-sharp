﻿namespace MediasoupSharp.Settings;

public record MediasoupStartupSettings
{
    public string? MediasoupVersion { get; set; }

    public bool? WorkerInProcess { get; set; }

    public string? WorkerPath { get; set; }

    public int? NumberOfWorkers { get; set; }

    public bool? UseWebRtcServer { get; set; }
}