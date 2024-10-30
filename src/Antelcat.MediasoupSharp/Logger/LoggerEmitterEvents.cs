// ReSharper disable InconsistentNaming
namespace Antelcat.MediasoupSharp.Logger;

public class LoggerEmitterEvents
{
    public (string, string)            debuglog;
    public (string, string)            warnlog;
    public (string, string, Exception) errorlog;
}