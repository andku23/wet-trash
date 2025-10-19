using UnityEngine;

[CreateAssetMenu(fileName = "ShopItems", menuName = "Scriptable Objects/ShopItems")]
public class ShopItems : ScriptableObject
{
    public ShopItem[] items;
}

[System.Serializable]
public class ShopItem
{
    public int id;
    public string name;
    public int price;
}
