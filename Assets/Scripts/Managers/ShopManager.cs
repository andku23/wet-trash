using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class ShopManager : NetworkBehaviour
{
    [FormerlySerializedAs("ui")] [SerializeField] private GameUI gameUI;
    
    public static ShopManager Instance;
    
    public ShopItems shopList;
    
    public List<int> boughtItems = new List<int>();

    public Dictionary<ShopItemType, ShopItemTypeData> ShopItemTypeLookup = new Dictionary<ShopItemType, ShopItemTypeData>();
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Instance == null)
        {
            Instance = this;
        }
        InitializeLookupDictionaries();
    }

    private void InitializeLookupDictionaries()
    {
        for (int i = 0; i < shopList.itemsTypesToNames.Length; i++)
        {
            ShopItemTypeData pair = shopList.itemsTypesToNames[i];
            //Debug.Log(pair.name+ " " + pair.type);
            ShopItemTypeLookup.Add(pair.type, pair);
        }
    }

    public void ViewBoughtInventory()
    {
        GameUI.Instance.ClearSharedInventoryUI();
        GameUI.Instance.PopulateSharedInventoryUI(boughtItems);
        GameUI.Instance.ShowSharedInventoryPanel(true);
    }
    
    public void ViewShop()
    {
        GameUI.Instance.ShowShopPanel(true);
    }

    public void PurchaseItem(ShopItem shopItem, int index)
    {
        switch (shopItem.type)
        {
            case ShopItemType.PlayerPowerUp:
                ApplyPlayerPowerUp(shopItem);
                break;
            case ShopItemType.InventoryItem:
                SpawnInventoryItem(shopItem);
                break;
            case ShopItemType.BoatPart:
                AddToStorage(shopItem, index);
                break;
            case ShopItemType.BoatAttachment:
                AddToStorage(shopItem, index);
                break;
        }
    }

    private void SpawnInventoryItem(ShopItem shopItem)
    {
        GameObject playerObject = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
        Vector3 spawnPosition = playerObject.transform.position;
        spawnPosition += new Vector3(playerObject.transform.forward.x, 0, playerObject.transform.forward.z).normalized;
        spawnPosition.y += 1.5f;
        LootManager.Instance.RequestSpawnItem(spawnPosition, shopItem.itemListIndex);
    }

    private void ApplyPlayerPowerUp(ShopItem shopItem)
    {
        PlayerState playerState = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerState>();
        
        if (shopItem.id == "run-speed-up")
        {
            playerState.SprintSpeed += playerState.SprintSpeed_UpgradeIncrement;
        } else if (shopItem.id == "swim-speed-up")
        {
            playerState.SprintSwimSpeed += playerState.SprintSwimSpeed_UpgradeIncrement;
        } else if (shopItem.id == "breath-up")
        {
            playerState.BreathFullAmount += playerState.BreathFullAmount_UpgradeIncrement;
        }
    }

    private void AddToStorage(ShopItem shopItem, int index)
    {
        AddToStorage_ServerRpc(index);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void AddToStorage_ServerRpc(int shopItemIndex)
    {
        boughtItems.Add(shopItemIndex);
        UpdateSharedStorage_ClientRpc(boughtItems.ToArray());
    }
    
    [ClientRpc(RequireOwnership = false)]
    private void UpdateSharedStorage_ClientRpc(int[] sharedStorage)
    {
        if (!IsServer)
        {
            boughtItems = new List<int>(sharedStorage);
        }
        //LogBoughtItems();
        
    }

    private void LogBoughtItems()
    {
        string s = "[";
        for (int i = 0; i < boughtItems.Count; i++)
        {
            s += boughtItems[i] + ", ";
        }
        //Debug.Log(s);
    }
}
