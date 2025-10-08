
using UnityEngine;

public interface IInteractable
{
   public void Interact(){}
   public void SetAsInteractable(bool isInteractable){}
   GameObject gameObject { get ; } 
}
