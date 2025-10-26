using UnityEngine;

// Whether it can be picked up by crane or not
public interface ICranable
{
    public Vector3 GetAttachPoint();
}
