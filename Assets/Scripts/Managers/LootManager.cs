using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class LootManager : NetworkBehaviour
{
    public static LootManager Instance;
    
    [SerializeField] private ItemList itemList;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private List<LootDeposit> _lootDeposits;
    [SerializeField] private List<LootGroup> _lootGroups;
    [SerializeField] private GameObject spawnCutoff;

    public Action<int> OnLootDeposited;

    private List<NetworkObject> _loots_S;
    
    public ItemList ItemList
    {
        get { return itemList; }
    }

    public Dictionary<WorldManager.BiomeType, BiomeLootSpawnProbability> BiomeLootSpawnTable = new Dictionary<WorldManager.BiomeType, BiomeLootSpawnProbability>();

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        for (int i = 0; i < itemList.biomeLootSpawnProbability.Length; i++)
        {
            var el = itemList.biomeLootSpawnProbability[i];
            BiomeLootSpawnTable.Add(el.biomeType, el);
        }
    }
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            _loots_S = new List<NetworkObject>();
        }
    }

    private void UpdateScore(int prev, int next)
    {
        moneyText.text = $"${next}";
    }

    // Returns dictionary of total loot costs of each currency time
    public Dictionary<CurrencyType, int> SpawnLoot_S()
    {
        //int totalCost = 0;
        var totalCosts = VarietyUtilities.GetInitializedCurrencyDictionary();
        
        // Fill probability arrays with n * probability of each item
        // So that we can randomly sample from these later
        List<int> spawnProbabilityCommon = new List<int>();
        List<int> spawnProbabilityRare = new List<int>();
        List<int> spawnProbabilitySuperRare = new List<int>();
        
        for (int i = 0; i < itemList.itemData.Length; i++)
        {
            for (int j = 0; j < itemList.itemData[i].spawnRateCommon; j++)
            {
                spawnProbabilityCommon.Add(i);
            }
            for (int j = 0; j < itemList.itemData[i].spawnRateRare; j++)
            {
                spawnProbabilityRare.Add(i);
            }
            for (int j = 0; j < itemList.itemData[i].spawnRateVeryRare; j++)
            {
                spawnProbabilitySuperRare.Add(i);
            }
        }
        
        // Spawn loot based on created loot tables
        Vector3 spawnPosition = Vector3.zero;
        List<Vector2> lootTerrainPosition = WorldManager.Instance.GetLootSpawnPositions();
        foreach (var terrainPosition in lootTerrainPosition)
        {
            WorldManager.BiomeType biomeType = WorldManager.Instance.GetBiomeType((int)terrainPosition.x, (int)terrainPosition.y);
            List<int> currentSpawnTable = null;
            switch (biomeType)
            {
                case WorldManager.BiomeType.NearShore:
                case WorldManager.BiomeType.Biome1:
                    currentSpawnTable = spawnProbabilityCommon;
                    break;
                case WorldManager.BiomeType.Biome2:
                case WorldManager.BiomeType.Biome3:
                case WorldManager.BiomeType.Biome4:
                    currentSpawnTable = spawnProbabilityRare;
                    break;
                case WorldManager.BiomeType.Depths:
                    currentSpawnTable = spawnProbabilitySuperRare;
                    break;
            }
            spawnPosition = WorldManager.Instance.GetPointOnTerrainFromResolution((int)terrainPosition.y, (int)terrainPosition.x);
            var itemData = SpawnLootFromTable(spawnPosition, currentSpawnTable);
            foreach (var valueType in itemData.valueRange)
            {
                totalCosts[valueType.CurrencyType] += valueType.MaxValue;
            }
        }
        
        //One on the surface just to debug
        var testItemData = SpawnLootFromTable(new Vector3(0,0,0), spawnProbabilityCommon);
        foreach (var valueType in testItemData.valueRange)
        {
            totalCosts[valueType.CurrencyType] += valueType.MaxValue;
        }
        
        for (int i = 0; i < _lootGroups.Count; i++)
        {
            for (int j = 0; j < _lootGroups[i].lootSpawnLocations.Length; j++)
            {
                var groupItemData =
                    SpawnLootFromTable(_lootGroups[i].lootSpawnLocations[j].position, spawnProbabilityRare);
                foreach (var valueType in groupItemData.valueRange)
                {
                    totalCosts[valueType.CurrencyType] += valueType.MaxValue;
                }
            }
        }
        
        return totalCosts;
    }
    
    private ItemData SpawnLootFromTable(Vector3 spawnPosition, List<int> spawnProbabilityTable)
    {
        int selectedLootIndex = spawnProbabilityTable[Random.Range(0, spawnProbabilityTable.Count)];
        return SpawnAndLoadLoot_S(spawnPosition, selectedLootIndex);
    }

    public ItemData SpawnAndLoadLoot_S(Vector3 spawnPosition, int lootIndex)
    {
        GameObject go = Instantiate(itemList.networkLootPrefab,
            spawnPosition,
            Quaternion.identity);
        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        NetworkLoot networkLoot = go.GetComponent<NetworkLoot>();
            
        networkLoot.lootIndex.Value = lootIndex;
        networkObject.Spawn();
        _loots_S.Add(networkObject);
        return itemList.itemData[lootIndex];
    }


    private void DestroyLootInHand(ulong targetPlayerNetworkObjectId)
    {
        NetworkClient pickupPlayerClient = NetworkManager.Singleton.ConnectedClients[targetPlayerNetworkObjectId];
        InteractionController pickupPlayerCollector = pickupPlayerClient.PlayerObject.GetComponent<InteractionController>();
        pickupPlayerCollector.DropItemNetwork(targetPlayerNetworkObjectId);
    }

    private NetworkObject SpawnItem_S(Vector3 position, int itemIndex)
    {
        GameObject go = Instantiate(itemList.networkLootPrefab, position, Quaternion.identity);
        NetworkObject networkObject = go.GetComponent<NetworkObject>();
        NetworkLoot networkLoot = go.GetComponent<NetworkLoot>();

        if (networkLoot != null) networkLoot.lootIndex.Value = itemIndex;
        networkObject.Spawn();
        
        NetworkFall networkFall = networkObject.GetComponent<NetworkFall>();
        if (networkFall != null) networkFall.enabled = true;
        
        //_loots.Add(networkObject);
        return networkObject;
    }

    public void DeleteAllLoot_S()
    {
        for (int i = 0; i < _loots_S.Count; i++)
        {
            _loots_S[i].Despawn();
        }
        _loots_S.Clear();
    }

    public ItemData LootIndextoData(int lootIndex)
    {
        if (lootIndex < itemList.itemData.Length)
        {
            return itemList.itemData[lootIndex];
        }

        return null;
    }
    
    public ItemData LootPrefabtoData(GameObject prefabInstance)
    {
        ItemInstance item = prefabInstance.GetComponent<ItemInstance>();
        if (item != null)
        {
            if (item.ItemIndex < itemList.itemData.Length)
            {
                return itemList.itemData[item.ItemIndex];
            }
        }
        return null;
    }
    
    public int LootPrefabtoIndex(GameObject prefabInstance)
    {
        ItemInstance item = prefabInstance.GetComponent<ItemInstance>();
        if (item != null)
        {
            return item.ItemIndex;
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
            DespawnLoot_S(networkLootObject);
            Pickup_ClientRpc(targetPlayerNetworkObjectId, lootIndex);
        }
    }

    public void DespawnLoot_S(NetworkObject networkObject)
    {
        _loots_S.Remove(networkObject);
        networkObject.Despawn();
    }

    [ClientRpc(RequireOwnership = false)]
    public void Pickup_ClientRpc(ulong targetPlayerNetworkObjectId, int lootIndex)
    {
        // Lookup who picked it up and get the client for it
        NetworkClient pickupPlayerClient = NetworkManager.Singleton.ConnectedClients[targetPlayerNetworkObjectId];
        InteractionController pickupPlayerCollector = pickupPlayerClient.PlayerObject.GetComponent<InteractionController>();
        //GameObject attachedLoot = pickupPlayerCollector.AttachToPoint(localLootPrefab, targetPlayerNetworkObjectId);
        pickupPlayerCollector.PickupItemNetwork(lootIndex, targetPlayerNetworkObjectId);
    }

    public void RequestDrop(Vector3 position, GameObject loot)
    {
        Drop_ServerRpc(NetworkManager.Singleton.LocalClientId, position, LootPrefabtoIndex(loot));
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void Drop_ServerRpc(ulong targetPlayerNetworkObjectId, Vector3 position, int lootIndex)
    {
        NetworkObject networkObject = SpawnItem_S(position, lootIndex);
        _loots_S.Add(networkObject);
        Drop_ClientRpc(targetPlayerNetworkObjectId);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void Drop_ClientRpc(ulong targetPlayerNetworkObjectId)
    {
        DestroyLootInHand(targetPlayerNetworkObjectId);
    }

    public void RequestSpawnItem(Vector3 position, int itemIndex)
    {
        RequestSpawnItem_ServerRpc(position, itemIndex, NetworkManager.Singleton.LocalClientId);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnItem_ServerRpc(Vector3 position, int itemIndex, ulong targetPlayerNetworkObjectId)
    {
        SpawnItem_S(position, itemIndex);
    }
    
    public void ResetLootHoles_S()
    {
        _lootGroups.Clear();
    }
    
    public void RegisterLootGroupServer(LootGroup hole)
    {
        _lootGroups.Add(hole);
        int id = _lootGroups.Count - 1;
        hole.id = id;
    }
    
    public void RegisterDepositServer(LootDeposit deposit)
    {
        _lootDeposits.Add(deposit);
        int id = _lootDeposits.Count - 1;
        deposit.id.Value = id;
    }

    public void RequestDeposit(LootDeposit deposit, GameObject loot)
    {
        Deposit_ServerRpc(NetworkManager.Singleton.LocalClientId, deposit.id.Value, LootPrefabtoIndex(loot));
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void Deposit_ServerRpc(ulong targetPlayerNetworkObjectId, int lootDepositIndex, int lootIndex)
    {
        MoneyManager.Instance.CashInLoot(lootIndex);
        Deposit_ClientRpc(targetPlayerNetworkObjectId, lootDepositIndex);
    }
    
    [ClientRpc(RequireOwnership = false)]
    public void Deposit_ClientRpc(ulong targetPlayerNetworkObjectId, int lootDepositIndex)
    {
        DestroyLootInHand(targetPlayerNetworkObjectId);
        OnLootDeposited.Invoke(lootDepositIndex);
        
        //LootDeposit lootDeposit = NetworkManager.Singleton.SpawnManager.SpawnedObjects[depositNetworkID].GetComponent<LootDeposit>();
        //lootDeposit.PlaceLootAtNextPosition();
    }
    
}
