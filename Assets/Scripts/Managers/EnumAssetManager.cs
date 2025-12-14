using System.Collections.Generic;
using UnityEngine;

public class EnumAssetManager : MonoBehaviour
{
    [SerializeField] private EnumAssetPairing enumAssetPairing;

    public Dictionary<ItemInteractionType, Sprite> itemInteractionLookup;

    public static EnumAssetManager Instance;

    private void Start()
    {
        Instance = this;

        itemInteractionLookup = new Dictionary<ItemInteractionType, Sprite>();
        foreach (ItemInteractionPairing pair in enumAssetPairing.ItemInteractionPairings)
        {
            itemInteractionLookup.Add(pair.interactionType, pair.sprite);
        }
    }
}
