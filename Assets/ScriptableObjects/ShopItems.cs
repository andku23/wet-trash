using UnityEngine;

[CreateAssetMenu(fileName = "ShopItems", menuName = "Scriptable Objects/ShopItems")]
public class ShopItems : ScriptableObject
{
    public ShopItem[] items;
}

[System.Serializable]
public class ShopItem
{
    public string id;
    public ShopItemType type;
    public string name;
    public int price;
}

public enum ShopItemType
{
    Default = 0,
    PlayerPowerUp = 1,
    InventoryItem = 2
}
