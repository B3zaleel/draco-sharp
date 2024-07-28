namespace Draco.IO.Extensions;

internal static class ArrayExtensions
{
    public static T[] GetSubArray<T>(this T[] array, int start)
    {
        int length = array.Length - start;
        var subArray = new T[length];
        Array.Copy(array, start, subArray, 0, length);
        return subArray;
    }

    public static void SetSubArray<T>(this T[] array, T[] subArray, int offset)
    {
        for (int i = 0; i < subArray.Length; ++i)
        {
            array[offset + i] = subArray[i];
        }
    }
}
