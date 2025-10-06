using UnityEngine;

[CreateAssetMenu(fileName = "LootLocalReferences", menuName = "Scriptable Objects/LootLocalReferences")]
public class LootLocalReferences : ScriptableObject
{
    public LocalNetworkPrefabPair[] pairs;
}

[System.Serializable]
public class LocalNetworkPrefabPair
{
    public LootType id;
    public GameObject network;
    public GameObject local;
}

public enum LootType
{
    Default = 0
}
