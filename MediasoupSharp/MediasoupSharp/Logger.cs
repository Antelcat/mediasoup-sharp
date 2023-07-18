namespace MediasoupSharp;

public class Loggable
{
    private readonly string format;
    public Loggable(string format) => this.format = format;
    public void Log(string msg) => Console.WriteLine(format, msg);
}

public class Logger
{
    const string AppName = "mediasoup";
    public Loggable Debug { get; }
    public Loggable Warn { get; }
    public Loggable Error { get; }

    public Logger(string? prefix)
    {
        if (prefix != null)
        {
            Debug = new Loggable($"{AppName}:${prefix}");
            Warn = new Loggable($"{AppName}:WARN:${prefix}");
            Error = new Loggable($"{AppName}:ERROR:${prefix}");
        }
        else
        {
            Debug = new Loggable(AppName);
            Warn = new Loggable($"{AppName}:WARN");
            Error = new Loggable($"{AppName}:ERROR");
        }
    }
}