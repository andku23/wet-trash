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

    private List<CaveRoom> spawnedRooms_c = new List<CaveRoom>();
    private DungeonGenerationInstructions dungeonGenData_c;
    
    private List<Transform> possibleDoorLocations_s = new List<Transform>();

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
        
        // Data so I can create the instructions at the end
        List<int> dungeonRoomIDs = new List<int>();
        List<int> dungeonRoomTypes = new List<int>();
        List<Vector3> doorLocations = new List<Vector3>();
        List<Quaternion> doorRotation = new List<Quaternion>();
        List<DungeonAttachmentInstructionStep> attachmentSteps = new List<DungeonAttachmentInstructionStep>();
        
        // Lists for tracking outermost attachment points
        List<CaveRoom> leafNodes = new List<CaveRoom>();
        List<CaveRoom> nextLeafNodes = new List<CaveRoom>();
        
        Vector3 dungeonPosition = new Vector3(0, -1000, 0);

        int entrancePoolIndex = 0;
        var startRoom = Instantiate(caveDungeonStartRooms[entrancePoolIndex]);
        startRoom.SpawnID = 0;
        startRoom.PoolID = entrancePoolIndex;
        startRoom.RoomType = CaveRoomType.Entrance;
        startRoom.transform.position = dungeonPosition;
        spawnedRooms_c.Add(startRoom);

        for (int i = 0; i < startRoom.caveDoorPossibleLocations.Length; i++)
        {
            possibleDoorLocations_s.Add(startRoom.caveDoorPossibleLocations[i]);
        }
        
        dungeonRoomIDs.Add(entrancePoolIndex);
        dungeonRoomTypes.Add((int)CaveRoomType.Entrance);
        
        leafNodes.Add(startRoom);

        for (int depth = 0; depth < DUNGEON_DEPTH; depth++)
        {
            for (int leafNodeIndex = 0; leafNodeIndex < leafNodes.Count; leafNodeIndex++)
            {
                CaveRoomType roomType = (depth == DUNGEON_DEPTH - 1) ? CaveRoomType.Leaf : CaveRoomType.Hall;
                
                for (int connectPointIdx = 0; connectPointIdx < leafNodes[leafNodeIndex].caveRoomConnectPoints.Length; connectPointIdx++)
                {
                    if (leafNodes[leafNodeIndex].caveRoomConnectPoints[connectPointIdx].IsConnected) continue;
                    var instructionStep 
                        = AttachDungeonRoomAndCreateStep(leafNodes[leafNodeIndex].SpawnID, connectPointIdx, nextLeafNodes, roomType);
                    CaveRoom addedRoom = spawnedRooms_c[spawnedRooms_c.Count - 1];
                    dungeonRoomIDs.Add(addedRoom.PoolID);
                    dungeonRoomTypes.Add((int)addedRoom.RoomType);
                    attachmentSteps.Add(instructionStep);
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
        
        Vector3 dungeonPosition = new Vector3(0, -1000, 0);
        
        CaveRoom[] roomPoolEntrance = GetRoomPoolFromType((CaveRoomType)dungeonGenData_c.DungeonRoomTypes[0]);
        spawnedRooms_c.Add(Instantiate(roomPoolEntrance[dungeonGenData_c.DungeonRoomIDs[0]]));
        spawnedRooms_c[0].transform.position = dungeonPosition;
        
        for (int i = 1; i < dungeonGenData_c.DungeonRoomIDs.Length; i++)
        {
            CaveRoom[] roomPool = GetRoomPoolFromType((CaveRoomType)dungeonGenData_c.DungeonRoomTypes[i]);
            spawnedRooms_c.Add(Instantiate(roomPool[dungeonGenData_c.DungeonRoomIDs[i]]));
        }
        
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

        attachPoint.IsConnected = true;
        nextPoint.IsConnected = true;
    }

    private DungeonAttachmentInstructionStep AttachDungeonRoomAndCreateStep(int fromRoomIndex, int attachPointIndex, 
        List<CaveRoom> nextTierLeafNodes, CaveRoomType roomType)
    {
        var roomPool = GetRoomPoolFromType(roomType);
        int poolIndex = Random.Range(0, roomPool.Length);
        CaveRoom fromRoom = spawnedRooms_c[fromRoomIndex];
        CaveRoom toRoom = Instantiate(roomPool[poolIndex]);
        spawnedRooms_c.Add(toRoom);
        nextTierLeafNodes.Add(toRoom);
        toRoom.PoolID = poolIndex;
        toRoom.RoomType = roomType;
        toRoom.SpawnID = spawnedRooms_c.Count - 1;
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
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref DungeonRoomIDs);
        serializer.SerializeValue(ref DungeonRoomTypes);
        serializer.SerializeValue(ref DoorPositions);
        serializer.SerializeValue(ref DoorRotations);
        serializer.SerializeValue(ref AttachmentSteps);
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
