using System.Collections;
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
            StartCoroutine(AfterOnNetworkSpawn());
        }
        
        Cursor.lockState = CursorLockMode.Locked;
    }

    private IEnumerator AfterOnNetworkSpawn()
    {
        yield return null; // Wait for everything to initialize
        var connectedClientIds = NetworkManager.Singleton.ConnectedClientsIds;
        GameManager.Instance.SetPlayerPositions_ServerRpc(
            connectedClientIds.ToArray(), 
            playerStartLocation.position,
            playerStartLocation.rotation);
        GameManager.Instance.ChangeAllPlayerControlModes_ServerRpc(PlayerController.ControlModeEnum.FirstPerson);
        GameManager.Instance.RequestToNextGameState(); // Goes from none to between days
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
