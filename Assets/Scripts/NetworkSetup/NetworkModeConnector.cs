using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class NetworkModeConnector : MonoBehaviour
{
    public string JoinCode;
    private bool connectionStarted;
    public static NetworkModeConnector Instance;
    
    [SerializeField] private TextMeshProUGUI joinCodeText;
    [SerializeField] private GameObject connectionButtons;
    [SerializeField] private UnityTransport transport;

    private void Start()
    {
        Instance = this;
        transport = FindObjectsByType<UnityTransport>(FindObjectsInactive.Include, FindObjectsSortMode.None)[0];
    }
    
    public void OnInputEnd(string joinCode)
    {
        JoinCode = joinCode;
    }

    public void StartHost()
    {
        if (transport.Protocol == UnityTransport.ProtocolType.UnityTransport)
        {
            NetworkManager.Singleton.StartHost();
            OnConnectionFinished();
        }
        else
        {
            StartHostAsync();
        }
    }

    public void StartClient()
    {
        if (transport.Protocol == UnityTransport.ProtocolType.UnityTransport)
        {
            NetworkManager.Singleton.StartClient();
            OnConnectionFinished();
        }
        else
        {
            StartClientAsync();
        }
    }

    private void OnConnectionFinished()
    {
        connectionButtons.SetActive(false);
        connectionStarted = false;
    }

    public async void StartHostAsync()
    {
        if (connectionStarted) return;
        connectionStarted = true;
        string joinCode = await StartHostWithRelay(4, "udp");
        if(joinCode != null)
            JoinCode = joinCode;
        else 
            Debug.Log("Join Failed idk why");

        OnConnectionFinished();
    }

    public async void StartClientAsync()
    {
        if (connectionStarted) return;
        connectionStarted = true;
        
        await StartClientWithRelay(JoinCode, "udp");
        
        OnConnectionFinished();
    }
    
    public async Task<string> StartHostWithRelay(int maxConnections, string connectionType)
    {
        
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        
        var allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
        var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        return NetworkManager.Singleton.StartHost() ? joinCode : null;
    }
    
    public static async Task<bool> StartClientWithRelay(string joinCode, string connectionType)
    {
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        var allocation = await RelayService.Instance.JoinAllocationAsync(joinCode: joinCode);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, connectionType));
        return !string.IsNullOrEmpty(joinCode) && NetworkManager.Singleton.StartClient();
    }
}