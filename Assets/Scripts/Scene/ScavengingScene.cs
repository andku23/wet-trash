using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ScavengingScene : NetworkBehaviour
{
    [SerializeField] private Transform playerStartLocation;
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            var connectedClientIds = NetworkManager.Singleton.ConnectedClientsIds;
            GameManager.Instance.SetPlayerPositions_ServerRpc(connectedClientIds.ToArray(), playerStartLocation.position);
            GameManager.Instance.ChangeAllPlayerControlModes_ServerRpc(PlayerController.ControlModeEnum.FirstPerson);
        }
        
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    public void ToNextGameState()
    {
        GameManager.Instance.RequestToNextGameState();
    }

    public void OpenShop()
    {
        ShopManager.Instance.ViewShop();
    }
}
