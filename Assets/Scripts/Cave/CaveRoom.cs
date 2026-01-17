using UnityEngine;
using UnityEngine.Splines;

public class CaveRoom : LootGroup
{
    public CaveRoomConnectPoint[] caveRoomConnectPoints; // The literal connection points where other rooms can connect
    public CaveTraverseNode caveRoomCenterTraverse; // The literal connection points where other rooms can connect
    public Transform[] caveDoorPossibleLocations; // Spots where doors can spawn
    public SplineContainer navigationSpline;

    [HideInInspector] public int PoolID; // The index of the room type
    [HideInInspector] public int DungeonNum; // Usually 3 dungeons spawn per day, this is which one the room belong to
    [HideInInspector] public DungeonManager.CaveRoomType RoomType;

    [HideInInspector] public bool PF_Explored;
    [HideInInspector] public CaveRoomConnectPoint PF_ParentConnectPoint;

    private int _spawnID;
    public int SpawnID { get { return _spawnID;}}

    private void Start()
    {
        foreach (var point in caveRoomConnectPoints)
        {
            point.SourceRoom = this;
        }
        
        caveRoomCenterTraverse.SourceRoom = this;
    }

    public void SetSpawnID(int spawnID)
    {
        _spawnID = spawnID;
        caveRoomCenterTraverse.Name = "(Center Node: " + SpawnID + ")";
    }
}
