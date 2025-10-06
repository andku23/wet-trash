using System;
using System.Collections.Generic;
using StarterAssets;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class LootController : NetworkBehaviour
{
    public static LootController Instance;
    
    [SerializeField] private GameObject _lootPrefab;
    [SerializeField] private GameObject _localLootPrefab;
    [SerializeField] private int _numLoot;
    [SerializeField] private LootLocalReferences lootLocalReferences;
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnLoot();
        }

        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void SpawnLoot()
    {
        for (int i = 0; i < _numLoot; i++)
        {
            GameObject go = Instantiate(_lootPrefab,
                new Vector3(Random.Range(-5f, 5f), 0.5f, Random.Range(-5f, 5f)),
                Quaternion.identity);
            NetworkObject networkObject = go.GetComponent<NetworkObject>();
            networkObject.Spawn();
        }
    }

    public void RequestPickup(NetworkLoot loot)
    {
        Pickup_ServerRpc(NetworkManager.Singleton.LocalClientId, loot.NetworkObjectId);
    }

    public void IDtoLocalPrefab(string id)
    {
        
    }
    
    public void LocalPrefabtoID(GameObject go)
    {
        
    }
    
    public void NetworkPrefabtoID(GameObject go)
    {
        
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void Pickup_ServerRpc(ulong targetPlayerNetworkObjectId, ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkLootObject))
        {

            uint prefabHash = networkLootObject.PrefabIdHash;
            networkLootObject.Despawn();
            Pickup_ClientRpc(targetPlayerNetworkObjectId, prefabHash);
        }
    }

    [ClientRpc(RequireOwnership = false)]
    public void Pickup_ClientRpc(ulong targetPlayerNetworkObjectId, uint prefabHash)
    {
        // Lookup who picked it up and get the client for it
        NetworkClient pickupPlayerClient = NetworkManager.Singleton.ConnectedClients[targetPlayerNetworkObjectId];
        LootCollector pickupPlayerCollector = pickupPlayerClient.PlayerObject.GetComponent<LootCollector>();
        pickupPlayerCollector.AttachToPoint(_localLootPrefab);

        for (int i = 0; i < lootLocalReferences.pairs.Length; i++)
        {
        }
    }

    public void RequestDrop(Vector3 position, GameObject loot)
    {
        Drop_ServerRpc(NetworkManager.Singleton.LocalClientId, position);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void Drop_ServerRpc(ulong targetPlayerNetworkObjectId, Vector3 position)
    {
       
        GameObject go = Instantiate(_lootPrefab, position, Quaternion.identity);
        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        networkObject.Spawn();
        
        Drop_ClientRpc(targetPlayerNetworkObjectId);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void Drop_ClientRpc(ulong targetPlayerNetworkObjectId)
    {
        NetworkClient pickupPlayerClient = NetworkManager.Singleton.ConnectedClients[targetPlayerNetworkObjectId];
        LootCollector pickupPlayerCollector = pickupPlayerClient.PlayerObject.GetComponent<LootCollector>();
        pickupPlayerCollector.DestroyHeldObject();
    }
    
}
