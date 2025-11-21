using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class ItemInstance : MonoBehaviour, IHoldable, IInventorable
{
    public int ItemIndex;
    public HeldItemModel Model;
    
    [SerializeField] private HeldObjectType _heldObjectType;
    
    public HeldObjectType HeldObjectType { get => _heldObjectType; }
    public GameObject ConnectedParent { get; set; }
    
    public ulong HeldPlayerID { get; set; }
    
    public void LoadLocal(ItemData itemData, int lootIndex)
    {
        ItemIndex = lootIndex;
        GameObject model = Instantiate(itemData.model, transform);
        model.GetComponent<ColliderReference>().enabled = false;
        model.GetComponent<Collider>().enabled = false;
        Model = model.GetComponent<HeldItemModel>();
    }
    
    public void LoadNetwork(int lootIndex)
    {
        ItemIndex = lootIndex;
        GameObject model = Instantiate(LootManager.Instance.ItemList.pairs[lootIndex].model, transform);
        model.GetComponent<ColliderReference>().reference = gameObject;
        Model = model.GetComponent<HeldItemModel>();
    }

    public void OnAddedToInventory()
    {
        
    }

    public void OnRemovedFromInventory()
    {
        
    }

    public float GetWeight()
    {
        return LootManager.Instance.LootIndextoData(ItemIndex).weight;
    }

    public void UseItem(PlayerController player)
    {
        Model.UseItem(player);
    }
}
