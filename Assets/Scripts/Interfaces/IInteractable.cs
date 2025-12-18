
using UnityEngine;

public interface IInteractable
{
   public void Interact(ulong networkPlayerID, InteractionButtonType buttonType){}

   public bool EnableInteractable(IHoldable heldObject)
   {
      return true;
   }
   
   public void DisableInteractable() {}
   
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

public enum InteractionButtonType
{
   Interact, //E
   Use, //Left Click
   Back // ESC or something, but not implemented yet
}

