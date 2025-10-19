using UnityEngine;

public class LootBaseData : MonoBehaviour
{
    public LootType lootType;

    public void LoadLoot(LootData lootData)
    {
        //lootType = lootType;
        GameObject model = Instantiate(lootData.model, transform);
        model.GetComponent<ColliderReference>().reference = gameObject;
    }
}
