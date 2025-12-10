using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "LootList", menuName = "Scriptable Objects/LootList")]
public class ItemList : ScriptableObject
{
    public GameObject networkLootPrefab;
    public GameObject localLootPrefab;
    [FormerlySerializedAs("pairs")] public ItemData[] itemData;
    public BiomeLootSpawnProbability[] biomeLootSpawnProbability;
}

[System.Serializable]
public class ItemData
{
    public ItemType itemType;
    public GameObject model;
    public int spawnRateCommon;
    public int spawnRateRare;
    public int spawnRateVeryRare;
    public int price;
    public float weight;
    public Sprite hotbarIcon;
    public HoldableHandType holdableHandType;
}

[System.Serializable]
public class BiomeLootSpawnProbability
{
    public WorldManager.BiomeType biomeType;
    public int probability;
}

public enum ItemType
{
    Default = 0,
    Loot = 1,
    UsableItem = 2,
}

public enum HoldableHandType
{
    Center = 0,
    Left = 1,
    Right = 2,
}
