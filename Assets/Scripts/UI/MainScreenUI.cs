using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainScreenUI : NetworkBehaviour
{
    public static MainScreenUI Instance;

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
        NetworkManager.Singleton.SceneManager.ActiveSceneSynchronizationEnabled = true;
        GameManager.Instance.ChangeScene_ServerRpc(1);
        gameObject.SetActive(false);
    }
    
    public void JoinGame()
    {
        NetworkModeConnector.Instance.StartClient();
        gameObject.SetActive(false);
    }
}
