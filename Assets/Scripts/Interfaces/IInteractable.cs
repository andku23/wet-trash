
using UnityEngine;

public interface IInteractable
{
   public void Interact(ulong networkPlayerID){}

   public bool EnableInteractable(IHoldable heldObject)
   {
      return true;
   }
   
   public void DisableInteractable()
   {
   }
   
   // If the interactor is something that takes over player control for a bit
   public bool IsPersistentInteractable
   {
      get { return false;} 
      set {}
   }
   
   // Whether its able to be interacted with or not
   public bool IsInteractionLocked
   {
      get { return false;} 
      set {}
      
   }
   
   GameObject gameObject { get ; } 
}
