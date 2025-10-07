using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class NetworkModeDebugOverlay : MonoBehaviour
{
    private static string _joinCode;
    private bool connectionStarted;
    
    [SerializeField] private TextMeshProUGUI joinCodeText;
    [SerializeField] private GameObject connectionButtons;
    
    public void OnInputEnd(string joinCode)
    {
        _joinCode = joinCode;
    }

    public void StartHost()
    {
        StartHostAsync();
    }

    public void StartClient()
    {
        StartClientAsync();
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
            joinCodeText.text = "Join Code: " + joinCode;
        else 
            joinCodeText.text = "Join Failed idk why";

        OnConnectionFinished();
    }

    public async void StartClientAsync()
    {
        if (connectionStarted) return;
        connectionStarted = true;
        
        await StartClientWithRelay(_joinCode, "udp");
        
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