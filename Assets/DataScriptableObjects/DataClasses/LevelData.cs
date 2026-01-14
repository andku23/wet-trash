using Unity.Netcode;
using UnityEngine;


public class LevelData: INetworkSerializable
{
    public int MAX_MONSTERS_SPAWNED = 1;
    public int MONSTER_OVERWORLD_SPAWN_PER_HOUR = 1;
    public float MONSTER_DUNGEON_SPAWN_PER_HOUR = 1;

    public LevelData(int maxMonstersSpawned, int monsterOverworldSpawnPerHour, float monsterDungeonSpawnPerHour)
    {
        this.MAX_MONSTERS_SPAWNED = maxMonstersSpawned;
        this.MONSTER_OVERWORLD_SPAWN_PER_HOUR = monsterOverworldSpawnPerHour;
        this.MONSTER_DUNGEON_SPAWN_PER_HOUR = monsterDungeonSpawnPerHour;
    }
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref MAX_MONSTERS_SPAWNED);
        serializer.SerializeValue(ref  MONSTER_OVERWORLD_SPAWN_PER_HOUR);
        serializer.SerializeValue(ref  MONSTER_DUNGEON_SPAWN_PER_HOUR);
    }
}
