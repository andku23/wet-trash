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
    public ItemType itemType;
    public GameObject model;
    public int spawnRateShallow;
    public int spawnRateDeep;
    public int price;
    public float weight;
    public Sprite hotbarIcon;
}

public enum ItemType
{
    Default = 0,
    Loot = 1,
    UsableItem = 2,
}
