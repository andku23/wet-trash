using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StaticEventWrapper : NetworkBehaviour
{
    private bool isTransitioningScenes;
    
    public void ToNextGameState()
    {
        GameManager.Instance.RequestToNextGameState();
    }

    public void OpenShop()
    {
        ShopManager.Instance.ViewShop();
    }

    public void ToScavengingScene()
    {
        GameManager.Instance.ChangeScene_ServerRpc(2);
    }
}
