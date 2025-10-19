using System;
using System.Collections.Generic;
using StarterAssets;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class LootManager : NetworkBehaviour
{
    public static LootManager Instance;
    
    [SerializeField] private int _numLoot;
    [SerializeField] private LootList lootList;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] LootDeposit[] _lootDeposits;
    

    private List<NetworkObject> _loots = new List<NetworkObject>();
    
    public LootList LootList
    {
        get { return lootList; }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void UpdateScore(int prev, int next)
    {
        moneyText.text = $"${next}";
    }

    public void SpawnLoot()
    {
        List<int> networkLootPrefabs = new List<int>();
        // Create spawn probability table
        for (int i = 0; i < lootList.pairs.Length; i++)
        {
            for (int j = 0; j < lootList.pairs[i].spawnRate; j++)
            {
                networkLootPrefabs.Add(i);
            }
        }
        
        for (int i = 0; i < _numLoot; i++)
        {
            GameObject go = Instantiate(lootList.networkLootPrefab,
                new Vector3(Random.Range(-15f, 15f), 0.5f, Random.Range(-15f, 15f)) + transform.position,
                Quaternion.identity);
            LootBaseData lootBaseData = go.GetComponent<LootBaseData>();
            LootData randomlySelectedData = lootList.pairs[networkLootPrefabs[Random.Range(0, networkLootPrefabs.Count)]];
            lootBaseData.LoadLoot(randomlySelectedData);
            
            NetworkObject networkObject = go.GetComponent<NetworkObject>();
            networkObject.Spawn();
            _loots.Add(networkObject);
        }
    }

    private void SpawnAndLoadLoot()
    {
        
    }

    public void DeleteAllLoot()
    {
        for (int i = 0; i < _loots.Count; i++)
        {
            _loots[i].Despawn();
        }
        _loots.Clear();
    }

    public LootData IDtoPrefabs(LootType id)
    {
        for (int i = 0; i < lootList.pairs.Length; i++)
        {
            if (id == lootList.pairs[i].id)
            {
                return lootList.pairs[i];
            }
        }

        return null;
    }
    
    public LootData LocalPrefabtoID(GameObject prefabInstance)
    {
        LootBaseData lootData = prefabInstance.GetComponent<LootBaseData>();
        if (lootData != null)
        {
            for (int i = 0; i < lootList.pairs.Length; i++)
            {
                if (lootData.lootType == lootList.pairs[i].id)
                {
                    return lootList.pairs[i];
                }
            }
        }

        return null;
    }
    
    public LootData NetworkPrefabtoID(GameObject prefabInstance)
    {
        LootBaseData lootData = prefabInstance.GetComponent<LootBaseData>();
        if (lootData != null)
        {
            for (int i = 0; i < lootList.pairs.Length; i++)
            {
                if (lootData.lootType == lootList.pairs[i].id)
                {
                    return lootList.pairs[i];
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
        
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkLootObject))
        {
            _loots.Remove(networkLootObject);
            networkLootObject.Despawn();
            Pickup_ClientRpc(targetPlayerNetworkObjectId, lootID);
        }
    }

    private void DestroyLootInHand(ulong targetPlayerNetworkObjectId)
    {
        NetworkClient pickupPlayerClient = NetworkManager.Singleton.ConnectedClients[targetPlayerNetworkObjectId];
        InteractionController pickupPlayerCollector = pickupPlayerClient.PlayerObject.GetComponent<InteractionController>();
        pickupPlayerCollector.DestroyHeldObject();
    }

    [ClientRpc(RequireOwnership = false)]
    public void Pickup_ClientRpc(ulong targetPlayerNetworkObjectId, LootType lootID)
    {
        // Lookup who picked it up and get the client for it
        GameObject localLootPrefab = lootList.localLootPrefab;
        
            
        NetworkClient pickupPlayerClient = NetworkManager.Singleton.ConnectedClients[targetPlayerNetworkObjectId];
        InteractionController pickupPlayerCollector = pickupPlayerClient.PlayerObject.GetComponent<InteractionController>();
        GameObject attachedLoot = pickupPlayerCollector.AttachToPoint(localLootPrefab);
        
        LootBaseData lootBaseData = attachedLoot.GetComponent<LootBaseData>();
        lootBaseData.LoadLoot(IDtoPrefabs(lootID));
    }

    public void RequestDrop(Vector3 position, GameObject loot)
    {
        Drop_ServerRpc(NetworkManager.Singleton.LocalClientId, position, loot.GetComponent<LootBaseData>().lootType);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void Drop_ServerRpc(ulong targetPlayerNetworkObjectId, Vector3 position, LootType lootID)
    {
        // GameObject go = Instantiate(IDtoPrefabs(lootID).network, position, Quaternion.identity);
        GameObject go = Instantiate(lootList.networkLootPrefab, position, Quaternion.identity);
        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        LootBaseData lootBaseData = go.GetComponent<LootBaseData>();
        lootBaseData.LoadLoot(IDtoPrefabs(lootID));
        networkObject.Spawn();
        _loots.Add(networkObject);     
        Drop_ClientRpc(targetPlayerNetworkObjectId);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void Drop_ClientRpc(ulong targetPlayerNetworkObjectId)
    {
        DestroyLootInHand(targetPlayerNetworkObjectId);
    }

    public void RequestDeposit(LootDeposit deposit, GameObject loot)
    {
        int id = -1;
        for (int i = 0; i < _lootDeposits.Length; i++)
        {
            if (_lootDeposits[i] == deposit)
            {
                id = i;
            }
        }

        if (id >= 0)
        {
            Deposit_ServerRpc(NetworkManager.Singleton.LocalClientId, id, loot.GetComponent<LootBaseData>().lootType);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void Deposit_ServerRpc(ulong targetPlayerNetworkObjectId, int depositID, LootType lootID)
    {
        MoneyManager.Instance.AddCash(200);
        Deposit_ClientRpc(targetPlayerNetworkObjectId, depositID);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void Deposit_ClientRpc(ulong targetPlayerNetworkObjectId, int depositID)
    {
        DestroyLootInHand(targetPlayerNetworkObjectId);
        _lootDeposits[depositID].PlaceLootAtNextPosition();
    }
    
}
