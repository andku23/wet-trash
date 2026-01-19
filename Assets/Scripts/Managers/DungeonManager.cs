using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class DungeonManager : NetworkBehaviour
{
    public static DungeonManager Instance;
    
    [SerializeField] private CaveRoom[] caveDungeonStartRooms;
    [SerializeField] private CaveRoom[] caveDungeonHallRooms;
    [SerializeField] private CaveRoom[] caveDungeonLeafRooms;
    [SerializeField] private CaveDoor caveDoorPrefab;
    [SerializeField] private CaveDoor surfaceDoorPrefab;
    
    private int DUNGEON_DEPTH = 3;
    private int DUNGEON_SPACING = 200;
    private int MAX_NUM_DUNGEONS = 3;
    
    private int numDungeons;

    private List<CaveRoom> spawnedRooms_c = new List<CaveRoom>();
    private DungeonGenerationInstructions dungeonGenData_c;
    
    private List<Transform> possibleDoorLocations_s = new List<Transform>();

    public CaveRoom[] GetCaveRoomFromEachDungeon()
    {
        CaveRoom[] caveRooms = new CaveRoom[numDungeons];
            
        for (int i = 0; i < numDungeons; i++)
        {
            for (int j = 0; j < spawnedRooms_c.Count; j++)
            {
                if (spawnedRooms_c[j].DungeonNum == i)
                {
                    caveRooms[i] = spawnedRooms_c[j];
                    break;
                }
            }
        }
        
        return caveRooms;
    }
    
    public CaveRoom GetRandomCaveRoom()
    {
        return spawnedRooms_c[Random.Range(0, spawnedRooms_c.Count)];
    }

    // doesnt check if dungeon is in there
    public CaveRoom GetRandomCaveRoom(int dungeonNum, CaveRoom exclude)
    {
        List<CaveRoom> caveRooms = new List<CaveRoom>();
        for (int i = 0; i < spawnedRooms_c.Count; i++)
        {
            if (spawnedRooms_c[i].DungeonNum == dungeonNum && spawnedRooms_c[i] != exclude)
            {
                caveRooms.Add(spawnedRooms_c[i]);
            }
        }
        return caveRooms[Random.Range(0, caveRooms.Count)];
    }

    public NetworkList<ulong> spawnedDoors;
    public enum CaveRoomType
    {
        Entrance,
        Hall,
        Leaf
    }
    
    private void Start()
    {
        Instance = this;
        spawnedDoors = new NetworkList<ulong>();
    }

    private CaveRoom[] GetRoomPoolFromType(CaveRoomType type)
    {
        switch (type)
        {
            case CaveRoomType.Entrance:
                return caveDungeonStartRooms;
            case CaveRoomType.Hall:
                return caveDungeonHallRooms;
            case CaveRoomType.Leaf:
                return caveDungeonLeafRooms;
            default:
                return caveDungeonStartRooms;
        }
    }

    private void ClearDungeon_C()
    {
        for (int i = 0; i < spawnedRooms_c.Count; i++)
        {
            Destroy(spawnedRooms_c[i].gameObject);
        }
        spawnedRooms_c.Clear();
    }

    public void ResetAllRoomSearchFlags()
    {
        for (int i = 0; i < spawnedRooms_c.Count; i++)
        {
            spawnedRooms_c[i].PF_Explored = false;
        }
    }

    public void GenerateSurfaceDoors_S()
    {
        for (int i = 0; i < WorldManager.Instance.SpawnedHoles.Count; i++)
        {
            Transform entranceTransform = WorldManager.Instance.SpawnedHoles[i].transform;
            SpawnLinkedDungeonDoor_S(surfaceDoorPrefab, entranceTransform.position, entranceTransform.rotation, i);
        }
    }
    
    // Just straight up creates the dungeon on the server (so we can check collisions and stuff)
    // then spits out instructions to hand to clients for them to generate too
    public DungeonGenerationInstructions GenerateDungeonAndCreateSteps_S()
    {
        ClearDungeon_C();

        for (int i = 0; i < spawnedDoors.Count; i++)
        {
            NetworkManager.Singleton.SpawnManager.SpawnedObjects[spawnedDoors[i]].Despawn(true);
        }
        spawnedDoors.Clear();
        possibleDoorLocations_s.Clear();
        
        // numDungeons = Random.Range(1, MAX_NUM_DUNGEONS + 1);
        numDungeons = MAX_NUM_DUNGEONS;
        
        // Data so I can create the instructions at the end
        List<int> dungeonRoomIDs = new List<int>();
        List<int> dungeonRoomTypes = new List<int>();
        List<Vector3> doorLocations = new List<Vector3>();
        List<Quaternion> doorRotation = new List<Quaternion>();
        List<int> startingRooms = new List<int>();
        List<DungeonAttachmentInstructionStep> attachmentSteps = new List<DungeonAttachmentInstructionStep>();

        for (int dungeonIndex = 0; dungeonIndex < numDungeons; dungeonIndex++)
        {
            
            // Lists for tracking outermost attachment points
            List<CaveRoom> leafNodes = new List<CaveRoom>();
            List<CaveRoom> nextLeafNodes = new List<CaveRoom>();
        
            // Position of initial room, slightly different for each room
            Vector3 dungeonPosition = new Vector3(dungeonIndex * DUNGEON_SPACING, -1000, 0);
            
            // Instantiate an entrance room and set all of it's values
            int entrancePoolIndex = 0;
            var startRoom = Instantiate(caveDungeonStartRooms[entrancePoolIndex]);
            startRoom.SetSpawnID(spawnedRooms_c.Count);
            startRoom.PoolID = entrancePoolIndex;
            startRoom.DungeonNum = dungeonIndex;
            startRoom.RoomType = CaveRoomType.Entrance;
            startRoom.transform.position = dungeonPosition;
            spawnedRooms_c.Add(startRoom);
            startingRooms.Add(startRoom.SpawnID);
            
            for (int i = 0; i < startRoom.caveDoorPossibleLocations.Length; i++)
            {
                possibleDoorLocations_s.Add(startRoom.caveDoorPossibleLocations[i]);
            }
        
            dungeonRoomIDs.Add(entrancePoolIndex);
            dungeonRoomTypes.Add((int)CaveRoomType.Entrance);
            leafNodes.Add(startRoom);
            
            // Loop through each of the leaf nodes and add rooms until  we
            // get to the total depth
            for (int depth = 0; depth < DUNGEON_DEPTH; depth++)
            {
                for (int leafNodeIndex = 0; leafNodeIndex < leafNodes.Count; leafNodeIndex++)
                {
                    CaveRoomType roomType = (depth == DUNGEON_DEPTH - 1) ? CaveRoomType.Leaf : CaveRoomType.Hall;
                
                    for (int connectPointIdx = 0; connectPointIdx < leafNodes[leafNodeIndex].caveRoomConnectPoints.Length; connectPointIdx++)
                    {
                        if (leafNodes[leafNodeIndex].caveRoomConnectPoints[connectPointIdx].ConnectedPoint != null) continue;
                        var instructionStep 
                            = AttachDungeonRoomAndCreateStep(leafNodes[leafNodeIndex].SpawnID, connectPointIdx, nextLeafNodes, roomType);
                        CaveRoom addedRoom = spawnedRooms_c[spawnedRooms_c.Count - 1];
                        dungeonRoomIDs.Add(addedRoom.PoolID);
                        dungeonRoomTypes.Add((int)addedRoom.RoomType);
                        attachmentSteps.Add(instructionStep);
                        LootManager.Instance.RegisterLootGroupServer(addedRoom);
                        for (int j = 0; j < addedRoom.caveDoorPossibleLocations.Length; j++)
                        {
                            possibleDoorLocations_s.Add(addedRoom.caveDoorPossibleLocations[j]);
                        }
                    }
                }

                leafNodes.Clear();
                leafNodes = nextLeafNodes;
                nextLeafNodes = new List<CaveRoom>();
            }
        }
        
        int numDoors = possibleDoorLocations_s.Count;
        
        if (possibleDoorLocations_s.Count > GameManager.Instance.gameData.NUM_DOORS)
        {
            numDoors = Random.Range(GameManager.Instance.gameData.NUM_DOORS, possibleDoorLocations_s.Count);
        }
            
        VarietyUtilities.Shuffle(possibleDoorLocations_s, 1);
        for (int i = 0; i < numDoors; i++)
        {
            doorLocations.Add(possibleDoorLocations_s[i].position);
            doorRotation.Add(possibleDoorLocations_s[i].rotation);
        }

        for (int i = 0; i < doorLocations.Count; i++)
        {
            SpawnDungeonDoor_S(caveDoorPrefab, doorLocations[i],
                doorRotation[i], 0);
        }
       
        DungeonGenerationInstructions instructions = new DungeonGenerationInstructions();
        instructions.DungeonRoomIDs = dungeonRoomIDs.ToArray();
        instructions.DungeonRoomTypes = dungeonRoomTypes.ToArray();
        instructions.DoorPositions = doorLocations.ToArray();
        instructions.DoorRotations = doorRotation.ToArray();
        instructions.StartingRoomIndexes = startingRooms.ToArray();
        instructions.AttachmentSteps = attachmentSteps.ToArray();
        return instructions;
    }
    
    [ClientRpc]
    public void AssignDungeonGenData_ClientRpc(DungeonGenerationInstructions instructions)
    {
        dungeonGenData_c = instructions;

        Debug.Log(string.Join(',', dungeonGenData_c.DungeonRoomIDs));
        Debug.Log(string.Join(',', dungeonGenData_c.DungeonRoomTypes));
    }
    
    // Used by clients to generate dungeon to match the one on host machine
    public async Awaitable BeginDungeonGeneration_C()
    {
        if (IsServer) return; // Already generated on server
        ClearDungeon_C();
        
        // Fully populate array of all the rooms
        for (int i = 0; i < dungeonGenData_c.DungeonRoomIDs.Length; i++)
        {
            CaveRoom[] roomPool = GetRoomPoolFromType((CaveRoomType)dungeonGenData_c.DungeonRoomTypes[i]);
            spawnedRooms_c.Add(Instantiate(roomPool[dungeonGenData_c.DungeonRoomIDs[i]]));
        }
        
        // Place all the starting rooms in  different locations
        for (int dungeonIndex = 0; dungeonIndex < dungeonGenData_c.StartingRoomIndexes.Length; dungeonIndex++)
        {
            int startRoomIndex = dungeonGenData_c.StartingRoomIndexes[dungeonIndex];
            Vector3 dungeonPosition = new Vector3(dungeonIndex * DUNGEON_SPACING, -1000, 0);
            spawnedRooms_c[startRoomIndex].transform.position = dungeonPosition;
        }
        
        // Attach all the rooms together accordingly
        for (int i = 0; i < dungeonGenData_c.AttachmentSteps.Length; i++)
        {
            var attachStep = dungeonGenData_c.AttachmentSteps[i];
            CaveRoom fromRoom = spawnedRooms_c[attachStep.FromRoomIndex];
            CaveRoom toRoom = spawnedRooms_c[attachStep.ToRoomIndex];
            AttachDungeonRoom(fromRoom, attachStep.FromRoomAttachmentIndex, toRoom, attachStep.ToRoomAttachmentIndex);
        }
    }

    private void AttachDungeonRoom(CaveRoom fromRoom, int fromAttachIndex, CaveRoom toRoom, int toAttachIndex)
    {
        CaveRoomConnectPoint attachPoint = fromRoom.caveRoomConnectPoints[fromAttachIndex];
        CaveRoomConnectPoint nextPoint = toRoom.caveRoomConnectPoints[toAttachIndex];
        
        Vector3 positionOffset = toRoom.transform.position - nextPoint.transform.position;
        float rotationOffset = attachPoint.transform.rotation.eulerAngles.y - nextPoint.transform.rotation.eulerAngles.y - 180;
        
        toRoom.transform.position = attachPoint.transform.position + positionOffset;
        toRoom.transform.RotateAround(attachPoint.transform.position, Vector3.up, rotationOffset);

        attachPoint.ConnectedPoint = nextPoint;
        attachPoint.ConnectedRoom = toRoom;
        attachPoint.Name = "(Connect Node: " + fromRoom.SpawnID + " to " + toRoom.SpawnID + ")";
        
        nextPoint.ConnectedPoint = attachPoint;
        nextPoint.ConnectedRoom = fromRoom;
        nextPoint.Name = "(Connect Node: " + toRoom.SpawnID + " to " + fromRoom.SpawnID + ")";
    }

    private DungeonAttachmentInstructionStep AttachDungeonRoomAndCreateStep(int fromRoomIndex, int attachPointIndex, 
        List<CaveRoom> nextTierLeafNodes, CaveRoomType roomType)
    {
        var roomPool = GetRoomPoolFromType(roomType);
        int poolIndex = Random.Range(0, roomPool.Length);
        CaveRoom fromRoom = spawnedRooms_c[fromRoomIndex];
        CaveRoom toRoom = Instantiate(roomPool[poolIndex]);
        nextTierLeafNodes.Add(toRoom);
        toRoom.PoolID = poolIndex;
        toRoom.DungeonNum = fromRoom.DungeonNum;
        toRoom.RoomType = roomType;
        toRoom.SetSpawnID(spawnedRooms_c.Count);
        spawnedRooms_c.Add(toRoom);
        int attachToIndex = Random.Range(0, toRoom.caveRoomConnectPoints.Length);
        
        AttachDungeonRoom(fromRoom, attachPointIndex, toRoom, attachToIndex);
        
        //Generate instruction step
        DungeonAttachmentInstructionStep instructionStep = new DungeonAttachmentInstructionStep();
        instructionStep.FromRoomIndex = fromRoomIndex;
        instructionStep.FromRoomAttachmentIndex = attachPointIndex;
        instructionStep.ToRoomIndex = toRoom.SpawnID;
        instructionStep.ToRoomAttachmentIndex = attachToIndex;
        
        return instructionStep;
    }

    private void SpawnDungeonDoor_S(CaveDoor prefab, Vector3 position, Quaternion rotation, int linkIndex)
    {
        CaveDoor caveDoor = Instantiate(prefab, position, rotation);
        NetworkObject no2 = caveDoor.GetComponent<NetworkObject>();
        no2.Spawn();
        caveDoor.DestinationDoorID.Value = linkIndex;
        spawnedDoors.Add(no2.NetworkObjectId);
    }
    
    private void SpawnLinkedDungeonDoor_S(CaveDoor prefab, Vector3 position, Quaternion rotation, int linkIndex)
    {
        SpawnDungeonDoor_S(prefab, position, rotation, linkIndex);
        CaveDoor dungeonDoor = NetworkManager.Singleton.SpawnManager.SpawnedObjects[spawnedDoors[linkIndex]]
            .GetComponent<CaveDoor>();
        dungeonDoor.DestinationDoorID.Value = spawnedDoors.Count - 1;
    }
}

public class DungeonGenerationInstructions : INetworkSerializable
{
    // These 2 arrays are the same length and correspond to each other
    // as in the type at one index is the caveroom of that id
    public int[] DungeonRoomIDs;
    public int[] DungeonRoomTypes;

    public Vector3[] DoorPositions;
    public Quaternion[] DoorRotations;

    public DungeonAttachmentInstructionStep[] AttachmentSteps;
    public int[] StartingRoomIndexes; // array of all the indexes that have starting rooms, array size equal to how many starting rooms
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref DungeonRoomIDs);
        serializer.SerializeValue(ref DungeonRoomTypes);
        serializer.SerializeValue(ref DoorPositions);
        serializer.SerializeValue(ref DoorRotations);
        serializer.SerializeValue(ref AttachmentSteps);
        serializer.SerializeValue(ref StartingRoomIndexes);
    }
}

[Serializable]
public struct DungeonAttachmentInstructionStep : INetworkSerializeByMemcpy
{
    public int FromRoomIndex;
    public int ToRoomIndex;
    public int FromRoomAttachmentIndex;
    public int ToRoomAttachmentIndex;
}
