using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;
using Antelcat.MediasoupSharp.Settings;

namespace Antelcat.MediasoupSharp.AspNetCore;

[AutoMetadataFrom(typeof(WorkerSettings), MemberTypes.Property,
    BindingFlags = BindingFlags.Public,
    Leading = """
              public static global::Antelcat.MediasoupSharp.Settings.WorkerSettings Apply(
              this global::Antelcat.MediasoupSharp.Settings.WorkerSettings current,
              global::Antelcat.MediasoupSharp.Settings.WorkerSettings another
              ){ return current with {
              """,
    Template = """
               {Name} = another.{Name} ?? current.{Name},
               
               """,
    Trailing = "};}")]
[AutoMetadataFrom(typeof(WebRtcTransportSettings), MemberTypes.Property,
    BindingFlags = BindingFlags.Public,
    Leading = """
              public static global::Antelcat.MediasoupSharp.Settings.WebRtcTransportSettings Apply(
              this global::Antelcat.MediasoupSharp.Settings.WebRtcTransportSettings current,
              global::Antelcat.MediasoupSharp.Settings.WebRtcTransportSettings another
              ){ return current with {
              """,
    Template = """
               {Name} = another.{Name} ?? current.{Name},

               """,
    Trailing = "};}")]
[AutoMetadataFrom(typeof(PlainTransportSettings), MemberTypes.Property, 
    BindingFlags = BindingFlags.Public,
    Leading = """
              public static global::Antelcat.MediasoupSharp.Settings.PlainTransportSettings Apply(
              this global::Antelcat.MediasoupSharp.Settings.PlainTransportSettings current,
              global::Antelcat.MediasoupSharp.Settings.PlainTransportSettings another
              ){ return current with {
              """,
    Template = """
               {Name} = another.{Name} ?? current.{Name},

               """,
    Trailing = "};}")]
[AutoMetadataFrom(typeof(MediasoupStartupSettings), MemberTypes.Property,
    BindingFlags = BindingFlags.Public,
    Leading = """
              public static global::Antelcat.MediasoupSharp.Settings.MediasoupStartupSettings Apply(
              this global::Antelcat.MediasoupSharp.Settings.MediasoupStartupSettings current,
              global::Antelcat.MediasoupSharp.Settings.MediasoupStartupSettings another
              ){ return current with {
              """,
    Template = """
               {Name} = another.{Name} ?? current.{Name},

               """,
    Trailing = "};}")]
internal static partial class Mappings;
