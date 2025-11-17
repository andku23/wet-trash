using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        NetworkManager.Singleton.SceneManager.ActiveSceneSynchronizationEnabled = true;
        NetworkManager.Singleton.SceneManager.LoadScene("Scavenging", LoadSceneMode.Additive);
        gameObject.SetActive(false);
    }
    
    public void JoinGame()
    {
        NetworkModeConnector.Instance.StartClient();
        NetworkManager.Singleton.SceneManager.ActiveSceneSynchronizationEnabled = true;
        NetworkManager.Singleton.SceneManager.LoadScene("Scavenging", LoadSceneMode.Additive);
        gameObject.SetActive(false);
    }
}
