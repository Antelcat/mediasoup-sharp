namespace MediasoupSharp;

public class Loggable
{
    private readonly string format;
    public Loggable(string format) => this.format = format;
    public void log(string msg) => Console.WriteLine(format, msg);
}

public class Logger
{
    const string APP_NAME = "mediasoup";
    public Loggable debug { get; }
    public Loggable warn { get; }
    public Loggable error { get; }

    public Logger(string? prefix)
    {
        if (prefix != null)
        {
            debug = new Loggable($"{APP_NAME}:${prefix}");
            warn = new Loggable($"{APP_NAME}:WARN:${prefix}");
            error = new Loggable($"{APP_NAME}:ERROR:${prefix}");
        }
        else
        {
            debug = new Loggable(APP_NAME);
            warn = new Loggable($"{APP_NAME}:WARN");
            error = new Loggable($"{APP_NAME}:ERROR");
        }
    }
}