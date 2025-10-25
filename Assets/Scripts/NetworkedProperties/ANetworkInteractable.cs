
using UnityEngine;

public interface IInteractable
{
   public void Interact(ulong networkPlayerID){}
   public void SetAsInteractable(bool isInteractable){}
   GameObject gameObject { get ; } 
}
