namespace Antelcat.MediasoupSharp.Exceptions;

public class EnumNotMappedException(Type enumType) : Exception($"{enumType} not mapped")
{
    public Type Type => enumType;
}
