using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Mathematics.Geometry;
using Random = UnityEngine.Random;

public class TerrainManager : NetworkBehaviour
{
    public static TerrainManager Instance;

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
    
    public void GenerateTerrain()
    {
        GenerateTerrain_ServerRpc();
    }
    
    [ServerRpc]
    private void GenerateTerrain_ServerRpc()
    {
        int seed = Random.Range(0, 999999);
        GenerateTerrain_ClientRpc(seed);
    }
    
    [ClientRpc]
    private void GenerateTerrain_ClientRpc(int seed)
    {
        int resolution = _terrain.terrainData.heightmapResolution; 
        float scale = 5f;
        
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

        Vector3 pointOnXZPlane = new Vector3(randomX, 0, randomZ);
        float height = _terrain.SampleHeight(pointOnXZPlane);

        return new Vector3(randomX, height + terrainPosition.y, randomZ);
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
                                   //Starting height
                                   //+ 0.15f*(noiseMap[i*_terrain.terrainData.heightmapResolution + j]// noise
                                   //0.55f*holesMap[i*_terrain.terrainData.heightmapResolution + j]);
                    heights[i, j] = height;
                }
            }
        
            _terrain.terrainData.SetHeights(0, 0, heights); 

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
