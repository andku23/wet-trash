using Unity.Netcode;
using UnityEngine;

public class CraneHook : MonoBehaviour, IHoldable
{
    [SerializeField] private HeldObjectType _heldObjectType;
    [SerializeField] private GameObject _connectedParent;
    [SerializeField] private LineRenderer _lineRenderer;

    public GameObject HookTop;
    public GameObject HookBottom;
    public AttachmentCrane AttachmentCrane;

    public HeldObjectType HeldObjectType { get => _heldObjectType; }
    public GameObject ConnectedParent { get => _connectedParent; set => _connectedParent = value; }

    public ulong HeldPlayerID { get; set; }

    private void Update()
    {
        if (AttachmentCrane != null)
        {
            _lineRenderer.SetPosition(0, HookTop.transform.position);
            _lineRenderer.SetPosition(1, AttachmentCrane.CraneHookParent.transform.position);
            
            if (HeldPlayerID == NetworkManager.Singleton.LocalClientId)
            {
                if (Vector3.Distance(HookTop.transform.position, AttachmentCrane.CraneHookParent.transform.position) >
                    AttachmentCrane.MaxCraneDistance)
                {
                    if (AttachmentCrane.LocalState == (int)AttachmentCrane.LocalStates.HookHeld)
                    {
                        AttachmentCrane.DropCraneHook(HeldPlayerID);
                    }
                }
            }
        }
    }

    public Vector3 GetAttachmentOffset()
    {
        return transform.position - HookTop.transform.position;
    }
}
