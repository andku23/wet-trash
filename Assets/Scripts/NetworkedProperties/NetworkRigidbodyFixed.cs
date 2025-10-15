using Unity.Netcode.Components;
using UnityEngine;

public class NetworkRigidbodyFixed : NetworkRigidbody
{
    public void ForceApplyAuthoritativeState()
    {
        InternalOnNetworkSessionSynchronized();
    }
}
