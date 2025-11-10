using Unity.Netcode;
using UnityEngine;

public class LootInstanceData : MonoBehaviour, IHoldable
{
    public int LootIndex;
    public LootModel LootModel;
    
    [SerializeField] private HeldObjectType _heldObjectType;
    
    public HeldObjectType HeldObjectType { get => _heldObjectType; }
    public GameObject ConnectedParent { get; set; }
    
    public ulong HeldPlayerID { get; set; }
    
    public void LoadLootLocal(LootData lootData, int lootIndex)
    {
        LootIndex = lootIndex;
        
        GameObject model = Instantiate(lootData.model, transform);
        model.GetComponent<ColliderReference>().enabled = false;
        model.GetComponent<Collider>().enabled = false;
        LootModel = model.GetComponent<LootModel>();
    }
    
    public void LoadLootNetwork(int lootIndex)
    {
        LootIndex = lootIndex;
        GameObject model = Instantiate(LootManager.Instance.LootList.pairs[lootIndex].model, transform);
        model.GetComponent<ColliderReference>().reference = gameObject;
        LootModel = model.GetComponent<LootModel>();
    }

    public float GetWeightMultiplier()
    {
        return LootManager.Instance.LootIndextoData(LootIndex).weightSpeedMultiplier;
    }
}
