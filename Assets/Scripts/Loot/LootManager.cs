using System;
using System.Collections.Generic;
using StarterAssets;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class LootManager : NetworkBehaviour
{
    public static LootManager Instance;
    
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
        List<int> networkLootPrefabs = new List<int>();
        // Create spawn probability table
        for (int i = 0; i < lootLocalReferences.pairs.Length; i++)
        {
            for (int j = 0; j < lootLocalReferences.pairs[i].spawnRate; j++)
            {
                networkLootPrefabs.Add(i);
            }
        }
        
        for (int i = 0; i < _numLoot; i++)
        {
            GameObject go = Instantiate(lootLocalReferences.pairs[networkLootPrefabs[Random.Range(0, networkLootPrefabs.Count)]].network,
                new Vector3(Random.Range(-5f, 5f), 0.5f, Random.Range(-5f, 5f)) + transform.position,
                Quaternion.identity);
            NetworkObject networkObject = go.GetComponent<NetworkObject>();
            networkObject.Spawn();
        }
    }

    public LocalNetworkPrefabPair IDtoPrefabs(LootType id)
    {
        for (int i = 0; i < lootLocalReferences.pairs.Length; i++)
        {
            if (id == lootLocalReferences.pairs[i].id)
            {
                return lootLocalReferences.pairs[i];
            }
        }

        return null;
    }
    
    public LocalNetworkPrefabPair LocalPrefabtoID(GameObject prefabInstance)
    {
        LootBaseData lootData = prefabInstance.GetComponent<LootBaseData>();
        if (lootData != null)
        {
            for (int i = 0; i < lootLocalReferences.pairs.Length; i++)
            {
                if (lootData.lootType == lootLocalReferences.pairs[i].id)
                {
                    return lootLocalReferences.pairs[i];
                }
            }
        }

        return null;
    }
    
    public LocalNetworkPrefabPair NetworkPrefabtoID(GameObject prefabInstance)
    {
        LootBaseData lootData = prefabInstance.GetComponent<LootBaseData>();
        if (lootData != null)
        {
            for (int i = 0; i < lootLocalReferences.pairs.Length; i++)
            {
                if (lootData.lootType == lootLocalReferences.pairs[i].id)
                {
                    return lootLocalReferences.pairs[i];
                }
            }
        }
        

        return null;
    }
    
    public void RequestPickup(NetworkLoot loot)
    {
        Pickup_ServerRpc(NetworkManager.Singleton.LocalClientId, loot.NetworkObjectId, NetworkPrefabtoID(loot.gameObject).id);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void Pickup_ServerRpc(ulong targetPlayerNetworkObjectId, ulong networkObjectId, LootType lootID)
    {
        Debug.Log(lootID);
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkLootObject))
        {
            networkLootObject.Despawn();
            Pickup_ClientRpc(targetPlayerNetworkObjectId, lootID);
        }
    }

    [ClientRpc(RequireOwnership = false)]
    public void Pickup_ClientRpc(ulong targetPlayerNetworkObjectId, LootType lootID)
    {
        // Lookup who picked it up and get the client for it
        GameObject localLootPrefab = IDtoPrefabs(lootID).local;
        NetworkClient pickupPlayerClient = NetworkManager.Singleton.ConnectedClients[targetPlayerNetworkObjectId];
        LootCollector pickupPlayerCollector = pickupPlayerClient.PlayerObject.GetComponent<LootCollector>();
        pickupPlayerCollector.AttachToPoint(localLootPrefab);
    }

    public void RequestDrop(Vector3 position, GameObject loot)
    {
        Drop_ServerRpc(NetworkManager.Singleton.LocalClientId, position, loot.GetComponent<LootBaseData>().lootType);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void Drop_ServerRpc(ulong targetPlayerNetworkObjectId, Vector3 position, LootType lootID)
    {
       
        GameObject go = Instantiate(IDtoPrefabs(lootID).network, position, Quaternion.identity);
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
