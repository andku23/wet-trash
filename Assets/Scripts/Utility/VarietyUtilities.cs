
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
}
