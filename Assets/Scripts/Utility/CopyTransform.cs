using UnityEngine;

public class CopyTransform : MonoBehaviour
{
    [SerializeField] private bool usePosition;
    [SerializeField] private bool useRotation;
    [SerializeField] private bool useScale;
    
    public GameObject target;
    
    private void LateUpdate()
    {
        if (target == null) return;
        if (usePosition)
        {
            transform.position = target.transform.position;
        }

        if (useRotation)
        {
            transform.rotation = target.transform.rotation;
        }

        if (useScale)
        {
            transform.localScale = target.transform.localScale;
        }
    }
}
