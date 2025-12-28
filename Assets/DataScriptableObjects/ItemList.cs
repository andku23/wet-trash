using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "LootList", menuName = "Scriptable Objects/LootList")]
public class ItemList : ScriptableObject
{
    public GameObject networkLootPrefab;
    public GameObject localLootPrefab;
    [FormerlySerializedAs("itemData")] [FormerlySerializedAs("pairs")] public ItemData[] itemDatas;
    public BiomeLootSpawnProbability[] biomeLootSpawnProbability;
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
    DeadPlayer = 3
}

public enum HoldableHandType
{
    Center = 0,
    Left = 1,
    Right = 2,
}
