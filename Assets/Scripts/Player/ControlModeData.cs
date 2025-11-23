using UnityEngine;
using UnityEngine.Serialization;

public class ControlModeData : MonoBehaviour
{
    public GameObject centerHoldingPoint;
    public GameObject rightHoldingPoint;
    public GameObject leftHoldingPoint;

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
}
