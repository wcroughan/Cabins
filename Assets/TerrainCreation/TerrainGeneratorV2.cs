using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;

public class TerrainGeneratorV2 : MonoBehaviour
{
    [SerializeField]
    int seed;
    public static int randomSeed;
    public const int randomOffsetRange = 100000;
    public const int LOD_MAX = 4;

    Queue<TerrainCallbackInfo<TerrainChunkData>> newTerrainChunkCallbackQueue = new Queue<TerrainCallbackInfo<TerrainChunkData>>();
    Queue<TerrainCallbackInfo<TerrainSectionMeshData>> terrainSectionMeshCallbackQueue = new Queue<TerrainCallbackInfo<TerrainSectionMeshData>>();
    Queue<TerrainCallbackInfo<TerrainSectionMeshBakeData>> terrainSectionMeshBakeCallbackQueue = new Queue<TerrainCallbackInfo<TerrainSectionMeshBakeData>>();


    void Start()
    {
        randomSeed = seed;
    }

    private TerrainChunkData GenerateNewTerrainChunkData(Vector2 chunkCoord)
    {
        return new TerrainChunkData();
    }


    private TerrainSectionMeshData GenerateTerrainSectionMeshData(TerrainChunkData terrainChunkData, int levelOfDetail)
    {
        return new TerrainSectionMeshData();
    }

    public void RequestNewChunkData(Action<TerrainChunkData> callback, Vector2 chunkCoord)
    {
        Task.Run(() =>
        {
            NewChunkRequestThread(callback, chunkCoord);
        });
    }

    private void NewChunkRequestThread(Action<TerrainChunkData> callback, Vector2 chunkCoord)
    {
        TerrainChunkData terrainChunkData = GenerateNewTerrainChunkData(chunkCoord);

        lock (newTerrainChunkCallbackQueue)
        {
            newTerrainChunkCallbackQueue.Enqueue(new TerrainCallbackInfo<TerrainChunkData>(callback, terrainChunkData));
        }
    }


    public void RequestSectionMesh(Action<TerrainSectionMeshData> callback, TerrainChunkData terrainChunkData, int levelOfDetail)
    {

        Task.Run(() =>
        {
            SectionMeshRequestThread(callback, terrainChunkData, levelOfDetail);
        });
    }

    private void SectionMeshRequestThread(Action<TerrainSectionMeshData> callback, TerrainChunkData terrainChunkData, int levelOfDetail)
    {
        TerrainSectionMeshData terrainSectionMeshData = GenerateTerrainSectionMeshData(terrainChunkData, levelOfDetail);

        lock (terrainSectionMeshCallbackQueue)
        {
            terrainSectionMeshCallbackQueue.Enqueue(new TerrainCallbackInfo<TerrainSectionMeshData>(callback, terrainSectionMeshData));
        }
    }

    public void RequestSectionMeshBake(Action<TerrainSectionMeshBakeData> callback, int meshID)
    {
        Task.Run(() =>
        {
            SectionMeshBakeThread(callback, meshID);
        });
    }

    private void SectionMeshBakeThread(Action<TerrainSectionMeshBakeData> callback, int meshID)
    {
        Physics.BakeMesh(meshID, false);

        lock (terrainSectionMeshBakeCallbackQueue)
        {
            terrainSectionMeshBakeCallbackQueue.Enqueue(new TerrainCallbackInfo<TerrainSectionMeshBakeData>(callback, new TerrainSectionMeshBakeData()));
        }
    }

    void Update()
    {
        for (int i = 0; i < newTerrainChunkCallbackQueue.Count; i++)
        {
            TerrainCallbackInfo<TerrainChunkData> item = newTerrainChunkCallbackQueue.Dequeue();
            item.callback(item.parameter);
        }
        for (int i = 0; i < terrainSectionMeshCallbackQueue.Count; i++)
        {
            TerrainCallbackInfo<TerrainSectionMeshData> item = terrainSectionMeshCallbackQueue.Dequeue();
            item.callback(item.parameter);
        }
        for (int i = 0; i < terrainSectionMeshBakeCallbackQueue.Count; i++)
        {
            TerrainCallbackInfo<TerrainSectionMeshBakeData> item = terrainSectionMeshBakeCallbackQueue.Dequeue();
            item.callback(item.parameter);
        }
    }

    struct TerrainCallbackInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public TerrainCallbackInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

public struct TerrainChunkData
{
    public float[,] heightMap;
}

public struct TerrainSectionMeshData
{
    public int LOD;
    public Mesh CreateMesh()
    {
        return new Mesh();
    }
}

public struct TerrainSectionMeshBakeData
{

}