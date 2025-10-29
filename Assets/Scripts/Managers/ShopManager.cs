using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ShopManager : NetworkBehaviour
{
    [SerializeField] private UI ui;
    
    public static ShopManager Instance;
    
    public ShopItems shopList;
    
    public List<int> boughtItems = new List<int>();

    public Dictionary<ShopItemType, string> ShopItemTypeLookup = new Dictionary<ShopItemType, string>();
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Instance == null)
        {
            Instance = this;
        }
        ui.PopulateShopContent();
        InitializeLookupDictionaries();
    }

    private void InitializeLookupDictionaries()
    {
        for (int i = 0; i < ShopManager.Instance.shopList.itemsTypesToNames.Length; i++)
        {
            ShopItemTypeToName pair = ShopManager.Instance.shopList.itemsTypesToNames[i];
            //Debug.Log(pair.name+ " " + pair.type);
            ShopItemTypeLookup.Add(pair.type, pair.name);
        }
    }

    public void ViewBoughtInventory()
    {
        ui.ClearSharedInventoryUI();
        ui.PopulateSharedInventoryUI(boughtItems);
        ui.ShowSharedInventoryPanel(true);
    }
    
    public void ViewShop()
    {
        ui.ShowShopPanel(true);
    }

    public void PurchaseItem(ShopItem shopItem, int index)
    {
        switch (shopItem.type)
        {
            case ShopItemType.PlayerPowerUp:
                ApplyPlayerPowerUp(shopItem);
                break;
            case ShopItemType.InventoryItem:
            case ShopItemType.BoatPart:
                AddToInventory(shopItem, index);
                break;
        }
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

    private void AddToInventory(ShopItem shopItem, int index)
    {
        AddToInventory_ServerRpc(index);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void AddToInventory_ServerRpc(int shopItemIndex)
    {
        boughtItems.Add(shopItemIndex);
        UpdateSharedInventory_ClientRpc(boughtItems.ToArray());
    }
    
    [ClientRpc(RequireOwnership = false)]
    private void UpdateSharedInventory_ClientRpc(int[] sharedInventory)
    {
        if (!IsServer)
        {
            boughtItems = new List<int>(sharedInventory);
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
