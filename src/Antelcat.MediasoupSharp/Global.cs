global using static Antelcat.MediasoupSharp.Utils;
global using Antelcat.MediasoupSharp.FBS;
using Antelcat.FlatBuffers;

[assembly: FlatcArguments("--cs-global-alias", "--gen-object-api", "--gen-onefile")]
[assembly: FlatcReplaces("namespace FBS", $"namespace {nameof(Antelcat)}.MediasoupSharp.FBS")]
[assembly: FlatcReplaces("global::FBS", $"global::{nameof(Antelcat)}.MediasoupSharp.FBS")]

