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
[AutoMetadataFrom(typeof(WebRtcTransportOptions), MemberTypes.Property,
    BindingFlags = BindingFlags.Public,
    Leading = """
              public static global::Antelcat.MediasoupSharp.Settings.WebRtcTransportOptions Apply(
              this global::Antelcat.MediasoupSharp.Settings.WebRtcTransportOptions current,
              global::Antelcat.MediasoupSharp.Settings.WebRtcTransportOptions another
              ){ return current with {
              """,
    Template = """
               {Name} = another.{Name} ?? current.{Name},

               """,
    Trailing = "};}")]
[AutoMetadataFrom(typeof(PlainTransportOptions), MemberTypes.Property, 
    BindingFlags = BindingFlags.Public,
    Leading = """
              public static global::Antelcat.MediasoupSharp.Settings.PlainTransportOptions Apply(
              this global::Antelcat.MediasoupSharp.Settings.PlainTransportOptions current,
              global::Antelcat.MediasoupSharp.Settings.PlainTransportOptions another
              ){ return current with {
              """,
    Template = """
               {Name} = another.{Name} ?? current.{Name},

               """,
    Trailing = "};}")]
internal static partial class Mappings;
