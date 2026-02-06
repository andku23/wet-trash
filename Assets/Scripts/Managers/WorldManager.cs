using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class WorldManager : NetworkBehaviour
{
    public static WorldManager Instance;
    [SerializeField] private GameObject[] caveRoomEnds;
    
    [SerializeField] private BaseEnemy[] enemyOverworldPrefabs;
    [SerializeField] private BaseEnemy[] enemyDungeonPrefabs;
    [SerializeField] private GameObject[] lootGroupPrefabs;
    [SerializeField] private GameObject[] harvestablePrefabs;
    public int NUM_OF_HOLES;
    public int NUM_BIOMES = 4;

    public List<GameObject> SpawnedHoles;
    public List<GameObject> SpawnedLootGroups;
    
    [HideInInspector] public List<NetworkObject> InitialEnemies; // Enemies spawned at start 
    [HideInInspector] public List<BaseEnemy> SpawnedEnemies_S; // Enemis that spawn as the day goes on 
    [HideInInspector] public List<NetworkObject> SpawnedHarvestables;  

    private Vector2[] currentHolePositions_c;
    private Vector2[] currentLootGroupPositions_c;
    private float[] currentLootGroupRotations_c;

    private ClientTerrainGenerationData terrainGenerationData;
    private ClientStructureGenerationData structureGenerationData;
    
    public int NumSpawnedMonsters => SpawnedEnemies_S.Count;


    [SerializeField] private Terrain _terrain;

    private Dictionary<NoiseMapType, NativeArray<float>> noiseMaps = new Dictionary<NoiseMapType, NativeArray<float>>();
    private NativeArray<float> biomeMap;
    private List<JobHandle> concurrentJobs = new List<JobHandle>();
    private int[,] terrainBiomes = null;
    
    private bool terrainGenerationRequested = false;
    private bool terrainGenerationCompleted = false;

    public enum NoiseMapType
    {
        TerrainHeightMap,
        SandyBottom,
        Cliffs,
        Peaks
    }

    public enum BiomeType
    {
        NearShore,
        Biome1,
        Biome2,
        Biome3,
        Biome4,
        Depths
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
        
        
        ClientTerrainGenerationData data = new ClientTerrainGenerationData();
        data.seed = seed;
        //data.holePositions = holePosition;
        //data.lootGroupPositions = lootGroupPosition;

        return data;
    }
    
    public ClientTerrainGenerationData GenerateLootHotspotPositions_S()
    {
        int seed = Random.Range(0, 999999);
        
        
        ClientTerrainGenerationData data = new ClientTerrainGenerationData();
        data.seed = seed;
        //data.holePositions = holePosition;
        //data.lootGroupPositions = lootGroupPosition;

        return data;
    }

    public ClientStructureGenerationData GenerateClientStructureData_S(int numDoorLocations)
    {
        NUM_OF_HOLES = numDoorLocations;
        Vector2[] holePositions = new Vector2[NUM_OF_HOLES];
        Vector2[] lootGroupPositions = new Vector2[GameManager.Instance.gameData.NUM_OF_LOOT_GROUPS_PER_HOTSPOT * NUM_OF_HOLES];
        float[] lootGroupRotations = new float[GameManager.Instance.gameData.NUM_OF_LOOT_GROUPS_PER_HOTSPOT * NUM_OF_HOLES];
        float lootGroupDistributionRange = 32f;
        
        
        //Holes and hotspots are the same currently
        // it will just spawn them all around
        

        for (int i = 0; i < NUM_OF_HOLES; i++)
        {
            holePositions[i] = new Vector2(
                Random.Range(180, _terrain.terrainData.heightmapResolution - 180),
                Random.Range(180, _terrain.terrainData.heightmapResolution - 180));
            
            for (int j = 0; j < GameManager.Instance.gameData.NUM_OF_LOOT_GROUPS_PER_HOTSPOT; j++)
            {
                
                lootGroupPositions[(i*NUM_OF_HOLES)+j] = new Vector2(
                    Math.Clamp(holePositions[i].x + Random.Range(-lootGroupDistributionRange, lootGroupDistributionRange), 0, _terrain.terrainData.heightmapResolution),
                    Math.Clamp(holePositions[i].y + Random.Range(-lootGroupDistributionRange, lootGroupDistributionRange), 0, _terrain.terrainData.heightmapResolution));

                lootGroupRotations[(i * NUM_OF_HOLES) + j] = Random.Range(0, 360);
            }
        }
        
        
        ClientStructureGenerationData data = new ClientStructureGenerationData();
        data.holePositions = holePositions;
        data.lootGroupPositions = lootGroupPositions;
        data.lootGroupRotations = lootGroupRotations;
        return data;
    }

    [ClientRpc]
    public void AssignTerrainGenerationData_ClientRpc(ClientTerrainGenerationData generationData)
    {
        terrainGenerationData = generationData;
    }
    
    [ClientRpc]
    public void AssignStructureGenerationData_ClientRpc(ClientStructureGenerationData generationData)
    {
        structureGenerationData = generationData;
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
    
    public Vector3 GetRandomPointOnTerrainNearHotspot()
    {
        Vector3 terrainPosition = _terrain.transform.position;
        int hotspotIndex = Mathf.FloorToInt(Random.Range(0f,currentHolePositions_c.Length));
        Vector3 randomPointOnTerrain = GetPointOnTerrain(
            terrainPosition.x + currentHolePositions_c[hotspotIndex].x + Random.Range(0f,20), 
            terrainPosition.z + currentHolePositions_c[hotspotIndex].y+ Random.Range(0f,20));

        return randomPointOnTerrain;
    }

    // Uses terrain resolution as input
    public Vector3 GetPointOnTerrainFromResolution(int x, int z)
    {
        Vector3 terrainPosition = _terrain.transform.position;
        float calculatedX = terrainPosition.x + x;
        float calculatedZ = terrainPosition.z + z;
        return GetPointOnTerrain(calculatedX, calculatedZ);
    }
    
    public Vector3 GetRandomPointInOcean()
    {
        Vector3 randomPointOnTerrain = GetRandomPointOnTerrain();
        randomPointOnTerrain.y = Random.Range(0, randomPointOnTerrain.y);

        return randomPointOnTerrain;
    }
    
    public Vector3 GetRandomPointNearHotspot()
    {
        Vector3 randomPointOnTerrain = GetRandomPointOnTerrainNearHotspot();
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

    public BiomeType GetBiomeType(int x, int z)
    {
        return (BiomeType) terrainBiomes[x, z];
    }

    public List<Vector2> GetLootSpawnPositions()
    {
        int resolution = _terrain.terrainData.heightmapResolution;
        List<Vector2> lootSpawnPositions = new List<Vector2>();
        for (int i = 0; i < resolution; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                BiomeType biomeType = (BiomeType) terrainBiomes[i, j];
                int probabilityThreshold  = LootManager.Instance.BiomeLootSpawnTable[biomeType].probability;
                int probability = Random.Range(0, GameManager.Instance.gameData.LOOT_PROBABILITY_DIVISOR);
                if (probability < probabilityThreshold)
                {
                    lootSpawnPositions.Add(new Vector2(i, j));
                }
            }
        }

        return lootSpawnPositions;
    }
    
    #endregion

    #region Create Terrain Entities
    private LootGroup CreateLootGroup(int xPos, int zPos, float yRot)
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
        lootGroup.transform.Rotate(Vector3.up, yRot);
        
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
        float fullSize = (float)holeWidth / 2;
        float holeSize = (float)holeWidth / 4;
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

    private void ClearAllTerrainStructures()
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
        
        for (int enemyIndex = 0; enemyIndex < SpawnedEnemies_S.Count; enemyIndex++)
        {
            SpawnedEnemies_S[enemyIndex].NetworkObject.Despawn(true);
        }
        SpawnedEnemies_S.Clear();
        
        for (int enemyIndex = 0; enemyIndex < InitialEnemies.Count; enemyIndex++)
        {
            InitialEnemies[enemyIndex].Despawn(true);
        }
        InitialEnemies.Clear();
        
        for (int index = 0; index < SpawnedHarvestables.Count; index++)
        {
            SpawnedHarvestables[index].Despawn(true);
        }
        SpawnedHarvestables.Clear();

        for (int i = 0; i < _terrain.terrainData.heightmapResolution - 1; i++)
        {
            for (int j = 0; j < _terrain.terrainData.heightmapResolution - 1; j++)
            {
                clearHoles[i, j] = true;
            }
        }
        _terrain.terrainData.SetHoles(0, 0, clearHoles);
        LootManager.Instance.ResetLootHoles_S();
    }

    public void SpawnRandomEnemy_S()
    {
        Vector3 randomPointInOcean = GetRandomPointNearHotspot();
        var randomEnemy = enemyOverworldPrefabs[Random.Range(0, enemyOverworldPrefabs.Length)];
        BaseEnemy baseEnemy = Instantiate(randomEnemy,
            randomPointInOcean,
            Quaternion.identity
        );
        baseEnemy.SpawnArea_S = BaseEnemy.SpawnAreaType.Overworld;
        baseEnemy.NetworkObject.Spawn();
        SpawnedEnemies_S.Add(baseEnemy);
    }
    
    public void SpawnRandomDungeonEnemy_S(CaveRoom caveRoom)
    {
        Vector3 spawnPoint = caveRoom.transform.position;
        var randomEnemy = enemyDungeonPrefabs[Random.Range(0, enemyDungeonPrefabs.Length)];
        BaseEnemy baseEnemy = Instantiate(randomEnemy,
            spawnPoint,
            Quaternion.identity
        );
        baseEnemy.SpawnArea_S = BaseEnemy.SpawnAreaType.Dungeon;
        CaveEnemy caveEnemy = baseEnemy.GetComponent<CaveEnemy>();
        caveEnemy.CurrentRoom_S = caveRoom;
        baseEnemy.NetworkObject.Spawn();
        SpawnedEnemies_S.Add(baseEnemy);
    }

    public void SpawnRandomHarvestable_S()
    {
        Vector3 spawnLocation = GetRandomPointOnTerrainNearHotspot();
        var randomHarvestable = harvestablePrefabs[Random.Range(0, harvestablePrefabs.Length)];
        NetworkObject no = Instantiate(randomHarvestable,
            spawnLocation,
            Quaternion.identity
        ).GetComponent<NetworkObject>();
        no.Spawn();
        SpawnedHarvestables.Add(no);
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
            CreateTerrain_C();
        }
    }
    
    //Starts generation of noise maps with burst compiler
    public async Awaitable BeginTerrainGeneration_C()
    {
        if (terrainGenerationData == null) return;
        int seed = terrainGenerationData.seed;
        int resolution = _terrain.terrainData.heightmapResolution;

        if (terrainBiomes == null ||
            _terrain.terrainData.heightmapResolution != terrainBiomes.GetLength(0) ||
            _terrain.terrainData.heightmapResolution != terrainBiomes.GetLength(1))
        {
            terrainBiomes = new int[resolution, resolution];
        }

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
    
    //Starts generation of noise maps with burst compiler
    public async Awaitable BeginStructureGeneration_C()
    {
        if (structureGenerationData == null) return;
        currentHolePositions_c = structureGenerationData.holePositions; 
        currentLootGroupPositions_c = structureGenerationData.lootGroupPositions;
        currentLootGroupRotations_c = structureGenerationData.lootGroupRotations;
        CreateStructures_C();
        await Awaitable.NextFrameAsync(); //Just stops any race conditions
    }

    #region Biome Height Functions
    // Initial Height Map
    private float CreateInitialHeight(int x, int z, int nativeArrayIndex, int resolution)
    {
        float normalizedX = (float) x / resolution;
        float normalizedZ = (float) z / resolution;
        float heightShapeX = 1 - Math.Abs(2 * normalizedX - 1);
        float heightShapeZ = 1 - Math.Abs(2 * normalizedZ - 1);
        float distanceFromCenter = 0.5f*(Math.Abs(2 *normalizedX-1f) + Math.Abs(2 *normalizedZ-1f));
        float heightCenter = 0.4f * (heightShapeX + heightShapeZ);
        float heightDistance = noiseMaps[NoiseMapType.TerrainHeightMap][nativeArrayIndex];
        float height =  Mathf.Clamp(1 - 2*distanceFromCenter, 0, 1) * heightCenter + Mathf.Clamp(2*distanceFromCenter, 0, 1) * heightDistance;
            //noiseMaps[NoiseMapType.TerrainHeightMap][nativeArrayIndex];
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
    
    #endregion

    private void CreateTerrain_C()
    {
        int resolution = _terrain.terrainData.heightmapResolution;
        float[,] heights = new float[resolution, resolution];

        ClearAllTerrainStructures();

        int biomeAccessOffset = resolution * resolution;
        List<TreeInstance> treeInstances = new List<TreeInstance>();

        // Calculate terrain heights
        for (int i = 0; i < resolution; i++)
        {
            for (int j = 0; j < resolution; j++)
            {
                float xRatio = i / (float)resolution;
                float zRatio = j / (float)resolution;
                float xInitialPosition = 1f - 2f * Mathf.Abs(xRatio - 0.5f);
                float zInitialPosition = 1f - 2f * Mathf.Abs(zRatio - 0.5f);

                float height = 0f;
                int nativeArrayIndex = i * resolution + j;


                float[] biomeValues = new float[NUM_BIOMES];
                for (int biomeIndex = 0; biomeIndex < biomeValues.Length; biomeIndex++)
                {
                    biomeValues[biomeIndex] = biomeMap[nativeArrayIndex + biomeAccessOffset * biomeIndex];
                }

                float th0 = CreateTerrainType0(i, j, nativeArrayIndex);
                float th1 = CreateTerrainType1(i, j, nativeArrayIndex);
                float th2 = CreateTerrainType2(i, j, nativeArrayIndex);
                float th3 = CreateTerrainType3(i, j, nativeArrayIndex);

                height = CreateInitialHeight(i, j, nativeArrayIndex, resolution);
                if (height >= 0.8f)
                {
                    height += CreateTerrainType0(i, j, nativeArrayIndex);
                    terrainBiomes[i, j] = (int)BiomeType.NearShore;
                }
                else if (height <= 0.2f)
                {
                    height -= CreateTerrainType2(i, j, nativeArrayIndex);
                    terrainBiomes[i, j] = (int)BiomeType.Depths;
                }
                else
                {
                    int mostBiome = VarietyUtilities.GetGreatestIndexInArray(biomeValues);
                    height += biomeValues[0] * th0;
                    height += biomeValues[1] * th1;
                    height += biomeValues[2] * th2;

                    if (mostBiome == 0)
                    {
                        terrainBiomes[i, j] = (int)BiomeType.Biome1;
                    } else if (mostBiome == 1)
                    {
                        terrainBiomes[i, j] = (int)BiomeType.Biome2;
                    } else if (mostBiome == 2)
                    {
                        terrainBiomes[i, j] = (int)BiomeType.Biome3;
                    }
                    else
                    {
                        terrainBiomes[i, j] = (int)BiomeType.Biome4;
                    }
                    
                    float noise = noiseMaps[NoiseMapType.Peaks][nativeArrayIndex];
                    if (height < 0.7f && i % 4 == 0 && j % 4 == 0 && noise > 0.15)
                    {
                        TreeInstance newTreeInstance = new TreeInstance();
                        newTreeInstance.position = new Vector3((j + Random.Range(-3f, 3f)) / resolution, height,
                            (i + Random.Range(-3f, 3f)) / resolution); // Normalized coordinates (0-1)
                        newTreeInstance.rotation = 0f;
                        newTreeInstance.widthScale = (noise - 0.15f) * 4f;
                        newTreeInstance.heightScale = (noise - 0.15f) * 4f;
                        newTreeInstance.prototypeIndex = 0;
                        treeInstances.Add(newTreeInstance);
                    }
                }

                heights[i, j] = height;
            }
        }

        _terrain.terrainData.SetHeights(0, 0, heights);
        _terrain.terrainData.SetTreeInstances(treeInstances.ToArray(), true);

        // Paint Terrain
        float[,,] splatmapData = _terrain.terrainData.GetAlphamaps(0, 0, _terrain.terrainData.alphamapWidth,
            _terrain.terrainData.alphamapHeight);
        float xLength = splatmapData.GetLength(0);
        float zLength = splatmapData.GetLength(1);

        for (int i = 0; i < splatmapData.GetLength(0); i++)
        {
            for (int j = 0; j < splatmapData.GetLength(1); j++)
            {
                Vector3 interpolatedNormal = _terrain.terrainData.GetInterpolatedNormal(j / zLength, i / xLength);
                float height = heights[i, j];

                float facingUpAmount = Vector3.Dot(interpolatedNormal, Vector3.up);
                facingUpAmount = Math.Clamp(facingUpAmount, 0, 1);
                int nativeArrayIndex = i * _terrain.terrainData.heightmapResolution + j;

                float[] biomeValues = new float[NUM_BIOMES];
                for (int biomeIndex = 0; biomeIndex < biomeValues.Length; biomeIndex++)
                {
                    biomeValues[biomeIndex] = biomeMap[nativeArrayIndex + biomeAccessOffset * biomeIndex];
                }

                splatmapData[i, j, 0] = 0;
                splatmapData[i, j, 1] = 0;
                splatmapData[i, j, 2] = 0;
                splatmapData[i, j, 3] = 0;
                splatmapData[i, j, 4] = 0;
                splatmapData[i, j, 5] = 0;

                if (height > 0.7f)
                {
                    float blendAmount = Mathf.Min((height - 0.7f) / 0.1f, 1f);
                    float multiplier = Mathf.Pow(10f, 2);
                    blendAmount = Mathf.Floor(blendAmount * multiplier) / multiplier;

                    if (facingUpAmount > 0.75f)
                        splatmapData[i, j, 0] = blendAmount;
                    else
                        splatmapData[i, j, 5] = blendAmount;

                    float inverseBlend = 1.0f - blendAmount;

                    splatmapData[i, j, 1] = inverseBlend * biomeValues[0];
                    splatmapData[i, j, 2] = inverseBlend * biomeValues[1];
                    splatmapData[i, j, 3] = inverseBlend * biomeValues[2];
                    splatmapData[i, j, 4] = inverseBlend * biomeValues[3];
                }
                else if (height <= 0.2f)
                {
                    splatmapData[i, j, 0] = 1;
                }
                else
                {
                    if (facingUpAmount <= 0.5f)
                        splatmapData[i, j, 2] = 1;
                    else
                    {
                        splatmapData[i, j, 1] = biomeValues[0];
                        splatmapData[i, j, 2] = biomeValues[1];
                        splatmapData[i, j, 3] = biomeValues[2];
                        splatmapData[i, j, 4] = biomeValues[3];
                    }
                }
            }
        }

        _terrain.terrainData.SetAlphamaps(0, 0, splatmapData);

        terrainGenerationCompleted = true;
        foreach (var noiseMap in noiseMaps)
        {
            noiseMap.Value.Dispose();
        }

        biomeMap.Dispose();
    }

    private void CreateStructures_C()
    {
        // Spawn in loot groups, enemies and terrain features
        SpawnedHoles = new List<GameObject>();
        SpawnedEnemies_S = new List<BaseEnemy>();
        InitialEnemies = new List<NetworkObject>();
        SpawnedLootGroups = new List<GameObject>();
        
        for (int i = 0; i < currentHolePositions_c.Length; i++)
        {
            LootGroup hole = CreateHole((int)currentHolePositions_c[i].x,
                (int)currentHolePositions_c[i].y,
                32, 32);

            SpawnedHoles.Add(hole.gameObject);

            if (IsServer)
            {
                LootManager.Instance.RegisterLootGroupServer(hole);
            }
        }

        for (int i = 0; i < currentLootGroupPositions_c.Length; i++)
        {
            LootGroup lootGroup = CreateLootGroup(
                (int)currentLootGroupPositions_c[i].x,
                (int)currentLootGroupPositions_c[i].y,
                currentLootGroupRotations_c[i]);

            SpawnedLootGroups.Add(lootGroup.gameObject);

            if (IsServer)
            {
                LootManager.Instance.RegisterLootGroupServer(lootGroup);
            }
        }
    }
    
}


public class ClientTerrainGenerationData: INetworkSerializable
{
    public int seed;
    
    // INetworkSerializable
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref seed);
    }
}

public class ClientStructureGenerationData: INetworkSerializable
{
    public Vector2[] holePositions;
    public Vector2[] lootGroupPositions;
    public float[] lootGroupRotations;
    
    // INetworkSerializable
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref holePositions);
        serializer.SerializeValue(ref lootGroupPositions);
        serializer.SerializeValue(ref lootGroupRotations);
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

        // normalizationSum += 0.2f;
        // biomeResults[greatestIndex] += 0.2f;
        
        for (int i = 0; i < biomeResults.Length; i++)
        {
            int biomeOffset = i * Resolution * Resolution;
            NoiseMap[index + biomeOffset] =  biomeResults[i]/normalizationSum;
        }
    }
}
