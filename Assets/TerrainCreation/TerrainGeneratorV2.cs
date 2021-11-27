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
    [SerializeField]
    float biomeMapNoiseScale = 0.1f;
    [SerializeField]
    Biome[] biomes;
    public const int randomOffsetRange = 100000;
    public const int LOD_MAX = 4;
    public const int LOD_MIN = 0;

    Queue<TerrainCallbackInfo<TerrainChunkData>> newTerrainChunkCallbackQueue = new Queue<TerrainCallbackInfo<TerrainChunkData>>();
    Queue<TerrainCallbackInfo<TerrainSectionMeshData>> terrainSectionMeshCallbackQueue = new Queue<TerrainCallbackInfo<TerrainSectionMeshData>>();
    Queue<TerrainCallbackInfo<TerrainSectionMeshBakeData>> terrainSectionMeshBakeCallbackQueue = new Queue<TerrainCallbackInfo<TerrainSectionMeshBakeData>>();


    void OnValidate()
    {
        randomSeed = seed;
    }

    void Start()
    {
        randomSeed = seed;
    }

    private int GetBiomeFromSelectionValues(float v1, float v2)
    {
        return v1 > v2 ? 0 : 1;
    }

    private int[,] GenerateChunkBiomeMap(Vector2 chunkCenter, int dim)
    {
        System.Random rngesus = new System.Random(seed);
        float biomeNoiseOffsetX1 = (float)rngesus.NextDouble() * randomOffsetRange;
        float biomeNoiseOffsetY1 = (float)rngesus.NextDouble() * randomOffsetRange;
        float biomeNoiseOffsetX2 = (float)rngesus.NextDouble() * randomOffsetRange;
        float biomeNoiseOffsetY2 = (float)rngesus.NextDouble() * randomOffsetRange;

        float x01 = biomeMapNoiseScale * ((float)(dim - 1) / -2f + chunkCenter.x) + biomeNoiseOffsetX1;
        float y01 = biomeMapNoiseScale * ((float)(dim - 1) / -2f + chunkCenter.y) + biomeNoiseOffsetY1;
        float x02 = biomeMapNoiseScale * ((float)(dim - 1) / -2f + chunkCenter.x) + biomeNoiseOffsetX2;
        float y02 = biomeMapNoiseScale * ((float)(dim - 1) / -2f + chunkCenter.y) + biomeNoiseOffsetY2;

        int[,] ret = new int[dim, dim];

        for (int x = 0; x < dim; x++)
        {
            for (int y = 0; y < dim; y++)
            {
                float sampX1 = (float)x * biomeMapNoiseScale + x01;
                float sampY1 = (float)y * biomeMapNoiseScale + y01;
                float sampX2 = (float)x * biomeMapNoiseScale + x02;
                float sampY2 = (float)y * biomeMapNoiseScale + y02;

                float v1 = Mathf.PerlinNoise(sampX1, sampY1);
                float v2 = Mathf.PerlinNoise(sampX2, sampY2);

                ret[x, y] = GetBiomeFromSelectionValues(v1, v2);
            }

        }

        return ret;
    }

    private TerrainChunkData GenerateNewTerrainChunkData(Vector2 chunkCenter, int chunkSideLength)
    {
        TerrainChunkData ret = new TerrainChunkData();
        ret.chunkCenter = chunkCenter;
        ret.chunkSideLength = chunkSideLength;
        ret.numMarginPts = 5;

        int dim = chunkSideLength + 1 + 2 * ret.numMarginPts;

        int[,] chunkBiomeMap = GenerateChunkBiomeMap(chunkCenter, dim);

        int numBiomes = biomes.Length;
        float[,,] allHeightMaps = new float[dim, dim, numBiomes];
        Dictionary<int, int> heightMapIndexForBiomeIndex = new Dictionary<int, int>();
        int z = 0, nextz = int.MaxValue;
        int numBiomesFilled = 0;
        while (numBiomesFilled < numBiomes)
        {
            for (int i = 0; i < numBiomes; i++)
            {
                if (biomes[i].heightMapFillOrder == z)
                {
                    bool[,] mask = new bool[dim, dim];
                    for (int x = 0; x < dim; x++)
                    {
                        for (int y = 0; y < dim; y++)
                        {
                            if (chunkBiomeMap[x, y] == i)
                                mask[x, y] = true;
                            else
                                mask[x, y] = false;
                        }
                    }
                    biomes[i].PopulateHeightMap(allHeightMaps, mask, numBiomesFilled, chunkCenter);
                    heightMapIndexForBiomeIndex[i] = numBiomesFilled;
                    numBiomesFilled++;
                }
                else if (biomes[i].heightMapFillOrder > z && biomes[i].heightMapFillOrder < nextz)
                {
                    nextz = biomes[i].heightMapFillOrder;
                }
            }
            z = nextz;
        }

        ret.heightMap = new float[dim, dim];
        for (int x = 0; x < dim; x++)
        {
            for (int y = 0; y < dim; y++)
            {
                int ahmi = heightMapIndexForBiomeIndex[chunkBiomeMap[x, y]];
                ret.heightMap[x, y] = allHeightMaps[x, y, ahmi];
            }
        }

        return ret;
    }


    private TerrainSectionMeshData GenerateTerrainSectionMeshData(TerrainChunkData terrainChunkData, Vector2 sectionCoord, int sectionSize, int levelOfDetail)
    {
        TerrainSectionMeshData ret = new TerrainSectionMeshData();
        ret.LOD = levelOfDetail;
        int dim = sectionSize + 1;
        ret.vertices = new Vector3[dim * dim];
        ret.UVs = new Vector2[dim * dim];
        ret.triangles = new int[6 * sectionSize * sectionSize];

        int xi0 = terrainChunkData.numMarginPts + Mathf.RoundToInt(sectionCoord.x) * sectionSize;
        int yi0 = terrainChunkData.numMarginPts + Mathf.RoundToInt(sectionCoord.y) * sectionSize;
        float xoffset = terrainChunkData.chunkCenter.x - (float)terrainChunkData.chunkSideLength / 2f + sectionCoord.x * sectionSize;
        float yoffset = terrainChunkData.chunkCenter.y - (float)terrainChunkData.chunkSideLength / 2f + sectionCoord.y * sectionSize;

        int tridx = 0;
        for (int y = 0; y < dim; y++)
        {
            for (int x = 0; x < dim; x++)
            {
                int vi = x + y * dim;
                ret.vertices[vi] = new Vector3(xoffset + x, terrainChunkData.heightMap[xi0 + x, yi0 + y], yoffset + y);
                ret.UVs[vi] = new Vector2((float)x / (dim - 1f), (float)y / (dim - 1f));
                if (x > 0 && y > 0)
                {
                    ret.triangles[tridx++] = vi - dim - 1;
                    ret.triangles[tridx++] = vi;
                    ret.triangles[tridx++] = vi - dim;
                    ret.triangles[tridx++] = vi - dim - 1;
                    ret.triangles[tridx++] = vi - 1;
                    ret.triangles[tridx++] = vi;
                }
            }
        }

        return ret;
    }

    public void RequestNewChunkData(Action<TerrainChunkData> callback, Vector2 chunkCenter, int chunkSideLength)
    {
#if UNITY_EDITOR
        NewChunkRequestThread(callback, chunkCenter, chunkSideLength);
#else
        Task.Run(() =>
        {
            NewChunkRequestThread(callback, chunkCenter, chunkSideLength);
        });
#endif
    }

    private void NewChunkRequestThread(Action<TerrainChunkData> callback, Vector2 chunkCenter, int chunkSideLength)
    {
        TerrainChunkData terrainChunkData = GenerateNewTerrainChunkData(chunkCenter, chunkSideLength);

#if UNITY_EDITOR
        callback(terrainChunkData);
#else
        lock (newTerrainChunkCallbackQueue)
        {
            newTerrainChunkCallbackQueue.Enqueue(new TerrainCallbackInfo<TerrainChunkData>(callback, terrainChunkData));
        }
#endif
    }


    public void RequestSectionMesh(Action<TerrainSectionMeshData> callback, TerrainChunkData terrainChunkData, Vector2 sectionCoord, int sectionSize, int levelOfDetail)
    {
#if UNITY_EDITOR
        SectionMeshRequestThread(callback, terrainChunkData, sectionCoord, sectionSize, levelOfDetail);
#else
        Task.Run(() =>
        {
            SectionMeshRequestThread(callback, terrainChunkData, sectionCoord, sectionSize, levelOfDetail);
        });
#endif
    }

    private void SectionMeshRequestThread(Action<TerrainSectionMeshData> callback, TerrainChunkData terrainChunkData, Vector2 sectionCoord, int sectionSize, int levelOfDetail)
    {
        TerrainSectionMeshData terrainSectionMeshData = GenerateTerrainSectionMeshData(terrainChunkData, sectionCoord, sectionSize, levelOfDetail);

#if UNITY_EDITOR
        callback(terrainSectionMeshData);
#else
        lock (terrainSectionMeshCallbackQueue)
        {
            terrainSectionMeshCallbackQueue.Enqueue(new TerrainCallbackInfo<TerrainSectionMeshData>(callback, terrainSectionMeshData));
        }
#endif
    }

    public void RequestSectionMeshBake(Action<TerrainSectionMeshBakeData> callback, int meshID)
    {
#if UNITY_EDITOR
        SectionMeshBakeThread(callback, meshID);
#else
        Task.Run(() =>
        {
            SectionMeshBakeThread(callback, meshID);
        });
#endif
    }

    private void SectionMeshBakeThread(Action<TerrainSectionMeshBakeData> callback, int meshID)
    {
        Physics.BakeMesh(meshID, false);

#if UNITY_EDITOR
        callback(new TerrainSectionMeshBakeData());
#else
        lock (terrainSectionMeshBakeCallbackQueue)
        {
            terrainSectionMeshBakeCallbackQueue.Enqueue(new TerrainCallbackInfo<TerrainSectionMeshBakeData>(callback, new TerrainSectionMeshBakeData()));
        }
#endif
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
    public int chunkSideLength;
    public int numMarginPts;
    public Vector2 chunkCenter;
}

public struct TerrainSectionMeshData
{
    public int LOD;
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] UVs;

    public Mesh CreateMesh()
    {
        Mesh m = new Mesh();
        m.vertices = vertices;
        m.triangles = triangles;
        m.uv = UVs;
        m.RecalculateNormals();
        return m;
    }
}

public struct TerrainSectionMeshBakeData
{

}