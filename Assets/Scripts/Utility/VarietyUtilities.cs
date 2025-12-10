
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
}
