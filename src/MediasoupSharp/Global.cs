﻿global using static MediasoupSharp.Utils;
global using MediasoupSharp.Extensions;
global using JsonStringEnumMemberConverter = System.Text.Json.Serialization.JsonStringEnumConverter;
using Antelcat.FlatBuffers;

[assembly:FlatcLocation("../../tools/flatc.exe")]
[assembly:FlatcArguments("--cs-global-alias", "--gen-object-api")]
