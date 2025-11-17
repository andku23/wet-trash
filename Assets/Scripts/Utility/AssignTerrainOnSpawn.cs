using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class AssignTerrainOnSpawn : MonoBehaviour
{
    private void Start()
    {
        TerrainManager.Instance.AssignTerrain(GetComponent<Terrain>());
    }
}
