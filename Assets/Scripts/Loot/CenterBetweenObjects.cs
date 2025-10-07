using UnityEngine;

public class CenterBetweenObjects : MonoBehaviour
{
    
    [SerializeField] private Transform _first;
    [SerializeField] private Transform _last;

    private void Update()
    {
        transform.position = (_first.position + _last.position) / 2f;
    }
}
