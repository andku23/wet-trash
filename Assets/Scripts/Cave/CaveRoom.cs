using UnityEngine;
using UnityEngine.Splines;

public class CaveRoom : LootGroup
{
    public CaveRoomConnectPoint[] caveRoomConnectPoints; // The literal connection points where other rooms can connect
    public Transform[] caveDoorPossibleLocations; // Spots where doors can spawn
    public SplineContainer navigationSpline;

    [HideInInspector] public int PoolID; // The index of the room type
    [HideInInspector] public int SpawnID; // The spot in the total array of rooms
    [HideInInspector] public int DungeonNum; // Usually 3 dungeons spawn per day, this is which one the room belong to
    [HideInInspector] public DungeonManager.CaveRoomType RoomType;
}
