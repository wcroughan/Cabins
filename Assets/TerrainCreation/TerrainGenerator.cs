using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;

public class TerrainGenerator : MonoBehaviour
{
    [SerializeField]
    int seed = 0;
    [SerializeField, Min(1)]
    int crossBiomeHeightSmoothRange = 10;

    public const int LOD_MIN = 0, LOD_MAX = 4;

    // public const int mapChunkNumVertices = 73;
    public const int mapChunkNumVertices = 241;

    private int randomOffsetRange = 100000;

    Queue<TerrainCallbackInfo<TerrainChunkMeshData>> terrainChunkMeshCallbackQueue = new Queue<TerrainCallbackInfo<TerrainChunkMeshData>>();
    Queue<TerrainCallbackInfo<TerrainChunkHeightData>> terrainChunkHeightCallbackQueue = new Queue<TerrainCallbackInfo<TerrainChunkHeightData>>();
    Queue<TerrainCallbackInfo<bool>> terrainMeshBakeCallbackQueue = new Queue<TerrainCallbackInfo<bool>>();


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

        // float a = Mathf.Abs(sample.x / 10f);
        // return a - Mathf.Floor(a);
    }

    public float[,] GenerateBiomeHeightMapChunk(Vector2 center, Biome biome, Vector2[] noiseSampleOffsets)
    {
        AnimationCurve heightCurve_ThreadSafe = new AnimationCurve(biome.heightCurve.keys);
        int dim = 2 * crossBiomeHeightSmoothRange + mapChunkNumVertices;
        float[,] heightMap = new float[dim, dim];

        float topLeftX = (dim - 1) / -2f;
        float topLeftY = (dim - 1) / -2f;

        for (int x = 0; x < dim; x++)
        {
            for (int y = 0; y < dim; y++)
            {
                Vector2 samplePoint = new Vector2(topLeftX + x, topLeftY + y) + center;
                float height01 = GetHeightMapSample(samplePoint, biome, noiseSampleOffsets);
                heightMap[x, y] = heightCurve_ThreadSafe.Evaluate(height01) * biome.heightMultiplier;
            }
        }

        return heightMap;
    }

    void PopulateSmoothMapCorner(float[,] mapTL, float[,] mapTR, float[,] mapBL, float[,] mapBR, float[,] mapToPopulate, int mapOutXStart, int mapOutYStart)
    {
        const int mtlXOff = mapChunkNumVertices - 1, mtlYOff = mapChunkNumVertices - 1;
        const int mtrXOff = 0, mtrYOff = mapChunkNumVertices - 1;
        const int mblXOff = mapChunkNumVertices - 1, mblYOff = 0;
        const int mbrXOff = 0, mbrYOff = 0;
        for (int x = 0; x < 2 * crossBiomeHeightSmoothRange + 1; x++)
        {
            float xlval = ((float)x) / (2f * (float)crossBiomeHeightSmoothRange);
            Vector4 w1 = Vector4.Lerp(new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), xlval);
            Vector4 w2 = Vector4.Lerp(new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1), xlval);
            for (int y = 0; y < 2 * crossBiomeHeightSmoothRange + 1; y++)
            {
                float ylval = ((float)y) / (2f * (float)crossBiomeHeightSmoothRange);
                Vector4 mapWeights = Vector4.Lerp(w1, w2, ylval);

                float mwsum = mapWeights.x + mapWeights.y + mapWeights.z + mapWeights.w;
                if (mwsum < 0.999 || mwsum > 1.001)
                    Debug.Log(string.Format("{0:G} => {1:G} => {2:G}, {3:G} ==> {4:G} ({5:G})", new Vector2(x, y), new Vector2(xlval, ylval), w1, w2, mapWeights, mwsum));


                float vtl = mapWeights.x * mapTL[x + mtlXOff, y + mtlYOff];
                float vtr = mapWeights.y * mapTR[x + mtrXOff, y + mtrYOff];
                float vbl = mapWeights.z * mapBL[x + mblXOff, y + mblYOff];
                float vbr = mapWeights.w * mapBR[x + mbrXOff, y + mbrYOff];
                mapToPopulate[x + mapOutXStart, y + mapOutYStart] = vtl + vtr + vbl + vbr;
            }
        }
    }

    void PopulateSmoothMapTopBottomEdge(float[,] mapTop, float[,] mapBot, float[,] mapToPopulate, int mapOutYStart)
    {
        int mtXOff = 2 * crossBiomeHeightSmoothRange + 1, mtYOff = mapChunkNumVertices - 1;
        int mbXOff = 2 * crossBiomeHeightSmoothRange + 1, mbYOff = 0;
        int mapOutXOff = 2 * crossBiomeHeightSmoothRange + 1;

        for (int x = 0; x < mapChunkNumVertices - (2 * crossBiomeHeightSmoothRange + 1); x++)
        {
            for (int y = 0; y < 2 * crossBiomeHeightSmoothRange + 1; y++)
            {
                float ylerpval = (float)y / (float)(2f * crossBiomeHeightSmoothRange);
                float topval = mapTop[x + mtXOff, y + mtYOff];
                float botval = mapBot[x + mbXOff, y + mbYOff];
                mapToPopulate[x + mapOutXOff, y + mapOutYStart] = Mathf.Lerp(topval, botval, ylerpval);
            }
        }
    }

    void PopulateSmoothMapLeftRightEdge(float[,] mapL, float[,] mapR, float[,] mapToPopulate, int mapOutXStart)
    {
        int mlXOff = mapChunkNumVertices - 1, mlYOff = 2 * crossBiomeHeightSmoothRange + 1;
        int mrXOff = 0, mrYOff = 2 * crossBiomeHeightSmoothRange + 1;
        int mapOutYOff = 2 * crossBiomeHeightSmoothRange + 1;

        for (int x = 0; x < 2 * crossBiomeHeightSmoothRange + 1; x++)
        {
            float xlerpval = (float)x / (float)(2f * crossBiomeHeightSmoothRange);
            for (int y = 0; y < mapChunkNumVertices - (2 * crossBiomeHeightSmoothRange + 1); y++)
            {
                float lval = mapL[x + mlXOff, y + mlYOff];
                float rval = mapR[x + mrXOff, y + mrYOff];
                mapToPopulate[x + mapOutXStart, y + mapOutYOff] = Mathf.Lerp(lval, rval, xlerpval);
            }
        }
    }


    float[,] GenerateSmoothedTransitionHeightMap(float[,][,] heightMaps)
    {
        int dim = 2 * crossBiomeHeightSmoothRange + mapChunkNumVertices;
        float[,] ret = new float[dim, dim];

        PopulateSmoothMapCorner(heightMaps[0, 0], heightMaps[1, 0], heightMaps[0, 1], heightMaps[1, 1], ret, 0, 0); //top left
        PopulateSmoothMapCorner(heightMaps[1, 0], heightMaps[2, 0], heightMaps[1, 1], heightMaps[2, 1], ret, mapChunkNumVertices - 1, 0); //top right
        PopulateSmoothMapCorner(heightMaps[0, 1], heightMaps[1, 1], heightMaps[0, 2], heightMaps[1, 2], ret, 0, mapChunkNumVertices - 1); //bottom left
        PopulateSmoothMapCorner(heightMaps[1, 1], heightMaps[2, 1], heightMaps[1, 2], heightMaps[2, 2], ret, mapChunkNumVertices - 1, mapChunkNumVertices - 1); //bottom right

        PopulateSmoothMapLeftRightEdge(heightMaps[0, 1], heightMaps[1, 1], ret, 0); // left edge
        PopulateSmoothMapLeftRightEdge(heightMaps[1, 1], heightMaps[2, 1], ret, mapChunkNumVertices - 1); // right edge

        PopulateSmoothMapTopBottomEdge(heightMaps[1, 0], heightMaps[1, 1], ret, 0); // top edge
        PopulateSmoothMapTopBottomEdge(heightMaps[1, 1], heightMaps[1, 2], ret, mapChunkNumVertices - 1); // bottom edge

        for (int x = 2 * crossBiomeHeightSmoothRange + 1; x < mapChunkNumVertices - 1; x++)
        {
            for (int y = 2 * crossBiomeHeightSmoothRange + 1; y < mapChunkNumVertices - 1; y++)
            {
                ret[x, y] = heightMaps[1, 1][x, y];
            }
        }




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
                heightMaps[x, y] = GenerateBiomeHeightMapChunk(mapCenter, neighborBiomes[new Vector2(x - 1, y - 1)], noiseSampleOffsets);
            }
        }

        float[,] heightMap = GenerateSmoothedTransitionHeightMap(heightMaps);


        return new TerrainChunkHeightData(heightMap, crossBiomeHeightSmoothRange, neighborBiomes);
    }

    public TerrainChunkMeshData GenerateTerrainChunkMesh(TerrainChunkHeightData heightMapData, int levelOfDetail)
    {
        int fullMapWidth = heightMapData.heightMap.GetLength(0);
        int fullMapHeight = heightMapData.heightMap.GetLength(1);
        int meshWidth = mapChunkNumVertices;
        int meshHeight = mapChunkNumVertices;
        int heightMapMargin = heightMapData.marginVtxs;

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
                ret.meshVertices[vi] = new Vector3(topLeftX + x, heightMapData.heightMap[x + heightMapMargin, y + heightMapMargin], topLeftY + y);
                ret.meshUVs[vi] = new Vector2(topLeftU + (float)x / (fullMapWidth - 1f), topLeftV + (float)y / (fullMapHeight - 1f));

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

        ret.BakeNormals();

        return ret;
    }


    public void RequestTerrainChunkHeightData(Action<TerrainChunkHeightData> callback, Vector2 offset, Dictionary<Vector2, Biome> neighborBiomes)
    {
        Task.Run(() =>
        {
            TerrainHeightDataRequestThread(callback, offset, neighborBiomes);
        });
    }

    void TerrainHeightDataRequestThread(Action<TerrainChunkHeightData> callback, Vector2 offset, Dictionary<Vector2, Biome> neighborBiomes)
    {
        TerrainChunkHeightData terrainChunkHeightData = GenerateTerrainChunkHeightData(offset, neighborBiomes);
        lock (terrainChunkHeightCallbackQueue)
        {
            terrainChunkHeightCallbackQueue.Enqueue(new TerrainCallbackInfo<TerrainChunkHeightData>(callback, terrainChunkHeightData));
        }
    }

    public void RequestTerrainMeshBake(Action<bool> callback, int meshid)
    {
        Task.Run(() =>
        {
            TerrainMeshBakeRequestThread(callback, meshid);
        });
    }

    void TerrainMeshBakeRequestThread(Action<bool> callback, int meshid)
    {
        Physics.BakeMesh(meshid, false);
        lock (terrainMeshBakeCallbackQueue)
        {
            terrainMeshBakeCallbackQueue.Enqueue(new TerrainCallbackInfo<bool>(callback, true));
        }
    }


    public void RequestTerrainChunkMeshData(Action<TerrainChunkMeshData> callback, TerrainChunkHeightData heightMapData, int lod)
    {
        Task.Run(() =>
        {
            TerrainMeshDataRequestThread(callback, heightMapData, lod);
        });
    }

    void TerrainMeshDataRequestThread(Action<TerrainChunkMeshData> callback, TerrainChunkHeightData heightMapData, int lod)
    {
        TerrainChunkMeshData terrainChunkMeshData = GenerateTerrainChunkMesh(heightMapData, lod);
        lock (terrainChunkMeshCallbackQueue)
        {
            terrainChunkMeshCallbackQueue.Enqueue(new TerrainCallbackInfo<TerrainChunkMeshData>(callback, terrainChunkMeshData));
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
        for (int i = 0; i < terrainMeshBakeCallbackQueue.Count; i++)
        {
            TerrainCallbackInfo<bool> item = terrainMeshBakeCallbackQueue.Dequeue();
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
    public Vector3[] bakedNormals;

    public TerrainChunkMeshData(int meshWidth, int meshHeight)
    {
        meshVertices = new Vector3[meshWidth * meshHeight];
        meshUVs = new Vector2[meshWidth * meshHeight];
        meshTriangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
        bakedNormals = null;
    }

    Vector3[] CalculateNormals()
    {
        Vector3[] ret = new Vector3[meshVertices.Length];

        for (int ti = 0; ti < meshTriangles.Length; ti += 3)
        {
            int a = meshTriangles[ti];
            int b = meshTriangles[ti + 1];
            int c = meshTriangles[ti + 2];
            Vector3 triNormal = TriangleNormal(a, b, c);

            ret[a] += triNormal;
            ret[b] += triNormal;
            ret[c] += triNormal;
        }

        foreach (Vector3 v in ret)
        {
            v.Normalize();
        }

        return ret;
    }

    Vector3 TriangleNormal(int a, int b, int c)
    {
        Vector3 va = meshVertices[a];
        Vector3 vb = meshVertices[b];
        Vector3 vc = meshVertices[c];

        Vector3 dab = vb - va;
        Vector3 dac = vc - va;
        return Vector3.Cross(dab, dac).normalized;
    }

    public void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = meshVertices;
        mesh.triangles = meshTriangles;
        mesh.uv = meshUVs;
        mesh.normals = bakedNormals;
        return mesh;
    }
}

public struct TerrainChunkHeightData
{
    //NOTE this height map is NOT on [0,1] scale, but is already evaluated mesh height according to biome with smoothing at transitions
    //Therefore, at edges of biome, heights might be out of typical range
    public float[,] heightMap;
    public Dictionary<Vector2, Biome> neighborBiomes;
    public int marginVtxs;
    Color[] colorMap;

    public TerrainChunkHeightData(float[,] heightMap, int marginVtxs, Dictionary<Vector2, Biome> neighborBiomes)
    {
        this.heightMap = heightMap;
        this.marginVtxs = marginVtxs;
        this.neighborBiomes = neighborBiomes;

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Biome biome = neighborBiomes[Vector2.zero];
        Gradient gradient_ThreadSafe = new Gradient();
        gradient_ThreadSafe.SetKeys(biome.gradient.colorKeys, biome.gradient.alphaKeys);

        colorMap = new Color[width * height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < width; y++)
            {
                colorMap[x + y * width] = gradient_ThreadSafe.Evaluate(heightMap[x, y] / biome.heightMultiplier);
            }
        }
    }

    public Texture2D CreateTexture()
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Texture2D texture = new Texture2D(width, height);

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();

        return texture;
    }
}