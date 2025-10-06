using System;
using System.Collections.Generic;
using StarterAssets;
using Unity.Netcode;
using UnityEditor;
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
            NetworkLoot loot = go.GetComponent<NetworkLoot>();
            //loot.lootID.Value = lootLocalReferences.pairs[0].id;
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
        //GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(prefabInstance);
        //for (int i = 0; i < lootLocalReferences.pairs.Length; i++)
        //{
        //    if (prefabAsset == lootLocalReferences.pairs[i].local)
        //    {
        //        return lootLocalReferences.pairs[i];
        //    }
        //}

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
