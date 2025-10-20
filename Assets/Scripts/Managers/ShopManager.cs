using Unity.Netcode;
using UnityEngine;

public class ShopManager : NetworkBehaviour
{
    [SerializeField] private UI ui;
    
    public static ShopManager Instance;
    
    public ShopItems shopList;
    
    
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

    public void PurchaseItem(ShopItem shopItem)
    {
        switch (shopItem.type)
        {
            case ShopItemType.PlayerPowerUp:
                ApplyPlayerPowerUp(shopItem);
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
}
