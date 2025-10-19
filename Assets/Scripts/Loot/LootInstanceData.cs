using Unity.Netcode;
using UnityEngine;

public class LootInstanceData : MonoBehaviour
{
    public int LootIndex;
    
    public void LoadLootLocal(LootData lootData, int lootIndex)
    {
        LootIndex = lootIndex;
        GameObject model = Instantiate(lootData.model, transform);
        model.GetComponent<ColliderReference>().reference = gameObject;
    }
    
    public void LoadLootNetwork(int lootIndex)
    {
        LootIndex = lootIndex;
        GameObject model = Instantiate(LootManager.Instance.LootList.pairs[lootIndex].model, transform);
        model.GetComponent<ColliderReference>().reference = gameObject;
    }
}
