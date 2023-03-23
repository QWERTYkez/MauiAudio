using System.Collections.ObjectModel;

namespace MauiAudio;

internal static class QuickSortExtenson
{
    private static void Swap(ObservableCollection<MediaContent> array, int x, int y) =>
        (array[y], array[x]) = (array[x], array[y]);

    private static int Partition(ObservableCollection<MediaContent> array, int minIndex, int maxIndex)
    {
        int x1 = minIndex - 1;
        for (int index = minIndex; index < maxIndex; ++index)
        {
            if (array[index].index < array[maxIndex].index)
            {
                ++x1;
                Swap(array, x1, index);
            }
        }
        int x2 = x1 + 1;
        Swap(array, x2, maxIndex);
        return x2;
    }

    private static ObservableCollection<MediaContent> QuickSort(
        ObservableCollection<MediaContent> array, int minIndex, int maxIndex)
    {
        if (minIndex >= maxIndex)
            return array;
        int num = Partition(array, minIndex, maxIndex);
        QuickSort(array, minIndex, num - 1);
        QuickSort(array, num + 1, maxIndex);
        return array;
    }

    public static void QuickSort(this ObservableCollection<MediaContent> array) =>
        QuickSort(array, 0, array.Count() - 1);
}
