using UnityEngine;

public class CaveRoom : MonoBehaviour
{
    public CaveRoomConnectPoint[] caveRoomConnectPoints;
    public Transform[] caveDoorPossibleLocations;

    public int PoolID;
    public int SpawnID;
    public DungeonManager.CaveRoomType RoomType;
}
