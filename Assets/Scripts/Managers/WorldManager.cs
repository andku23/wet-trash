using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = UnityEngine.Random;

public class WorldManager : NetworkBehaviour
{
    public static WorldManager Instance;
    [SerializeField] private GameObject[] caveRoomEnds;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private GameObject[] lootGroupPrefabs;
    public int NUM_OF_HOLES;
    public int NUM_OF_LOOT_GROUPS;
    public int NUM_BIOMES = 4;

    public List<GameObject> SpawnedHoles;
    public List<GameObject> SpawnedLootGroups;
    
    public List<NetworkObject> InitialEnemies; // Enemies spawned at start
    public List<NetworkObject> SpawnedEnemies; // Enemis that spawn as the day goes on

    private Vector2[] currentHolePositions;
    private Vector2[] currentLootGroupPositions;

    private ClientTerrainGenerationData currentGenerationData;
    
    public int NumSpawnedMonsters => SpawnedEnemies.Count;


    [SerializeField] private Terrain _terrain;

    //private List<NativeArray<float>> noiseMaps = new List<NativeArray<float>>();
    private Dictionary<NoiseMapType, NativeArray<float>> noiseMaps = new Dictionary<NoiseMapType, NativeArray<float>>();
    private NativeArray<float> biomeMap;
    //List<NativeArray<float>> noiseMaps = new List<NativeArray<float>>();
    private List<JobHandle> concurrentJobs = new List<JobHandle>();
    private bool terrainGenerationRequested = false;
    private bool terrainGenerationCompleted = false;

    public enum NoiseMapType
    {
        TerrainHeightMap,
        SandyBottom,
        Cliffs,
        Peaks
    }

    private void Start()
    {
        Instance = this;
    }
    
    public void AssignTerrain(Terrain terrain)
    {
        _terrain = terrain;
    }
    
    public ClientTerrainGenerationData GenerateClientTerrainData_S()
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
        
        ClientTerrainGenerationData data = new ClientTerrainGenerationData();
        data.seed = seed;
        data.holePositions = holePosition;
        data.lootGroupPositions = lootGroupPosition;

        return data;
    }

    [ClientRpc]
    public void AssignGenerationData_ClientRpc(ClientTerrainGenerationData generationData)
    {
        currentGenerationData = generationData;
    }
    
    
    
    #region Get Terrain Info
    public Vector3 GetRandomPointOnTerrain()
    {
        Vector3 terrainSize = _terrain.terrainData.size;
        Vector3 terrainPosition = _terrain.transform.position;

        float randomX = Random.Range(terrainPosition.x, terrainPosition.x + terrainSize.x);
        float randomZ = Random.Range(terrainPosition.z, terrainPosition.z + terrainSize.z);

        return GetPointOnTerrain(randomX, randomZ);
    }
    
    public Vector3 GetRandomPointInOcean()
    {
        Vector3 randomPointOnTerrain = GetRandomPointOnTerrain();
        randomPointOnTerrain.y = Random.Range(0, randomPointOnTerrain.y);

        return randomPointOnTerrain;
    }
    
    public Vector3 GetPointOnTerrain(float x, float z)
    {
        Vector3 terrainPosition = _terrain.transform.position;

        Vector3 pointOnXZPlane = new Vector3(x, 0, z);
        float height = _terrain.SampleHeight(pointOnXZPlane);

        return new Vector3(x, height + terrainPosition.y, z);
    }
    
    #endregion

    #region Create Terrain Entities
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

    private LootGroup CreateHole(int xPos, int zPos, int holeWidth, int holeHeight)
    {
        bool[,] holeMap = new bool[holeWidth, holeHeight];
        float[,] heightMap = new float[holeWidth, holeHeight];
        Vector2 originOfCircle = new Vector2(holeWidth / 2, holeHeight / 2);
        int xbase = xPos - holeWidth / 2;
        int zbase = zPos - holeHeight / 2;
        
        float[,] heights = _terrain.terrainData.GetHeights(xbase, zbase,  holeWidth, holeHeight);
        
        Vector3 lowestHolePosition = new Vector3(xPos, 0, zPos);
        lowestHolePosition.y = heights[holeWidth/2, holeHeight/2] - 0.05f;
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
        
        for (int enemyIndex = 0; enemyIndex < InitialEnemies.Count; enemyIndex++)
        {
            InitialEnemies[enemyIndex].Despawn(true);
        }
        InitialEnemies.Clear();

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

    public void SpawnRandomEnemy_S()
    {
        Vector3 randomPointInOcean = GetRandomPointInOcean();
        var randomEnemy = enemyPrefabs[Random.Range(1, enemyPrefabs.Length)];
        NetworkObject no = Instantiate(randomEnemy,
            randomPointInOcean,
            Quaternion.identity
        ).GetComponent<NetworkObject>();
        no.Spawn();
        SpawnedEnemies.Add(no);
    }
    
    #endregion

    private void Update()
    {
        if (!terrainGenerationRequested) return;
        bool allJobsCompleted = true;
        foreach (JobHandle handle in concurrentJobs)
        {
            if (!handle.IsCompleted)
            {
                allJobsCompleted = false;
                break;
            }
        }
        if (allJobsCompleted)
        {
            // Make sure all jobs are completed
            foreach (JobHandle handle in concurrentJobs)
            {
                handle.Complete();
            }
           
            terrainGenerationRequested = false;
            CreateTerrain();
        }
    }
    
    public async Awaitable GenerateTerrain()
    {
        if (currentGenerationData == null) return;
        int seed = currentGenerationData.seed;
        Vector2[] holePositions = currentGenerationData.holePositions; 
        Vector2[] lootGroupPositions = currentGenerationData.lootGroupPositions;
        
        int resolution = _terrain.terrainData.heightmapResolution; 
        currentHolePositions = holePositions;
        currentLootGroupPositions = lootGroupPositions;

        noiseMaps.Clear();

        int mapSize = resolution * resolution;
        noiseMaps.Add(NoiseMapType.TerrainHeightMap, new NativeArray<float>(mapSize, Allocator.Persistent));
        noiseMaps.Add(NoiseMapType.SandyBottom, new NativeArray<float>(mapSize, Allocator.Persistent));
        noiseMaps.Add(NoiseMapType.Cliffs, new NativeArray<float>(mapSize, Allocator.Persistent));
        noiseMaps.Add(NoiseMapType.Peaks, new NativeArray<float>(mapSize, Allocator.Persistent));

        biomeMap = new NativeArray<float>(mapSize*NUM_BIOMES, Allocator.Persistent);
        
        LayeredPerlin2 terrainHeightJob = new LayeredPerlin2
        {
            NoiseMap = noiseMaps[NoiseMapType.TerrainHeightMap],
            Resolution = resolution,
            Scale1 = 500f,
            Scale2 = 200f,
            Noise1Strength = 0.7f,
            Noise2Strength = 0.3f,
            Seed = seed
        };
        
        LayeredPerlin2 noiseJob = new LayeredPerlin2
        {
            NoiseMap = noiseMaps[NoiseMapType.Cliffs],
            Resolution = resolution,
            Scale1 = 25f,
            Scale2 = 12f,
            Noise1Strength = 0.7f,
            Noise2Strength = 0.3f,
            Seed = seed
        };
        
        GenerateVoronoiJob voronoiJob = new GenerateVoronoiJob
        {
            NoiseMap = noiseMaps[NoiseMapType.Peaks],
            Resolution = resolution,
            Scale = 40f,
            Seed = seed
        };
        
        LayeredPerlin2 sandBottomJob = new LayeredPerlin2
        {
            NoiseMap = noiseMaps[NoiseMapType.SandyBottom],
            Resolution = resolution,
            Scale1 = 80f,
            Scale2 = 60f,
            Noise1Strength = 0.5f,
            Noise2Strength = 0.5f,
            Seed = seed
        };
        
        GenerateBiomes biomeJob = new GenerateBiomes
        {
            NoiseMap = biomeMap,
            Resolution = resolution,
            Scale = 80f,
            Seed = seed,
            NumBiomes = NUM_BIOMES
        };
        
        concurrentJobs.Clear();
        concurrentJobs.Add(terrainHeightJob.Schedule(noiseMaps[NoiseMapType.TerrainHeightMap].Length, 64));
        concurrentJobs.Add(noiseJob.Schedule(noiseMaps[NoiseMapType.Cliffs].Length, 64));
        concurrentJobs.Add(voronoiJob.Schedule(noiseMaps[NoiseMapType.Peaks].Length, 64));
        concurrentJobs.Add(sandBottomJob.Schedule(noiseMaps[NoiseMapType.SandyBottom].Length, 64));
        concurrentJobs.Add(biomeJob.Schedule(biomeMap.Length/NUM_BIOMES, 64));
        terrainGenerationRequested = true;
        terrainGenerationCompleted = false;
        
        while (!terrainGenerationCompleted)
        {
            await Awaitable.NextFrameAsync();
        }
    }
    
    // Sandy Bottom
    private float CreateInitialHeight(int x, int z, int nativeArrayIndex)
    {
        float height = noiseMaps[NoiseMapType.TerrainHeightMap][nativeArrayIndex];
        return height;
    }

    // Sandy Bottom
    private float CreateTerrainType0(int x, int z, int nativeArrayIndex)
    {
        float height = 0.1f * noiseMaps[NoiseMapType.SandyBottom][nativeArrayIndex];
        return height;
    }
    
    // Cliffs
    private float CreateTerrainType1(int x, int z, int nativeArrayIndex)
    {
        float height = 0.1f * noiseMaps[NoiseMapType.Cliffs][nativeArrayIndex];
        return height;
    }
    
    // Peaks
    private float CreateTerrainType2(int x, int z, int nativeArrayIndex)
    {
        float height = 0.4f * noiseMaps[NoiseMapType.Peaks][nativeArrayIndex];
        return height;
    }
    
    // High Sand
    private float CreateTerrainType3(int x, int z, int nativeArrayIndex)
    {
        float height =  0.15f * noiseMaps[NoiseMapType.SandyBottom][nativeArrayIndex];
        return height;
    }

    private void CreateTerrain()
    {
        float[,] heights = new float[
                _terrain.terrainData.heightmapResolution,
                _terrain.terrainData.heightmapResolution];

            ClearAllHoles();
            
            // Calculate terrain heights
            for (int i = 0; i < _terrain.terrainData.heightmapResolution; i++)
            {
                for (int j = 0; j < _terrain.terrainData.heightmapResolution; j++)
                {
                    float xRatio = i / (float)_terrain.terrainData.heightmapResolution;
                    float zRatio = j / (float)_terrain.terrainData.heightmapResolution;
                    float xInitialPosition = 1f - 2f * Mathf.Abs(xRatio - 0.5f);
                    float zInitialPosition = 1f - 2f * Mathf.Abs(zRatio - 0.5f);
                    
                    float height = 0f;
                    int nativeArrayIndex = i * _terrain.terrainData.heightmapResolution + j;
                    int biomeAccessOffset = _terrain.terrainData.heightmapResolution *
                                            _terrain.terrainData.heightmapResolution;
                    float[] biomeValues = new float[NUM_BIOMES];

                    for (int biomeIndex = 0; biomeIndex < biomeValues.Length; biomeIndex++)
                    {
                        biomeValues[biomeIndex] = biomeMap[nativeArrayIndex + biomeAccessOffset * biomeIndex];
                    }
                    
                    float th0 = CreateTerrainType0(i,j, nativeArrayIndex);
                    float th1 = CreateTerrainType1(i,j, nativeArrayIndex);
                    float th2 = CreateTerrainType2(i,j, nativeArrayIndex);
                    float th3 = CreateTerrainType3(i,j, nativeArrayIndex);

                    height = CreateInitialHeight(i, j, nativeArrayIndex);
                    if (height >= 0.8f)
                    {
                        height += CreateTerrainType0(i, j, nativeArrayIndex);
                    } else if (height <= 0.2f)
                    {
                        height += CreateTerrainType2(i, j, nativeArrayIndex);
                    }
                    else
                    {
                        height += biomeValues[0] * th0;
                        height += biomeValues[1] * th1;
                        height += biomeValues[2] * th2;
                    }
                    
                    //heights[i, j] = height;
                    heights[i, j] = height;
                }
            }
            _terrain.terrainData.SetHeights(0, 0, heights);
            
            // Paint Terrain
            float[,,] splatmapData = _terrain.terrainData.GetAlphamaps(0, 0, _terrain.terrainData.alphamapWidth, _terrain.terrainData.alphamapHeight);
            float xLength = splatmapData.GetLength(0);
            float zLength = splatmapData.GetLength(1);
            for (int i = 0; i < splatmapData.GetLength(0); i++)
            {
                for (int j = 0; j < splatmapData.GetLength(1); j++)
                {
                    Vector3 interpolatedNormal = _terrain.terrainData.GetInterpolatedNormal(j/zLength, i/xLength);
                    float facingUpAmount = Vector3.Dot(interpolatedNormal, Vector3.up);
                    facingUpAmount = Math.Clamp(facingUpAmount, 0, 1);
                    int nativeArrayIndex = i * _terrain.terrainData.heightmapResolution + j;
                    
                    splatmapData[i, j, 0] = 0;
                    splatmapData[i, j, 1] = 0;
                    splatmapData[i, j, 2] = 0;
                    splatmapData[i, j, 3] = 0;

                    if (facingUpAmount >= 0.65)
                    {
                        splatmapData[i, j, 0] = 1;
                    }
                    else
                    {
                        splatmapData[i, j, 2] = 1;
                    }

                    //float biomeResult = biomeMap[nativeArrayIndex];
                    //switch (biomeResult)
                    //{
                    //    case 0f:
                    //        splatmapData[i, j, 0] = 1;
                    //        break;
                    //    case 1f:
                    //        splatmapData[i, j, 1] = 1;
                    //        break;
                    //    case 2f:
                    //        splatmapData[i, j, 2] = 1;
                    //        break;
                    //    case 3f:
                    //        if (facingUpAmount >= 0.6f)
                    //        {
                    //            splatmapData[i, j, 0] = 1;
                    //        }
                    //        else
                    //        {
                    //            splatmapData[i, j, 2] = 1;
                    //        }
                    //        break;
                    //}
                }
            }
            _terrain.terrainData.SetAlphamaps(0, 0, splatmapData);
            
            // Spawn in loot groups, enemies and terrain features
            SpawnedHoles = new List<GameObject>();
            SpawnedEnemies = new List<NetworkObject>();
            InitialEnemies = new List<NetworkObject>();
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
                    InitialEnemies.Add(no);
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

            terrainGenerationCompleted = true;

            foreach (var noiseMap in noiseMaps)
            {
                noiseMap.Value.Dispose();
            }

            biomeMap.Dispose();
    }
}


public class ClientTerrainGenerationData: INetworkSerializable
{
    public int seed;
    public Vector2[] holePositions;
    public Vector2[] lootGroupPositions;
    
    // INetworkSerializable
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref seed);
        serializer.SerializeValue(ref holePositions);
        serializer.SerializeValue(ref lootGroupPositions);
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
        NoiseMap[index] = ((0.1f*noiseValue + 0.9f*noiseValue2) + 1f) / 2f; 
    }
}

public struct GenerateVoronoiJob : IJobParallelFor
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
        noiseValue = math.unlerp(-1, 1, noiseValue);

        // Map the noise value to a desired range (e.g., 0-1)
        NoiseMap[index] = 1f- noiseValue;
    }
}

public struct LayeredPerlin2 : IJobParallelFor
{
    public NativeArray<float> NoiseMap; // Output array for noise values
    public int Resolution; // Resolution of the noise map
    public float Scale1;
    public float Scale2; // Scale of the noise
    public float Noise1Strength;
    public float Noise2Strength;
    public int Seed; // Seed for reproducible noise

    public void Execute(int index)
    {
        // Calculate UV coordinates from the index
        int x = index % Resolution;
        int y = index / Resolution;

        float sampleX = (x + Seed) / Scale1;
        float sampleY = (y + Seed) / Scale1;
        float sampleX2 = (x + Seed + Seed) / Scale2;
        float sampleY2 = (y + Seed + Seed) / Scale2;

        // Generate noise using Unity.Mathematics functions (e.g., Perlin noise)
        float noiseValue = noise.snoise(new float2(sampleX, sampleY));
        float noiseValue2 = noise.snoise(new float2(sampleX2, sampleY2));
        
        noiseValue = math.unlerp(-1, 1, noiseValue);
        noiseValue2 = math.unlerp(-1, 1, noiseValue2);

        // Map the noise value to a desired range (e.g., 0-1)
        NoiseMap[index] = Noise1Strength * noiseValue + Noise2Strength * noiseValue2; 
    }
}


public struct GenerateBiomes : IJobParallelFor
{
    [NativeDisableParallelForRestriction]
    public NativeArray<float> NoiseMap; // Output array for noise values
    public int Resolution; // Resolution of the noise map
    public int Seed; // Seed for reproducible noise
    public float Scale;
    public int NumBiomes;
    
    public void Execute(int index)
    {
        // Calculate UV coordinates from the index
        int x = index % Resolution;
        int y = index / Resolution;
        //int biomeNum = index / (Resolution * Resolution);

        //float biomeOffset = biomeNum + 0.5f;
        //float sampleX1= (x + biomeOffset*Seed ) / Scale;
        //float sampleY1= (y + biomeOffset*Seed) / Scale;

        float[] biomeResults = new float[NumBiomes];
        float normalizationSum = 0;
        for (int i = 0; i < biomeResults.Length; i++)
        {
            float biomeOffset = i + 0.5f;
            float sampleX1= (x + biomeOffset*Seed ) / Scale;
            float sampleY1= (y + biomeOffset*Seed) / Scale;
            float noiseValue1 = noise.cnoise(new float2(sampleX1, sampleY1));
            noiseValue1 = math.unlerp(-1, 1, noiseValue1);
            biomeResults[i] = noiseValue1;
            normalizationSum += noiseValue1;
        }

        int greatestIndex = 0;
        for (int i = 0; i < biomeResults.Length; i++)
        {
            if (biomeResults[i] > biomeResults[greatestIndex])
            {
                greatestIndex = i;
            }
        }

        normalizationSum += 0.2f;
        biomeResults[greatestIndex] += 0.2f;
        
        for (int i = 0; i < biomeResults.Length; i++)
        {
            int biomeOffset = i * Resolution * Resolution;
            NoiseMap[index + biomeOffset] =  biomeResults[i]/normalizationSum;
        }
    }
}
