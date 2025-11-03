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
                Debug.Log("undrive");
                networkBoat.RequestToDrive(false);
            }
        }
        else
        {
            networkBoat.RequestToDrive(true);
            
        }
    }
    
    public bool EnableInteractable(IHoldable heldObject)
    {
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
