using Unity.Netcode;
using UnityEngine;

public class MainScreenUI : NetworkBehaviour
{
    public static MainScreenUI Instance;

    [SerializeField] private Panel HostingScreen;
    
    private void Start()
    {
        if(Instance == null) Instance = this;
    }
    
    public void OnInputEnd(string joinCode)
    {
        NetworkModeConnector.Instance.JoinCode = joinCode;
    }

    public void HostGame()
    {
        NetworkModeConnector.Instance.StartHost();
    }
    
    public void JoinGame()
    {
        NetworkModeConnector.Instance.StartClient();
    }
}
