using Unity.Netcode;
using UnityEngine;

public class InteractableSteeringWheel : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject instructions;
    [SerializeField] private NetworkedBoat networkBoat;

    private void Start()
    {
        instructions.SetActive(false);
    }
    
    public void Interact(ulong networkPlayerId)
    {
        if (networkBoat.HasDriver)
        {
            if (networkBoat.DriverID == networkPlayerId)
            {
                networkBoat.RequestToDrive(false);
            }
        }
        else
        {
            networkBoat.RequestToDrive(true);
            instructions.SetActive(false);
        }
    }
    
    public bool EnableInteractable(IHoldable heldObject)
    {
        if (networkBoat.HasDriver)
        {
            instructions.SetActive(false);
            return false;
        }

        if (heldObject != null)
        {
            instructions.SetActive(false);
            return false;
        }
        instructions.SetActive(true);
        return true;
    }
    
    public void DisableInteractable()
    {
        instructions.SetActive(false);
    }
    
    public bool IsInteractionLocked
    {
        get { return networkBoat.HasDriver; ;} 
        set {}
      
    }
    
    public bool IsPersistentInteractable
    {
        get => true;
        set {}
    }
}
