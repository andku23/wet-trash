using UnityEngine;

[CreateAssetMenu(fileName = "EnumAssetPairing", menuName = "Scriptable Objects/EnumAssetPairing")]
public class EnumAssetPairing : ScriptableObject
{
    public ItemInteractionPairing[] ItemInteractionPairings;
}

[System.Serializable]
public class ItemInteractionPairing
{
    public ItemInteractionType interactionType;
    public Sprite sprite;
}
