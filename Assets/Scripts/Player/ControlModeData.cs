using UnityEngine;
using UnityEngine.Serialization;

public class ControlModeData : MonoBehaviour
{
    [SerializeField] private GameObject centerHoldingPoint;
    [SerializeField] private GameObject rightHoldingPoint;
    [SerializeField] private GameObject leftHoldingPoint;

    [SerializeField] private GameObject airAttachmentPoint;

    public GameObject GetConnectionPoint(HoldableHandType holdableHandType)
    {
        switch (holdableHandType)
        {
            case HoldableHandType.Left:
                return leftHoldingPoint;
                break;
            case HoldableHandType.Right:
                return rightHoldingPoint;
                break;
            default:
                return centerHoldingPoint;
                break;
        }
    }
    
    public GameObject GetAirAttachPoint()
    {
        return airAttachmentPoint;
    }
}
