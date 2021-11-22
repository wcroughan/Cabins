using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField]
    int seed = 0;
    [SerializeField, Min(1)]
    int crossBiomeSmoothRange = 10;

    public const int LOD_MIN = 0, LOD_MAX = 6;

    public const int mapChunkNumVertices = 241;

    private int randomOffsetRange = 100000;

    Queue<TerrainCallbackInfo<TerrainChunkMeshData>> terrainChunkMeshCallbackQueue = new Queue<TerrainCallbackInfo<TerrainChunkMeshData>>();
    Queue<TerrainCallbackInfo<float[,]>> terrainChunkHeightCallbackQueue = new Queue<TerrainCallbackInfo<float[,]>>();

    //Singleton
    private static TerrainGenerator _instance;
    public static TerrainGenerator Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<TerrainGenerator>();
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    float GetHeightMapSample(Vector2 sample, Biome biome, Vector2[] noiseSampleOffsets)
    {
        float ret = 0;

        float scale = Mathf.Exp(biome.baseNoiseScale);
        float falloffFactor = 1f;
        float denom = 0f;
        for (int s = 0; s < biome.numNoiseScales; s++)
        {
            Vector2 scaleSample = (sample + noiseSampleOffsets[s]) * scale;
            ret += falloffFactor * Mathf.PerlinNoise(scaleSample.x, scaleSample.y);
            denom += falloffFactor;
            scale *= biome.scaleScalingFactor;
            falloffFactor *= biome.scaleFalloff;
        }

        ret /= denom;

        return Mathf.Clamp01(ret / denom);
    }

    public float[,] GenerateBiomeHeightMapChunk(Vector2 center, Biome biome, Vector2[] noiseSampleOffsets)
    {
        int dim = 2 * crossBiomeSmoothRange + mapChunkNumVertices;
        float[,] heightMap = new float[dim, dim];

        float topLeftX = (dim - 1) / -2f;
        float topLeftY = (dim - 1) / -2f;

        for (int x = 0; x < dim; x++)
        {
            for (int y = 0; y < dim; y++)
            {
                Vector2 samplePoint = new Vector2(topLeftX + x, topLeftY + y) + center;
                heightMap[x, y] = GetHeightMapSample(samplePoint, biome, noiseSampleOffsets);
            }
        }

        return heightMap;
    }

    float[,] GenerateSmoothedTransitionHeightMap(float[,][,] heightMaps)
    {
        int dim = 2 * crossBiomeSmoothRange + mapChunkNumVertices;
        float[,] ret = new float[dim, dim];

        return ret;
    }

    public TerrainChunkHeightData GenerateTerrainChunkHeightData(Vector2 center, Dictionary<Vector2, Biome> neighborBiomes)
    {
        int maxNumNoiseScales = 0;
        foreach (Biome b in neighborBiomes.Values)
        {
            if (b.numNoiseScales > maxNumNoiseScales)
                maxNumNoiseScales = b.numNoiseScales;
        }

        System.Random rngesus = new System.Random(seed);
        Vector2[] noiseSampleOffsets = new Vector2[maxNumNoiseScales];
        for (int i = 0; i < maxNumNoiseScales; i++)
        {
            float x = rngesus.Next(-randomOffsetRange, randomOffsetRange);
            float y = rngesus.Next(-randomOffsetRange, randomOffsetRange);
            noiseSampleOffsets[i] = new Vector2(x, y);
        }

        float[,][,] heightMaps = new float[3, 3][,];
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                Vector2 mapCenter = center + new Vector2((x - 1) * (mapChunkNumVertices - 1), (y - 1) * (mapChunkNumVertices - 1));
                heightMaps[x, y] = GenerateBiomeHeightMapChunk(mapCenter, neighborBiomes[new Vector2(x, y)], noiseSampleOffsets);
            }
        }

        float[,] heightMap = GenerateSmoothedTransitionHeightMap(heightMaps);

        return new TerrainChunkHeightData(heightMap, crossBiomeSmoothRange, neighborBiomes);
    }

    public TerrainChunkMeshData GenerateTerrainChunkMesh(TerrainChunkHeightData heightMapData, int levelOfDetail, Dictionary<Vector2, Biome> neighborBiomes)
    {
        //TODO rewrite
        Biome biome = neighborBiomes[Vector2.one];
        AnimationCurve heightCurve_ThreadSafe = new AnimationCurve(biome.heightCurve.keys);

        int fullMapWidth = heightMapData.heightMap.GetLength(0);
        int fullMapHeight = heightMapData.heightMap.GetLength(1);
        int meshWidth = fullMapWidth - 2 * heightMapData.marginVtxs;
        int meshHeight = fullMapHeight - 2 * heightMapData.marginVtxs;

        float topLeftX = (meshWidth - 1) / -2f;
        float topLeftY = (meshHeight - 1) / -2f;
        float topLeftU = (float)heightMapData.marginVtxs / (float)(fullMapWidth - 1);
        float topLeftV = (float)heightMapData.marginVtxs / (float)(fullMapHeight - 1);

        TerrainChunkMeshData ret = new TerrainChunkMeshData(meshWidth, meshHeight);

        if (levelOfDetail < LOD_MIN)
        {
            Debug.LogWarning("Level of detail too low (must be >= " + LOD_MIN + "): " + levelOfDetail);
            levelOfDetail = LOD_MIN;
        }
        else if (levelOfDetail > LOD_MAX)
        {
            Debug.LogWarning("Level of detail too high (must be <= " + LOD_MAX + "): " + levelOfDetail);
            levelOfDetail = LOD_MAX;
        }

        int vtxStride = (levelOfDetail == 0) ? 1 : 2 * levelOfDetail;
        int vtxPerLine = (meshWidth - 1) / vtxStride + 1;

        int vi = 0;
        int ti = 0;
        for (int x = 0; x < meshWidth; x += vtxStride)
        {
            for (int y = 0; y < meshHeight; y += vtxStride)
            {
                ret.meshVertices[vi] = new Vector3(topLeftX + x, heightCurve_ThreadSafe.Evaluate(heightMapData.heightMap[x, y]) * biome.heightMultiplier, topLeftY + y);
                ret.meshUVs[vi] = new Vector2(topLeftU + x, topLeftV + y);

                if (x < meshWidth - 1 && y < meshHeight - 1)
                {
                    ret.meshTriangles[ti++] = vi;
                    ret.meshTriangles[ti++] = vi + vtxPerLine + 1;
                    ret.meshTriangles[ti++] = vi + vtxPerLine;
                    ret.meshTriangles[ti++] = vi;
                    ret.meshTriangles[ti++] = vi + 1;
                    ret.meshTriangles[ti++] = vi + vtxPerLine + 1;
                }

                vi++;
            }
        }

        return ret;
    }


    public void RequestTerrainChunkHeightData(Action<TerrainChunkHeightData> callback, float xOffset, float yOffset, Biome biome)
    {
        ThreadStart threadStart = delegate
        {
            TerrainHeightDataRequestThread(callback, xOffset, yOffset, biome);
        };

        new Thread(threadStart).Start();
    }

    void TerrainHeightDataRequestThread(Action<TerrainChunkHeightData> callback, float xOffset, float yOffset, Biome biome)
    {
        float[,] terrainChunkData = GenerateHeightMap(xOffset, yOffset, biome);
        lock (terrainChunkHeightCallbackQueue)
        {
            terrainChunkHeightCallbackQueue.Enqueue(new TerrainCallbackInfo<TerrainChunkHeightData>(callback, terrainChunkData));
        }
    }
    public void RequestTerrainChunkMeshData(Action<TerrainChunkMeshData> callback, TerrainChunkHeightData heightMapData, int lod, Dictionary<Vector2, Biome> neighborBiomes)
    {
        ThreadStart threadStart = delegate
        {
            TerrainMeshDataRequestThread(callback, heightMapData, lod, neighborBiomes);
        };

        new Thread(threadStart).Start();
    }

    void TerrainMeshDataRequestThread(Action<TerrainChunkMeshData> callback, TerrainChunkHeightData heightMapData, int lod, Dictionary<Vector2, Biome> neighborBiomes)
    {
        TerrainChunkMeshData terrainChunkData = GenerateTerrainChunkMesh(heightMapData, lod, neighborBiomes);
        lock (terrainChunkMeshCallbackQueue)
        {
            terrainChunkMeshCallbackQueue.Enqueue(new TerrainCallbackInfo<TerrainChunkMeshData>(callback, terrainChunkData));
        }
    }

    void Update()
    {
        for (int i = 0; i < terrainChunkMeshCallbackQueue.Count; i++)
        {
            TerrainCallbackInfo<TerrainChunkMeshData> item = terrainChunkMeshCallbackQueue.Dequeue();
            item.callback(item.parameter);
        }
        for (int i = 0; i < terrainChunkHeightCallbackQueue.Count; i++)
        {
            TerrainCallbackInfo<TerrainChunkHeightData> item = terrainChunkHeightCallbackQueue.Dequeue();
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

public struct TerrainChunkMeshData
{
    public Vector3[] meshVertices;
    public int[] meshTriangles;
    public Vector2[] meshUVs;

    public TerrainChunkMeshData(int meshWidth, int meshHeight)
    {
        meshVertices = new Vector3[meshWidth * meshHeight];
        meshUVs = new Vector2[meshWidth * meshHeight];
        meshTriangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = meshVertices;
        mesh.triangles = meshTriangles;
        mesh.uv = meshUVs;
        mesh.RecalculateNormals();
        return mesh;
    }
}

public struct TerrainChunkHeightData
{
    //NOTE this height map is NOT on [0,1] scale, but is already evaluated mesh height according to biome
    public float[,] heightMap;
    public Dictionary<Vector2, Biome> neighborBiomes;
    public int marginVtxs;

    public TerrainChunkHeightData(float[,] heightMap, int marginVtxs, Dictionary<Vector2, Biome> neighborBiomes)
    {
        this.heightMap = heightMap;
        this.marginVtxs = marginVtxs;
        this.neighborBiomes = neighborBiomes;
    }
}