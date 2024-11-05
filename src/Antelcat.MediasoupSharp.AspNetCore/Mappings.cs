using System.Reflection;
using Antelcat.AutoGen.ComponentModel.Diagnostic;

namespace Antelcat.MediasoupSharp.AspNetCore;

[AutoMetadataFrom(typeof(WorkerSettings<object>), MemberTypes.Property,
    BindingFlags = BindingFlags.Public,
    Leading = """
              public static global::Antelcat.MediasoupSharp.WorkerSettings<T> Apply<T>(
              this global::Antelcat.MediasoupSharp.WorkerSettings<T> current,
              global::Antelcat.MediasoupSharp.WorkerSettings<T> another
              ){ return current with {
              """,
    Template = """
               {Name} = another.{Name} ?? current.{Name},
               
               """,
    Trailing = "};}")]
[AutoMetadataFrom(typeof(WebRtcTransportOptions<object>), MemberTypes.Property,
    BindingFlags = BindingFlags.Public,
    Leading = """
              public static global::Antelcat.MediasoupSharp.WebRtcTransportOptions<T> Apply<T>(
              this global::Antelcat.MediasoupSharp.WebRtcTransportOptions<T> current,
              global::Antelcat.MediasoupSharp.WebRtcTransportOptions<T> another
              ){ return current with {
              """,
    Template = """
               {Name} = another.{Name} ?? current.{Name},

               """,
    Trailing = "};}")]
[AutoMetadataFrom(typeof(PlainTransportOptions<object>), MemberTypes.Property, 
    BindingFlags = BindingFlags.Public,
    Leading = """
              public static global::Antelcat.MediasoupSharp.PlainTransportOptions<T> Apply<T>(
              this global::Antelcat.MediasoupSharp.PlainTransportOptions<T> current,
              global::Antelcat.MediasoupSharp.PlainTransportOptions<T> another
              ){ return current with {
              """,
    Template = """
               {Name} = another.{Name} ?? current.{Name},

               """,
    Trailing = "};}")]
internal static partial class Mappings;
