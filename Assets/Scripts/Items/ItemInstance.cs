using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class ItemInstance : MonoBehaviour, IHoldable, IInventorable
{
    //public int ItemIndex;
    //public ulong SpawnedPlayerID; // Currently only used to handle dead players as loot

    public ItemInstanceData ItemInstanceData;
    
    public HeldItemModel Model;
    
    [SerializeField] private HeldObjectType _heldObjectType;
    [SerializeField] private GameObject _highlight;
    
    public HeldObjectType HeldObjectType { get => _heldObjectType; }
    public GameObject ConnectedParent { get; set; }


    public Vector3 HoldAttachOffset()
    {
        if(Model.HoldAttachPoint == null) return Vector3.zero;
        return -Model.HoldAttachPoint.transform.localPosition;
    }
    
    public ulong HeldPlayerID { get; set; }
    
    public void LoadLocal(ItemData itemData, int itemIndex)
    {
        ItemInstanceData.ItemIndex = itemIndex;
        GameObject model = Instantiate(itemData.model, transform);
        model.GetComponent<ColliderReference>().enabled = false;
        model.GetComponent<Collider>().enabled = false;
        Model = model.GetComponent<HeldItemModel>();
    }
    
    public void LoadNetwork(int lootIndex, ulong spawnedPlayerID)
    {
        ItemInstanceData.ItemIndex = lootIndex;
        ItemInstanceData.SpawnedPlayerID = spawnedPlayerID;
        GameObject model = Instantiate(LootManager.Instance.ItemList.itemDatas[lootIndex].model, transform);
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
        return LootManager.Instance.LootIndextoData(ItemInstanceData.ItemIndex).weight;
    }

    public void OnEquip(InteractionController interactionController, PlayerStateData playerState)
    {
        Model.OnEquip(interactionController, playerState);
    }

    public void UseItem(InteractionController interactionController, PlayerStateData playerState)
    {
        Model.UseItem(interactionController, playerState);
    }
    
    public void OnUnequip(InteractionController interactionController, PlayerStateData playerState)
    {
        Model.OnUnequip(interactionController, playerState);
    }

    public void SetHighlight(bool isHighlighted)
    {
        if(_highlight != null)
            _highlight.SetActive(isHighlighted);
        
        Model.SetHighlight(isHighlighted);
    }
}

// All the data needed to recreate and identical item instance
public struct ItemInstanceData: INetworkSerializable
{
    public int ItemIndex;
    public ulong SpawnedPlayerID;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ItemIndex);
        serializer.SerializeValue(ref SpawnedPlayerID);
    }
}
