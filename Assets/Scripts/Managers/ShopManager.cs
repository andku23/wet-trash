using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ShopManager : NetworkBehaviour
{
    [SerializeField] private UI ui;
    
    public static ShopManager Instance;
    
    public ShopItems shopList;
    
    private List<int> boughtItems = new List<int>();
    
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Instance == null)
        {
            Instance = this;
        }
        
        ui.SpawnShopContent();
    }
    
    public void ViewInventory()
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
        Debug.Log(s);
    }
}
