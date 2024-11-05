namespace Antelcat.MediasoupSharp;

public class EnumNotMappedException(Type enumType) : Exception($"{enumType} not mapped")
{
    public Type Type => enumType;
}

public class InvalidStateException : Exception
{
    public InvalidStateException(string message) : base(message)
    {
    }

    public InvalidStateException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public InvalidStateException()
    {
    }
}