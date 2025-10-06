using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class SetIP : MonoBehaviour
{
    public void OnInputEnd(string text)
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(text,(ushort)7777);
    }
}
