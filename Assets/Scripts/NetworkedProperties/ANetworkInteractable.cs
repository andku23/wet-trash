
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
   
   GameObject gameObject { get ; } 
}
