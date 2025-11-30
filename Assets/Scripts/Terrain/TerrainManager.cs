using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class TerrainManager : NetworkBehaviour
{
    public static TerrainManager Instance;
    [SerializeField] private GameObject[] caveRoomEnds;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private GameObject[] lootGroupPrefabs;
    public int NUM_OF_HOLES;
    public int NUM_OF_LOOT_GROUPS;

    public List<GameObject> SpawnedHoles;
    public List<GameObject> SpawnedLootGroups;
    public List<NetworkObject> SpawnedEnemies;

    private Vector2[] currentHolePositions;
    private Vector2[] currentLootGroupPositions;
    
    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    [SerializeField] private Terrain _terrain;
    
    private NativeArray<float> noiseMap;
    private NativeArray<float> holesMap;
    private JobHandle noiseJobHandle;
    private JobHandle holesJobHandle;
    private bool terrainGenerationRequested = false;
    
    [ServerRpc]
    public void GenerateTerrain_ServerRpc()
    {
        int seed = Random.Range(0, 999999);
        Vector2[] holePosition = new Vector2[NUM_OF_HOLES];
        Vector2[] lootGroupPosition = new Vector2[NUM_OF_LOOT_GROUPS];

        for (int i = 0; i < NUM_OF_HOLES; i++)
        {
            holePosition[i] = new Vector2(
                Random.Range(32, _terrain.terrainData.heightmapResolution - 32),
                Random.Range(32, _terrain.terrainData.heightmapResolution - 32));
        }
        
        for (int i = 0; i < NUM_OF_LOOT_GROUPS; i++)
        {
            lootGroupPosition[i] = new Vector2(
                Random.Range(32, _terrain.terrainData.heightmapResolution - 32),
                Random.Range(32, _terrain.terrainData.heightmapResolution - 32));
        }
        
        GenerateTerrain_ClientRpc(seed,holePosition, lootGroupPosition);
    }
    
    [ClientRpc]
    private void GenerateTerrain_ClientRpc(int seed, Vector2[] holePositions, Vector2[] lootGroupPositions)
    {
        int resolution = _terrain.terrainData.heightmapResolution; 
        float scale = 5f;

        currentHolePositions = holePositions;
        currentLootGroupPositions = lootGroupPositions;
        
        noiseMap = new NativeArray<float>(resolution * resolution, Allocator.Persistent);
        holesMap = new NativeArray<float>(resolution * resolution, Allocator.Persistent);
        
        GenerateNoiseJob noiseJob = new GenerateNoiseJob
        {
            NoiseMap = noiseMap,
            Resolution = resolution,
            Scale = scale,
            Seed = seed
        };
        
        GenerateHolesJob holesJob = new GenerateHolesJob
        {
            NoiseMap = holesMap,
            Resolution = resolution,
            Scale = 50f,
            Seed = seed
        };
        
        noiseJobHandle = noiseJob.Schedule(noiseMap.Length, 64);
        holesJobHandle = holesJob.Schedule(holesMap.Length, 64);
        terrainGenerationRequested = true;
        
    }
    
    public Vector3 GetRandomPointOnTerrain()
    {
        Vector3 terrainSize = _terrain.terrainData.size;
        Vector3 terrainPosition = _terrain.transform.position;

        float randomX = Random.Range(terrainPosition.x, terrainPosition.x + terrainSize.x);
        float randomZ = Random.Range(terrainPosition.z, terrainPosition.z + terrainSize.z);

        return GetPointOnTerrain(randomX, randomZ);
    }
    
    public Vector3 GetPointOnTerrain(float x, float z)
    {
        Vector3 terrainPosition = _terrain.transform.position;

        Vector3 pointOnXZPlane = new Vector3(x, 0, z);
        float height = _terrain.SampleHeight(pointOnXZPlane);

        return new Vector3(x, height + terrainPosition.y, z);
    }

    private LootGroup CreateLootGroup(int xPos, int zPos)
    {
        float[,] heights = _terrain.terrainData.GetHeights(xPos, zPos,  1, 1);
        Vector3 lowestHolePosition = new Vector3(xPos, 0, zPos);
        lowestHolePosition.y = heights[0, 0];
        
        GameObject lootGroup = Instantiate(lootGroupPrefabs[Random.Range(0, lootGroupPrefabs.Length)]);
        lowestHolePosition.x /=  _terrain.terrainData.heightmapResolution;
        lowestHolePosition.z /=  _terrain.terrainData.heightmapResolution;
        lowestHolePosition.x *=  _terrain.terrainData.size.x;
        lowestHolePosition.y *=  _terrain.terrainData.size.y;
        lowestHolePosition.z *=  _terrain.terrainData.size.z;
        //lowestHolePosition = Vector3.Scale(lowestHolePosition, _terrain.terrainData.size);
        lootGroup.transform.position = lowestHolePosition + _terrain.transform.position;
        
        return lootGroup.GetComponent<LootGroup>();
    }

    public void AssignTerrain(Terrain terrain)
    {
        _terrain = terrain;
    }

    private LootGroup CreateHole(int xPos, int zPos, int holeWidth, int holeHeight)
    {
        bool[,] holeMap = new bool[holeWidth, holeHeight];
        float[,] heightMap = new float[holeWidth, holeHeight];
        Vector2 originOfCircle = new Vector2(holeWidth / 2, holeHeight / 2);
        int xbase = xPos - holeWidth / 2;
        int zbase = zPos - holeHeight / 2;
        
        float[,] heights = _terrain.terrainData.GetHeights(xbase, zbase,  holeWidth, holeHeight);
        
        Vector3 lowestHolePosition = new Vector3(xPos, 0, zPos);
        lowestHolePosition.y = heights[holeWidth/2, holeHeight/2] - 0.2f;
        float lowestHoleHeight = lowestHolePosition.y;
        float fullSize = holeWidth / 2;
        float holeSize = holeWidth / 4;
        float outsideToHoleEdge = fullSize - holeSize;
        
        for (int x = 0; x < holeWidth; x++)
        {
            for (int y = 0; y < holeHeight; y++)
            {
                float currentHeight = heights[x, y];
                float distanceFromCenter = Vector2.Distance(new Vector2(x, y), originOfCircle);
                
                heightMap[x, y] = currentHeight;

                float targetDepthOffset = currentHeight - lowestHoleHeight;
                
                heightMap[x, y] -= targetDepthOffset * (1.0f-Mathf.Clamp((distanceFromCenter - holeSize) / outsideToHoleEdge, 0.0f, 1.0f));
                
                if (distanceFromCenter <= holeSize)
                {
                    holeMap[x, y] = false; // This is a hole
                }
                else
                {
                    holeMap[x, y] = true; // This is solid terrain
                }
            }
        }
        _terrain.terrainData.SetHoles(xbase, zbase, holeMap);
        _terrain.terrainData.SetHeights(xbase, zbase, heightMap);
        
        // TODO It currently places the hole at the lowest height which is too low
        // and it needs to place it at the edge height where the hole starts drawing
        // and while im at it, i should make the height lerp to the edge height 
        
        GameObject caveRoom = Instantiate(caveRoomEnds[Random.Range(0, caveRoomEnds.Length)]);
        lowestHolePosition.x /=  _terrain.terrainData.heightmapResolution;
        lowestHolePosition.z /=  _terrain.terrainData.heightmapResolution;
        lowestHolePosition.x *=  _terrain.terrainData.size.x;
        lowestHolePosition.y *=  _terrain.terrainData.size.y;
        lowestHolePosition.z *=  _terrain.terrainData.size.z;
        caveRoom.transform.position = lowestHolePosition + _terrain.transform.position;
        
        return caveRoom.GetComponent<LootGroup>();
    }

    private void ClearAllHoles()
    {
        bool[,] clearHoles  = new bool[
            _terrain.terrainData.heightmapResolution - 1,
            _terrain.terrainData.heightmapResolution - 1];

        for (int holeIndex = 0; holeIndex < SpawnedHoles.Count; holeIndex++)
        {
            Destroy(SpawnedHoles[holeIndex]);
        }
        SpawnedHoles.Clear();
        
        for (int lootGroupIndex = 0; lootGroupIndex < SpawnedLootGroups.Count; lootGroupIndex++)
        {
            Destroy(SpawnedLootGroups[lootGroupIndex]);
        }
        SpawnedLootGroups.Clear();
        
        for (int enemyIndex = 0; enemyIndex < SpawnedEnemies.Count; enemyIndex++)
        {
            SpawnedEnemies[enemyIndex].Despawn(true);
        }
        SpawnedEnemies.Clear();

        for (int i = 0; i < _terrain.terrainData.heightmapResolution - 1; i++)
        {
            for (int j = 0; j < _terrain.terrainData.heightmapResolution - 1; j++)
            {
                clearHoles[i, j] = true;
            }
        }
        _terrain.terrainData.SetHoles(0, 0, clearHoles);
        LootManager.Instance.ResetLootHolesServer();
    }
    
    private void Update()
    {
        if (!terrainGenerationRequested) return;
        if (noiseJobHandle.IsCompleted && holesJobHandle.IsCompleted)
        {
            noiseJobHandle.Complete(); // Ensure the job is finished
            holesJobHandle.Complete();
            terrainGenerationRequested = false;

          float[,] heights = new float[
                _terrain.terrainData.heightmapResolution,
                _terrain.terrainData.heightmapResolution];
          
          ClearAllHoles();

            for (int i = 0; i < _terrain.terrainData.heightmapResolution; i++)
            {
                for (int j = 0; j < _terrain.terrainData.heightmapResolution; j++)
                {
                    float xRatio = i/(float)_terrain.terrainData.heightmapResolution;
                    float zRatio = (j/(float)_terrain.terrainData.heightmapResolution);
                    float xInitialPosition = 1f - 2f * Mathf.Abs(xRatio - 0.5f);
                    float zInitialPosition = 1f - 2f * Mathf.Abs(zRatio - 0.5f);
                    float height = 0.6f*(xInitialPosition + zInitialPosition)/2f + 0.25f
                                   + 0.55f * holesMap[i * _terrain.terrainData.heightmapResolution + j]
                                   + 0.05f*noiseMap[i*_terrain.terrainData.heightmapResolution + j];
                    heights[i, j] = height;
                }
            }
        
            _terrain.terrainData.SetHeights(0, 0, heights);
            
            SpawnedHoles = new List<GameObject>();
            SpawnedEnemies = new List<NetworkObject>();
            SpawnedLootGroups = new List<GameObject>();
            for (int i = 0; i < currentHolePositions.Length; i++)
            {
                LootGroup hole = CreateHole((int)currentHolePositions[i].x, 
                    (int)currentHolePositions[i].y,
                    32, 32);
                
                SpawnedHoles.Add(hole.gameObject);

                if (IsServer)
                {
                    NetworkObject no = Instantiate(enemyPrefabs[0],
                        hole.transform.position,
                        Quaternion.identity
                    ).GetComponent<NetworkObject>();
                    no.Spawn();
                    SpawnedEnemies.Add(no);
                    LootManager.Instance.RegisterLootGroupServer(hole);
                }
            }
            
            for (int i = 0; i < currentLootGroupPositions.Length; i++)
            {
                LootGroup lootGroup = CreateLootGroup(
                    (int)currentLootGroupPositions[i].x, 
                    (int)currentLootGroupPositions[i].y);
                
                SpawnedLootGroups.Add(lootGroup.gameObject);

                if (IsServer)
                {
                    LootManager.Instance.RegisterLootGroupServer(lootGroup);
                }
            }

            if (IsServer)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 randomPointOnTerrain = GetRandomPointOnTerrain();
                    randomPointOnTerrain.y = Random.Range(0, randomPointOnTerrain.y);
                    NetworkObject no = Instantiate(enemyPrefabs[1],
                        randomPointOnTerrain,
                        Quaternion.identity
                    ).GetComponent<NetworkObject>();
                    no.Spawn();
                    SpawnedEnemies.Add(no);
                }
            }

            GameManager.Instance.PlayerWaitResponse_ServerRpc(NetworkManager.Singleton.LocalClientId);

            noiseMap.Dispose();
            holesMap.Dispose();
        }
    }
   
}
//For noise functions

public struct GenerateNoiseJob : IJobParallelFor
{
    public NativeArray<float> NoiseMap; // Output array for noise values
    public int Resolution; // Resolution of the noise map
    public float Scale; // Scale of the noise
    public int Seed; // Seed for reproducible noise

    public void Execute(int index)
    {
        // Calculate UV coordinates from the index
        int x = index % Resolution;
        int y = index / Resolution;

        float sampleX = (x + Seed/1000f) / Scale;
        float sampleY = (y + Seed/1000f) / Scale;
        
        float sampleX2 = (x + Seed/1000f) / (Scale * 3f);
        float sampleY2 = (y + Seed/1000f) / (Scale * 3f);

        // Generate noise using Unity.Mathematics functions (e.g., Perlin noise)
        float noiseValue = noise.snoise(new float2(sampleX, sampleY));
        float noiseValue2 = noise.snoise(new float2(sampleX2, sampleY2));

        // Map the noise value to a desired range (e.g., 0-1)
        NoiseMap[index] = (0.5f*(noiseValue + noiseValue2) + 1f) / 2f; 
    }
}

public struct GenerateHolesJob : IJobParallelFor
{
    public NativeArray<float> NoiseMap; // Output array for noise values
    public int Resolution; // Resolution of the noise map
    public float Scale; // Scale of the noise
    public int Seed; // Seed for reproducible noise

    public void Execute(int index)
    {
        // Calculate UV coordinates from the index
        int x = index % Resolution;
        int y = index / Resolution;

        float sampleX = (x + Seed/1000f) / Scale;
        float sampleY = (y + Seed/1000f) / Scale;

        // Generate noise using Unity.Mathematics functions (e.g., Perlin noise)
        float noiseValue = noise.cellular(new float2(sampleX, sampleY)).x;

        // Map the noise value to a desired range (e.g., 0-1)
        NoiseMap[index] = (noiseValue - 1f); 
    }
}
