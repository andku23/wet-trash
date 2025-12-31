using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LobbyScene : NetworkBehaviour
{
    [SerializeField] private Transform[] playerPositions;
    [SerializeField] private TextMeshProUGUI joinCodeText;
    
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject waitForPlayers;
    
    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            startButton.SetActive(true);
            waitForPlayers.SetActive(false);
            PlaceNextPlayerServer(NetworkManager.Singleton.LocalClientId);
        } else
        {
            startButton.SetActive(false);
            waitForPlayers.SetActive(true);
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
            
            GameManager.Instance.SetPlayerPositions_ServerRpc(
                playersToSet, 
                playerPositions[currentPlayerIndex].position,
                playerPositions[currentPlayerIndex].rotation);
        }
        else
        {
            GameManager.Instance.SetPlayerPositions_ServerRpc(
                playersToSet, 
                Vector3.zero,
                Quaternion.identity);
        }
    }
    
    public void ToScavengingScene()
    {
        GameManager.Instance.ChangeScene_ServerRpc(2);
    }
}
