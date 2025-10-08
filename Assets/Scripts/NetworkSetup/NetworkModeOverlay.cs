using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class NetworkModeOverlay : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private TextMeshProUGUI textMesh;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (NetworkManager.Singleton.IsServer)
        {
            textMesh.text = "Server";
        }
        NetworkManager.Singleton.OnConnectionEvent += OnConnected;

    }
    
    private void OnConnected(NetworkManager manager, ConnectionEventData eventData)
    {
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("Host");
        }
        
        if (NetworkManager.Singleton.IsClient)
        {
            Debug.Log("Client");
        }
        
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Server");
        }

        if (textMesh.text == "Server")
        {
            textMesh.text = "Host";
        }
        else
        {
            textMesh.text = "Client";
        }
        
    }
  
}
