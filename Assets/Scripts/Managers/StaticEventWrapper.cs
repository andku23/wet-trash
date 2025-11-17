using UnityEngine;

public class StaticEventWrapper : MonoBehaviour
{
    public void ToNextGameState()
    {
        GameManager.Instance.RequestToNextGameState();
    }

    public void OpenShop()
    {
        ShopManager.Instance.ViewShop();
    }
}
