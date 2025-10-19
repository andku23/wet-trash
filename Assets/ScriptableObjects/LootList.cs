using UnityEngine;

[CreateAssetMenu(fileName = "LootList", menuName = "Scriptable Objects/LootList")]
public class LootList : ScriptableObject
{
    public LootData[] pairs;
}

[System.Serializable]
public class LootData
{
    public LootType id;
    public GameObject network;
    public GameObject local;
    public int spawnRate;
}

public enum LootType
{
    Default = 0,
    Special = 1
}
