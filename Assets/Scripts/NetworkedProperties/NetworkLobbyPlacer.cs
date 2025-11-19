using Unity.Netcode;
using UnityEngine;

public class NetworkLobbyPlacer : NetworkBehaviour
{
    [SerializeField] private Transform[] playerPositions;
    
    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            PlaceNextPlayer(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void OnDestroy()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
        {
            Debug.Log($"Client {clientId} connected to the server.");
            PlaceNextPlayer(clientId);
        }
    }

    private void PlaceNextPlayer(ulong clientId)
    {
        int currentPlayerIndex = NetworkManager.Singleton.ConnectedClients.Count - 1;
        if (currentPlayerIndex < playerPositions.Length)
        {
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.transform.position = playerPositions[currentPlayerIndex].position;
        }
        else
        {
            NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.transform.position = Vector3.zero;
        }
    }
}
