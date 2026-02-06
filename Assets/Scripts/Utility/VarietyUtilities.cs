
using System.Collections.Generic;

public class VarietyUtilities
{
    public static int GetGreatestIndexInArray(float[] array)
    {
        int index = 0;
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] > array[index])
                index = i;
        }
        return index;
    }

    public static Dictionary<CurrencyType, int> GetInitializedCurrencyDictionary()
    {
        Dictionary<CurrencyType, int> dict = new Dictionary<CurrencyType, int>();
        string[] PieceTypeNames = System.Enum.GetNames(typeof(CurrencyType));

        for(int i = 0; i < PieceTypeNames.Length; i++){
            dict.Add((CurrencyType)i, 0);
        }

        return dict;
    }
    
    public static List<int> GetInitializedCurrencyList()
    {
        List<int> list = new List<int>();
        string[] PieceTypeNames = System.Enum.GetNames(typeof(CurrencyType));

        for(int i = 0; i < PieceTypeNames.Length; i++){
            list.Add(0);
        }

        return list;
    }
    
    public static int[] GetInitializedCurrencyArray()
    {
        string[] PieceTypeNames = System.Enum.GetNames(typeof(CurrencyType));
        int[] array = new int[PieceTypeNames.Length];

        return array;
    }
    
    public static int GetInitializedCurrencySize()
    {
        string[] PieceTypeNames = System.Enum.GetNames(typeof(CurrencyType));
        return PieceTypeNames.Length;
    }
    
    public static void Shuffle<T>(List<T> ts, int startIndex) {
        var count = ts.Count;
        var last = count - 1;
        for (var i = startIndex; i < last; ++i) {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }
    
    public static void Shuffle<T>(T[] ts, int startIndex) {
        var count = ts.Length;
        var last = count - 1;
        for (var i = startIndex; i < last; ++i) {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }
}
