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
            //LootBaseData lootBaseData = go.GetComponent<LootBaseData>();
            int selectedPrefabIndex = networkLootPrefabs[Random.Range(0, networkLootPrefabs.Count)];
            //LootData randomlySelectedData = lootList.pairs[selectedPrefabIndex];
            
            NetworkObject networkObject = go.GetComponent<NetworkObject>();
            NetworkLoot networkLoot = go.GetComponent<NetworkLoot>();
            
            networkLoot.lootIndex.Value = selectedPrefabIndex;
            networkObject.Spawn();
            _loots.Add(networkObject);
        }
    }

    private void SpawnAndLoadLoot()
    {
        
    }
    
    private void DestroyLootInHand(ulong targetPlayerNetworkObjectId)
    {
        NetworkClient pickupPlayerClient = NetworkManager.Singleton.ConnectedClients[targetPlayerNetworkObjectId];
        InteractionController pickupPlayerCollector = pickupPlayerClient.PlayerObject.GetComponent<InteractionController>();
        pickupPlayerCollector.DestroyHeldObject();
    }

    public void DeleteAllLoot()
    {
        for (int i = 0; i < _loots.Count; i++)
        {
            _loots[i].Despawn();
        }
        _loots.Clear();
    }

    public LootData LootIndextoData(int lootIndex)
    {
        if (lootIndex < lootList.pairs.Length)
        {
            return lootList.pairs[lootIndex];
        }

        return null;
    }
    
    public LootData LootPrefabtoData(GameObject prefabInstance)
    {
        LootInstanceData lootData = prefabInstance.GetComponent<LootInstanceData>();
        if (lootData != null)
        {
            if (lootData.LootIndex < lootList.pairs.Length)
            {
                return lootList.pairs[lootData.LootIndex];
            }
        }
        return null;
    }
    
    public int LootPrefabtoIndex(GameObject prefabInstance)
    {
        LootInstanceData lootData = prefabInstance.GetComponent<LootInstanceData>();
        if (lootData != null)
        {
            return lootData.LootIndex;
        }
        return -1;
    }
    
    public void RequestPickup(NetworkLoot loot)
    {
        Pickup_ServerRpc(NetworkManager.Singleton.LocalClientId, loot.NetworkObjectId, LootPrefabtoIndex(loot.gameObject));
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void Pickup_ServerRpc(ulong targetPlayerNetworkObjectId, ulong networkObjectId, int lootIndex)
    {
        int depositNum = -1;
        List<NetworkObject> list = null;
        
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkLootObject))
        {
            _loots.Remove(networkLootObject);
            networkLootObject.Despawn();
            Pickup_ClientRpc(targetPlayerNetworkObjectId, lootIndex);
        }
    }

    [ClientRpc(RequireOwnership = false)]
    public void Pickup_ClientRpc(ulong targetPlayerNetworkObjectId, int lootIndex)
    {
        // Lookup who picked it up and get the client for it
        GameObject localLootPrefab = lootList.localLootPrefab;
            
        NetworkClient pickupPlayerClient = NetworkManager.Singleton.ConnectedClients[targetPlayerNetworkObjectId];
        InteractionController pickupPlayerCollector = pickupPlayerClient.PlayerObject.GetComponent<InteractionController>();
        GameObject attachedLoot = pickupPlayerCollector.AttachToPoint(localLootPrefab);
        
        LootInstanceData lootInstanceData = attachedLoot.GetComponent<LootInstanceData>();
        lootInstanceData.LoadLootLocal(LootIndextoData(lootIndex), lootIndex);
    }

    public void RequestDrop(Vector3 position, GameObject loot)
    {
        Drop_ServerRpc(NetworkManager.Singleton.LocalClientId, position, LootPrefabtoIndex(loot));
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void Drop_ServerRpc(ulong targetPlayerNetworkObjectId, Vector3 position, int lootIndex)
    {
        // GameObject go = Instantiate(IDtoPrefabs(lootID).network, position, Quaternion.identity);
        GameObject go = Instantiate(lootList.networkLootPrefab, position, Quaternion.identity);
        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        LootInstanceData lootInstanceData = go.GetComponent<LootInstanceData>();
        NetworkLoot networkLoot = go.GetComponent<NetworkLoot>();
        
        networkLoot.lootIndex.Value = lootIndex;
        //lootBaseData.LoadLootNetwork();
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
            Deposit_ServerRpc(NetworkManager.Singleton.LocalClientId, id, LootPrefabtoIndex(loot));
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void Deposit_ServerRpc(ulong targetPlayerNetworkObjectId, int depositID, int lootIndex)
    {
        MoneyManager.Instance.AddCash(lootList.pairs[lootIndex].price);
        Deposit_ClientRpc(targetPlayerNetworkObjectId, depositID);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void Deposit_ClientRpc(ulong targetPlayerNetworkObjectId, int depositID)
    {
        DestroyLootInHand(targetPlayerNetworkObjectId);
        _lootDeposits[depositID].PlaceLootAtNextPosition();
    }
    
}
