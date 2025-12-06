using Unity.Netcode;
using UnityEngine;


public class LevelData: INetworkSerializable
{
    public int MAX_MONSTERS_SPAWNED = 1;
    public int MONSTER_SPAWN_PER_HOUR = 1;

    public LevelData(int MAX_MONSTERS_SPAWNED, int MONSTER_SPAWN_PER_HOUR)
    {
        this.MAX_MONSTERS_SPAWNED = MAX_MONSTERS_SPAWNED;
        this.MONSTER_SPAWN_PER_HOUR = MONSTER_SPAWN_PER_HOUR;
    }
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref MAX_MONSTERS_SPAWNED);
        serializer.SerializeValue(ref  MONSTER_SPAWN_PER_HOUR);
    }
}
