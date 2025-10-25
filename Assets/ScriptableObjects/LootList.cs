using UnityEngine;

[CreateAssetMenu(fileName = "LootList", menuName = "Scriptable Objects/LootList")]
public class LootList : ScriptableObject
{
    public GameObject networkLootPrefab;
    public GameObject localLootPrefab;
    public LootData[] pairs;
}

[System.Serializable]
public class LootData
{
    public LootType lootType;
    public GameObject model;
    public int spawnRate;
    public int price;
}

public enum LootType
{
    Default = 0,
    Special = 1,
    Heavy = 2,
}
