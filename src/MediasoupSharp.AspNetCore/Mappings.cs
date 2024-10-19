using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using MediasoupSharp.Settings;

namespace MediasoupSharp.AspNetCore;
[AutoMetadataFrom(typeof(WorkerSettings), MemberTypes.Property,
    Leading = """
              public static global::MediasoupSharp.Settings.WorkerSettings Apply(
              this global::MediasoupSharp.Settings.WorkerSettings current,
              global::MediasoupSharp.Settings.WorkerSettings another
              ){ return current with {
              """,
    Template = """
               #if {CanWrite}
               {Name} = another.{Name} ?? current.{Name},
               #endif
               
               """,
    Trailing = "};}")]
[AutoMetadataFrom(typeof(WebRtcTransportSettings), MemberTypes.Property,
    Leading = """
              public static global::MediasoupSharp.Settings.WebRtcTransportSettings Apply(
              this global::MediasoupSharp.Settings.WebRtcTransportSettings current,
              global::MediasoupSharp.Settings.WebRtcTransportSettings another
              ){ return current with {
              """,
    Template = """
               #if {CanWrite}
               {Name} = another.{Name} ?? current.{Name},
               #endif

               """,
    Trailing = "};}")]
[AutoMetadataFrom(typeof(PlainTransportSettings), MemberTypes.Property,
    Leading = """
              public static global::MediasoupSharp.Settings.PlainTransportSettings Apply(
              this global::MediasoupSharp.Settings.PlainTransportSettings current,
              global::MediasoupSharp.Settings.PlainTransportSettings another
              ){ return current with {
              """,
    Template = """
               #if {CanWrite}
               {Name} = another.{Name} ?? current.{Name},
               #endif

               """,
    Trailing = "};}")]
[AutoMetadataFrom(typeof(MediasoupStartupSettings), MemberTypes.Property,
    Leading = """
              public static global::MediasoupSharp.Settings.MediasoupStartupSettings Apply(
              this global::MediasoupSharp.Settings.MediasoupStartupSettings current,
              global::MediasoupSharp.Settings.MediasoupStartupSettings another
              ){ return current with {
              """,
    Template = """
               #if {CanWrite}
               {Name} = another.{Name} ?? current.{Name},
               #endif

               """,
    Trailing = "};}")]
internal static partial class Mappings;
