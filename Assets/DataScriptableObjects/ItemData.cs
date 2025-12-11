using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "ItemData")]
public class ItemData : ScriptableObject
{
    public ItemType itemType;
    public GameObject model;
    public int spawnRateCommon;
    public int spawnRateRare;
    public int spawnRateVeryRare;
    //public int price;
    public ValueRange[] valueRange; // Range of all the values
    public float weight;
    public Sprite hotbarIcon;
    public HoldableHandType holdableHandType;
}

[System.Serializable]
public class ValueRange
{
    public CurrencyType CurrencyType;
    public int MinValue = 100;
    public int MaxValue = 200;
}

public enum CurrencyType
{
    Scrap,
    Ore,
    Fish
}


