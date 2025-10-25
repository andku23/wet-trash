using UnityEngine;

public class CraneHook : MonoBehaviour, IHoldable
{
    [SerializeField] private HeldObjectType _heldObjectType;
    [SerializeField] private GameObject _connectedParent;
    
    public HeldObjectType HeldObjectType { get => _heldObjectType; }
    public GameObject ConnectedParent { get => _connectedParent; set => _connectedParent = value; }
}
