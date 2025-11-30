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

    public void RegisterBoatPartServer(NetworkObject boatPartNO, BoatPart boatPart)
    {
        Boat.BoatParts.Add(boatPartNO.NetworkObjectId, boatPart);
        foreach (BoatAttachmentPoint attachmentPoint in boatPart.AttachmentPoints)
        {
            Boat.BoatAttachmentPoints.Add(attachmentPoint);
        }
    }

    public void SpawnBoatServer()
    {
        GameObject boat = Instantiate(_baseBoatPrefab);
        NetworkObject networkObject = boat.GetComponent<NetworkObject>();
        networkObject.Spawn();
        //boat.transform.position = new Vector3(0.98f, 0.0f, 5.92f);
    }

    public void PlaceAtConnectionPoint(GameObject boatPart, Transform boatPartPoint, 
        GameObject addedPart, Transform addedPartPoint)
    {
        addedPart.transform.position = boatPartPoint.position;
        addedPart.transform.rotation = boatPartPoint.rotation;
        Vector3 outwardDirection = boatPart.transform.position - boatPartPoint.position;
        outwardDirection.Normalize();
        float attachPointDistance = Vector3.Distance(addedPart.transform.position, addedPartPoint.position);
        addedPart.transform.position -= outwardDirection * attachPointDistance;
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
        NetworkObject no = go.GetComponent<NetworkObject>();
        BoatPart boatPart = go.GetComponent<BoatPart>();
        
        
        PlaceAtConnectionPoint(Boat.BoatParts[attachedToNetworkID].gameObject, 
            Boat.BoatParts[attachedToNetworkID].ConnectionPoints[attachedToPoint],
            go, boatPart.ConnectionPoints[attachedPartPoint]);
        
        no.Spawn();
        no.transform.parent = Boat.transform;
       
        RegisterBoatPartServer(no, boatPart);
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
