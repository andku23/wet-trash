using UnityEngine;

public abstract class HeldItemModel : MonoBehaviour
{
    public GameObject CraneAttachPoint;
    public GameObject HoldAttachPoint;
    public GameObject MeshModel;

    public virtual void OnEquip(InteractionController interactionController, PlayerStateData playerState)
    {
        
    }

    public virtual void UseItem(InteractionController interactionController, PlayerStateData playerState)
    {
        
    }
    
    public virtual void OnUnequip(InteractionController interactionController, PlayerStateData playerState)
    {
        
    }
    
    public virtual void SetHighlight(bool isHighlighted)
    {
        if (MeshModel == null) return;
        Material[] mat = MeshModel.GetComponent<Renderer>().materials;
        if(mat.Length < 1) return;
        if (isHighlighted)
        {
            mat[0].EnableKeyword("_EMISSION");
        }
        else
        {
            mat[0].DisableKeyword("_EMISSION");
        }
    }
}
