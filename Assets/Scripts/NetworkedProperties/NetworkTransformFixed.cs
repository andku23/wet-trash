using Unity.Netcode.Components;
using UnityEngine;

public class NetworkTransformFixed : NetworkTransform
{
    public void ForceApplyAuthoritativeState()
    {
        ApplyAuthoritativeState();
        InternalOnNetworkSessionSynchronized();
    }
}
