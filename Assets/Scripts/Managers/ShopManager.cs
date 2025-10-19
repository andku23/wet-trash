using Unity.Netcode;
using UnityEngine;

public class ShopManager : NetworkBehaviour
{
    [SerializeField] private UI ui;
    
    public static ShopManager Instance;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (Instance == null)
        {
            Instance = this;
        }
    }
    
    public void ViewInventory()
    {
        ui.ShowShopPanel(true);
    }

    public void PurchaseItem(int index)
    {
       // ShopItem item = 
    }
}
