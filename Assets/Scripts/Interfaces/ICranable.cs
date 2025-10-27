using UnityEngine;

// Whether it can be picked up by crane or not
public interface ICranable
{
    public Vector3 GetAttachPoint();

    // This is gonna be whats gets attached to the cran
    public GameObject GetLocalModel();
}
