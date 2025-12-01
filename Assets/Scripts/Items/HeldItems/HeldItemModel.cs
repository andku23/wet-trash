using UnityEngine;

public abstract class HeldItemModel : MonoBehaviour
{
    public GameObject CraneAttachPoint;

    public GameObject HoldAttachPoint;

    public virtual void UseItem(InteractionController interactionController)
    {
        
    }
}
