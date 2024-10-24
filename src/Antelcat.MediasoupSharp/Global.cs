global using static Antelcat.MediasoupSharp.Utils;
global using JsonStringEnumMemberConverter = System.Text.Json.Serialization.JsonStringEnumConverter;
global using Newtonsoft = System.Text;
using Antelcat.FlatBuffers;

[assembly: FlatcArguments("--cs-global-alias", "--gen-object-api", "--gen-onefile")]