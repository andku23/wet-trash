using System;
using System.Collections.Generic;
using StarterAssets;
using TMPro;
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
    [SerializeField] private TextMeshProUGUI moneyText;

    private List<NetworkObject> _loots = new List<NetworkObject>();
    private Dictionary<int, List<NetworkObject>> _deposits = new Dictionary<int, List<NetworkObject>>();
    private NetworkVariable<int> award = new NetworkVariable<int>(0);
    
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Instance == null)
        {
            Instance = this;
        }

        award.OnValueChanged += UpdateScore;
    }

    private void UpdateScore(int prev, int next)
    {
        moneyText.text = $"${next}";
    }

    public void SpawnLoot()
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
            _loots.Add(networkObject);
        }
    }

    public void DeleteAllLoot()
    {
        for (int i = 0; i < _loots.Count; i++)
        {
            _loots[i].Despawn();
        }
        _loots.Clear();
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
        int depositNum = -1;
        List<NetworkObject> list = null;
        
        //Check if its in one of the deposits so we can subtract cost
        foreach (KeyValuePair<int, List<NetworkObject>> entry in _deposits)
        {
            for (int i = 0; i < entry.Value.Count; i++)
            {
                if (networkObjectId == entry.Value[i].NetworkObjectId)
                {
                    depositNum = i;
                    list = entry.Value;
                }
            }
        }

        if (list != null)
        {
            list.RemoveAt(depositNum);
            award.Value -= 200;
        }
        
        
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

    public void RequestDeposit(LootDeposit deposit, GameObject loot)
    {
        Deposit_ServerRpc(NetworkManager.Singleton.LocalClientId, deposit.ID, loot.GetComponent<LootBaseData>().lootType, deposit.GetRandomPosition());
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void Deposit_ServerRpc(ulong targetPlayerNetworkObjectId, int depositID, LootType lootID, Vector3 position)
    {
        if (!_deposits.ContainsKey(depositID))
        {
            _deposits[depositID] = new List<NetworkObject>();
        }
        
        GameObject go = Instantiate(IDtoPrefabs(lootID).network, position, Quaternion.identity);
        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        networkObject.Spawn();
        award.Value += 200;
        
        _deposits[depositID].Add(networkObject);
        
        Drop_ClientRpc(targetPlayerNetworkObjectId);
    }
    
}
