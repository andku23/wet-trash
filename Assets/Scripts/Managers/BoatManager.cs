using Unity.Netcode;
using UnityEngine;

public class BoatManager : NetworkBehaviour
{
    public static BoatManager Instance;
    [HideInInspector] public NetworkedBoat Boat;
    
    [SerializeField] private GameObject _baseBoatPrefab;

    private void Start()
    {
        Instance = this;
    }

    public void SpawnBoatServer()
    {
        GameObject boat = Instantiate(_baseBoatPrefab);
        boat.transform.position = new Vector3(0.98f, 0, 5.92f);
        NetworkObject networkObject = boat.GetComponent<NetworkObject>();
        networkObject.Spawn();
    }
    
    public void PlaceAttachmentPoint(int shopItemIndex, BoatAttachmentPoint boatAttachmentPoint, float rotationPlaceOffset)
    {
        int attachmentPointIndex = -1;
        for (int i = 0; i < Boat.BoatAttachmentPoints.Count; i++)
        {
            if (boatAttachmentPoint == Boat.BoatAttachmentPoints[i])
            {
                attachmentPointIndex = i;
            }
        }

        if (attachmentPointIndex >= 0)
        {
            PlaceAttachmentPoint_ServerRpc(shopItemIndex, attachmentPointIndex, rotationPlaceOffset);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void PlaceAttachmentPoint_ServerRpc(int shopIndex, int boatAttachmentIndex, float rotationPlaceOffset)
    {
        BoatAttachmentPoint boatAttachmentPoint = Boat.BoatAttachmentPoints[boatAttachmentIndex];
        NetworkObject no = Instantiate(ShopManager.Instance.shopList.items[shopIndex].placePrefab, 
            boatAttachmentPoint.transform.position, boatAttachmentPoint.transform.rotation).GetComponent<NetworkObject>();
        no.transform.Rotate(boatAttachmentPoint.transform.up, rotationPlaceOffset);
        no.Spawn();
        no.transform.parent = Boat.transform;
        boatAttachmentPoint.heldItem.Value = no.NetworkObjectId;
        
        IAttachment attachable = no.GetComponent<IAttachment>();
        if (attachable != null)
        {
            attachable.OnAttach(Boat);
        }
    }
}
