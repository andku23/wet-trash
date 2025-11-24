using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BoatPartsList", menuName = "Scriptable Objects/BoatPartsList")]
public class BoatPartsList : ScriptableObject
{
    public List<BoatPartData> boatParts;
}

[System.Serializable]
public class BoatPartData
{
    public BoatPartID id;
    public GameObject prefab;
}

public enum BoatPartID
{
    BasicPlatform = 0,
}
