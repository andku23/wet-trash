using UnityEngine;

[CreateAssetMenu(fileName = "LootList", menuName = "Scriptable Objects/LootList")]
public class ItemList : ScriptableObject
{
    public GameObject networkLootPrefab;
    public GameObject localLootPrefab;
    public ItemData[] pairs;
}

[System.Serializable]
public class ItemData
{
    public LootType lootType;
    public GameObject model;
    public int spawnRateShallow;
    public int spawnRateDeep;
    public int price;
    public float weight;
    public float weightSpeedMultiplier;
    public Sprite hotbarIcon;
}

public enum LootType
{
    Default = 0,
    Special = 1,
    Heavy = 2,
}
