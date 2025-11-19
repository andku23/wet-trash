using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LobbyScene : NetworkBehaviour
{
    [SerializeField] private Transform[] playerPositions;
    [SerializeField] private TextMeshProUGUI joinCodeText;
    
    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            PlaceNextPlayerServer(NetworkManager.Singleton.LocalClientId);
        }

        Cursor.lockState = CursorLockMode.None;
        joinCodeText.text = NetworkModeConnector.Instance.JoinCode;
    }

    private void OnDestroy()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    public void CopyCode()
    {
        GUIUtility.systemCopyBuffer = NetworkModeConnector.Instance.JoinCode;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (IsServer)
        {
            PlaceNextPlayerServer(clientId);
        }
    }

    private void PlaceNextPlayerServer(ulong clientId)
    {
        int currentPlayerIndex = NetworkManager.Singleton.ConnectedClients.Count - 1;
        ulong[] playersToSet = new ulong[1];
        playersToSet[0] = clientId;
        if (currentPlayerIndex < playerPositions.Length)
        {
            
            GameManager.Instance.SetPlayerPositions_ServerRpc(playersToSet, playerPositions[currentPlayerIndex].position);
        }
        else
        {
            GameManager.Instance.SetPlayerPositions_ServerRpc(playersToSet, Vector3.zero);
        }
    }
    
    public void ToScavengingScene()
    {
        GameManager.Instance.ChangeScene_ServerRpc(2);
    }
}
