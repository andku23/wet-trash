using UnityEngine;

public class CaveRoom : LootGroup
{
    public CaveRoomConnectPoint[] caveRoomConnectPoints;
    public Transform[] caveDoorPossibleLocations;

    [HideInInspector] public int PoolID;
    [HideInInspector] public int SpawnID;
    [HideInInspector] public DungeonManager.CaveRoomType RoomType;
}
