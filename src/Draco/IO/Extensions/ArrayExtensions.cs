namespace Draco.IO.Extensions;

internal static class ArrayExtensions
{
    public static T[] GetSubArray<T>(this T[] array, int offset)
    {
        if (offset < 0 || offset >= array.Length)
        {
            return [];
        }
        int length = array.Length - offset;
        var subArray = new T[length];
        Array.Copy(array, offset, subArray, 0, length);
        return subArray;
    }

    public static T[] GetSubArray<T>(this T[] array, int offset, int count)
    {
        if (offset < 0 || count < 0 || offset + count > array.Length)
        {
            return [];
        }
        var subArray = new T[count];
        Array.Copy(array, offset, subArray, 0, count);
        return subArray;
    }

    public static void SetSubArray<T>(this T[] array, T[] subArray, int offset)
    {
        if (offset < 0 || offset >= array.Length || subArray.Length + offset > array.Length)
        {
            return;
        }
        for (int i = 0; i < subArray.Length; ++i)
        {
            array[offset + i] = subArray[i];
        }
    }
}
