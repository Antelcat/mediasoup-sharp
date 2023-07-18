using System.Reflection.Metadata.Ecma335;

namespace MediasoupSharp;

public abstract class AppData
{
    public abstract object? this[string key] { get; set; }
}
