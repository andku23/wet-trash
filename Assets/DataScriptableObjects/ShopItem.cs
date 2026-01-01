using UnityEngine;

[CreateAssetMenu(fileName = "ShopItem", menuName = "ShopItem")]
public class ShopItem : ScriptableObject
{
    public string id;
    public ShopItemType type;
    public string name;
    //public int price;
    public CurrencyValuePair[] cost;
    public int itemListIndex;
    public GameObject placePrefab;
    public Sprite icon;
}
