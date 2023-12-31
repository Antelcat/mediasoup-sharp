﻿// ReSharper disable InconsistentNaming

using System.Text.Json.Serialization;

namespace MediasoupSharp.Consumer;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConsumerType
{
    simple = 1,
    simulcast = 2,
    svc = 3,
    pipe
}