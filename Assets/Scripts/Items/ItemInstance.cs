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

    public Vector3 HoldAttachOffset()
    {
        if(Model.HoldAttachPoint == null) return Vector3.zero;
        return -Model.HoldAttachPoint.transform.localPosition;
    }
    
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
        Debug.Log(LootManager.Instance.ItemList.itemData.Length);
        Debug.Log(lootIndex);
        GameObject model = Instantiate(LootManager.Instance.ItemList.itemData[lootIndex].model, transform);
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

    public void OnEquip(InteractionController interactionController, PlayerState playerState)
    {
        Model.OnEquip(interactionController, playerState);
    }

    public void UseItem(InteractionController interactionController, PlayerState playerState)
    {
        Model.UseItem(interactionController, playerState);
    }
    
    public void OnUnequip(InteractionController interactionController, PlayerState playerState)
    {
        Model.OnUnequip(interactionController, playerState);
    }
}
