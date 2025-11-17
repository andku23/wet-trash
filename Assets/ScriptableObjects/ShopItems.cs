using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(fileName = "ShopItems", menuName = "Scriptable Objects/ShopItems")]
public class ShopItems : ScriptableObject
{
    public ShopItem[] items;
    public ShopItemTypeToName[] itemsTypesToNames;
}

[System.Serializable]
public class ShopItem
{
    public string id;
    public ShopItemType type;
    public string name;
    public int price;
    public int itemListIndex;
    public GameObject placePrefab;
}

[System.Serializable]
public class ShopItemTypeToName
{
    public ShopItemType type;
    public string name;
}

public enum ShopItemType
{
    Default = 0,
    PlayerPowerUp = 1,
    InventoryItem = 2,
    BoatPart = 3
}
