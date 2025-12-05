using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class AssignTerrainOnSpawn : MonoBehaviour
{
    private void Start()
    {
        WorldManager.Instance.AssignTerrain(GetComponent<Terrain>());
    }
}
