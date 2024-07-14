namespace Draco.IO.Extensions;

internal static class Assertions
{
    public static void Throw(string reason = "")
    {
        throw new InvalidDataException(reason);
    }

    public static void ThrowIf(bool condition, string reason = "")
    {
        if (condition)
        {
            throw new InvalidDataException(reason);
        }
    }

    public static void ThrowIfNot(bool condition, string reason = "")
    {
        if (!condition)
        {
            throw new InvalidDataException(reason);
        }
    }
}
