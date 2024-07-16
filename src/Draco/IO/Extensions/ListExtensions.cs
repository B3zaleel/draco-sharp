namespace Draco.IO.Extensions;

internal static class ListExtensions
{
    public static void PopBack<T>(this List<T> list)
    {
        list.RemoveAt(list.Count - 1);
    }

    public static void Fill<T>(this List<T> list, int count, Func<T> elementCreator)
    {
        list.Clear();
        for (var i = 0; i < count; i++)
        {
            list.Add(elementCreator());
        }
    }

    public static void Fill<T>(this List<T> list, int count, T value)
    {
        list.Clear();
        list.AddRange(Enumerable.Repeat(value, count));
    }

    public static void Resize<T>(this List<T> list, int count, T value)
    {
        if (list.Count < count)
        {
            list.AddRange(Enumerable.Repeat(value, count - list.Count));
        }
        else if (list.Count > count)
        {
            list.RemoveRange(count, list.Count - count);
        }
    }

    public static void Resize<T>(this List<T> list, int count, Func<T> elementCreator)
    {
        if (list.Count < count)
        {
            for (var i = 0; i < count; i++)
            {
                list.Add(elementCreator());
            }
        }
        else if (list.Count > count)
        {
            list.RemoveRange(count, list.Count - count);
        }
    }
}
