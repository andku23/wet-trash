using Unity.Netcode;
using UnityEngine;

public class BoatManager : NetworkBehaviour
{
    public static BoatManager Instance;
    [HideInInspector] public NetworkedBoat Boat;

    public BoatPartsList BoatPartsList;
    
    [SerializeField] private GameObject _baseBoatPrefab;

    private void Start()
    {
        Instance = this;
    }
    
    //TODO collider ref on boat parts needs to hook itself up on clients

    public void SpawnBoatServer()
    {
        GameObject boat = Instantiate(_baseBoatPrefab);
        NetworkObject networkObject = boat.GetComponent<NetworkObject>();
        networkObject.Spawn();
        boat.transform.position = new Vector3(0.98f, 0.5f, 5.92f);
    }
    
    public void RequestConnectBoatPart(ulong attachedToNetworkID, int attachedToPoint, BoatPartID attachedPart, int attachedPartPoint)
    {
        ConnectBoatPart_ServerRpc( attachedToNetworkID, attachedToPoint, attachedPart, attachedPartPoint);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void ConnectBoatPart_ServerRpc(ulong attachedToNetworkID, int attachedToPoint, BoatPartID attachedPart, int attachedPartPoint)
    {
        Transform attachToPoint = Boat.BoatParts[attachedToNetworkID].ConnectionPoints[attachedToPoint];
        BoatPartData partData = BoatPartsList.boatParts.Find(partData => partData.id == attachedPart);
        GameObject go = Instantiate(partData.prefab);
        BoatPart boatPart = go.GetComponent<BoatPart>();
        go.transform.position = attachToPoint.position;
        go.transform.rotation = attachToPoint.rotation;
        NetworkObject no = go.GetComponent<NetworkObject>();
        no.Spawn();
        no.transform.parent = Boat.transform;
        Vector3 outwardDirection = Boat.BoatParts[attachedToNetworkID].transform.position - attachToPoint.position;
        outwardDirection.Normalize();
        float attachPointDistance = Vector3.Distance(go.transform.position, boatPart.ConnectionPoints[attachedPartPoint].position);
        go.transform.position -= outwardDirection * attachPointDistance;
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
