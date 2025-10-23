using Unity.Netcode;
using UnityEngine;

public class BoatAttachmentPoint : NetworkBehaviour
{
    public NetworkVariable<ulong> heldItem = new NetworkVariable<ulong>(0);
}
