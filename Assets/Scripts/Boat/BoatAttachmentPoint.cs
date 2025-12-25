using Unity.Netcode;
using UnityEngine;

public class BoatAttachmentPoint : NetworkBehaviour
{
    public NetworkVariable<ulong> heldItem = new NetworkVariable<ulong>(0);
    public NetworkVariable<int> index = new NetworkVariable<int>(0);

    public void OnAttach()
    {
        
    }
    
    public void OnDetach()
    {
        
    }
}
