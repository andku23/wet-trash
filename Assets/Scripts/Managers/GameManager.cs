using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private LootManager _lootManager;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    public void SpawnLoot()
    {
        SpawnLoot_ServerRPC();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnLoot_ServerRPC()
    {
        _lootManager.DeleteAllLoot();
        _lootManager.SpawnLoot();
    }
}
