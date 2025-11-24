using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BoatPart : NetworkBehaviour
{
    public List<Transform> ConnectionPoints;
    public List<BoatAttachmentPoint> AttachmentPoints;
    public InteractableSteeringWheel SteeringWheel;
    
    [SerializeField] private ColliderReference colliderReference;
    
    public override void OnNetworkSpawn()
    {
        if (colliderReference != null)
        {
            colliderReference.reference = BoatManager.Instance.Boat.gameObject;
        }
    }
}
