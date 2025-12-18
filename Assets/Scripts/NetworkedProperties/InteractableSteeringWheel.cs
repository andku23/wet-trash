using Unity.Netcode;
using UnityEngine;

public class InteractableSteeringWheel : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject instructions;

    public Transform DriverPosition;

    private void Start()
    {
        instructions.SetActive(false);
    }
    
    public void Interact(ulong networkPlayerId, InteractionButtonType buttonType)
    {
        if (BoatManager.Instance.Boat.HasDriver)
        {
            if (BoatManager.Instance.Boat.DriverID == networkPlayerId)
            {
                BoatManager.Instance.Boat.RequestToDrive(false);
                InteractionController.Instance.DisconnectFromPersistentInteractable();
            }
        }
        else
        {
            BoatManager.Instance.Boat.RequestToDrive(true);
            instructions.SetActive(false);
        }
    }
    
    public bool EnableInteractable(IHoldable heldObject)
    {
        if (BoatManager.Instance.Boat.HasDriver)
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
        get { return BoatManager.Instance.Boat.HasDriver; ;} 
        set {}
      
    }
    
    public bool IsPersistentInteractable
    {
        get => true;
        set {}
    }
}
