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
    BiomeSelector biomesInfo;
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
        biomesInfo.LoadRemapValues();
        biomes = biomesInfo.GetAllBiomes();
    }

    void Start()
    {
        randomSeed = seed;
        biomesInfo.LoadRemapValues();
        biomes = biomesInfo.GetAllBiomes();
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

                ret[x, y] = biomesInfo.GetBiomeForVals(Mathf.Clamp01(v1), Mathf.Clamp01(v2));
                if (ret[x, y] == -1)
                {
                    ret[x, y] = 0;
                }
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
        int vtxStride = levelOfDetail == 0 ? 1 : 2 * levelOfDetail;
        if (sectionSize % vtxStride != 0)
        {
            Debug.LogAssertion("Level of detail incompatible with section size");
            throw new Exception();
        }
        int vtxPerLine = (sectionSize / vtxStride) + 1;

        TerrainSectionMeshData ret = new TerrainSectionMeshData();
        ret.LOD = levelOfDetail;

        ret.vertices = new Vector3[vtxPerLine * vtxPerLine];
        ret.UVs = new Vector2[vtxPerLine * vtxPerLine];
        ret.triangles = new int[6 * sectionSize * sectionSize];

        int xi0 = terrainChunkData.numMarginPts + Mathf.RoundToInt(sectionCoord.x) * sectionSize;
        int yi0 = terrainChunkData.numMarginPts + Mathf.RoundToInt(sectionCoord.y) * sectionSize;
        float xoffset = terrainChunkData.chunkCenter.x - (float)terrainChunkData.chunkSideLength / 2f + sectionCoord.x * sectionSize;
        float yoffset = terrainChunkData.chunkCenter.y - (float)terrainChunkData.chunkSideLength / 2f + sectionCoord.y * sectionSize;

        float fullUVWidth = terrainChunkData.heightMap.GetLength(0) - 1;
        float xUV0 = (float)xi0 / fullUVWidth;
        float fullUVHeight = terrainChunkData.heightMap.GetLength(1) - 1;
        float yUV0 = (float)yi0 / fullUVHeight;

        int tridx = 0;
        int vi = 0;
        for (int y = 0; y < sectionSize + 1; y += vtxStride)
        {
            for (int x = 0; x < sectionSize + 1; x += vtxStride)
            {
                float z = terrainChunkData.heightMap[xi0 + x, yi0 + y];
                ret.vertices[vi] = new Vector3(xoffset + x, z, yoffset + y);
                ret.UVs[vi] = new Vector2((float)x / fullUVWidth + xUV0, (float)y / fullUVHeight + yUV0);
                if (x > 0 && y > 0)
                {
                    ret.triangles[tridx++] = vi - vtxPerLine - 1;
                    ret.triangles[tridx++] = vi;
                    ret.triangles[tridx++] = vi - vtxPerLine;
                    ret.triangles[tridx++] = vi - vtxPerLine - 1;
                    ret.triangles[tridx++] = vi - 1;
                    ret.triangles[tridx++] = vi;
                }

                vi++;
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

    [System.Serializable]
    struct BiomeSelector
    {
        [SerializeField]
        Texture2D biomeValueMap;
        [SerializeField]
        BiomeKeyEntry[] mapKeys;
        [SerializeField]
        TextAsset perlinRemapTextFile;

        private float[] remapValues;

        private int numPrintedWarnings;

        public void LoadRemapValues()
        {
            string remapTxt = perlinRemapTextFile.text;
            string[] lines = remapTxt.Split('\n');
            remapValues = new float[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Equals(""))
                    break;
                remapValues[i] = float.Parse(lines[i], System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowExponent);
            }

            numPrintedWarnings = 0;
        }

        public int GetBiomeForVals(float v1, float v2)
        {
            float rv1 = remapValues[Mathf.RoundToInt(v1 * (remapValues.Length - 1))];
            float rv2 = remapValues[Mathf.RoundToInt(v2 * (remapValues.Length - 1))];
            int x = Mathf.RoundToInt(rv1 * biomeValueMap.width);
            int y = Mathf.RoundToInt(rv2 * biomeValueMap.height);
            Color c = biomeValueMap.GetPixel(x, y);
            int cval = Mathf.RoundToInt(c.r * 255);
            // int cval = v1 > v2 ? 0 : 1;
            for (int i = 0; i < mapKeys.Length; i++)
            {
                if (mapKeys[i].colorVal == cval)
                    return i;
            }


            if (numPrintedWarnings++ < 10)
                Debug.LogWarning(string.Format("Couldn't get biome: v1={0}, v2={1}, rv1={2}, rv2={3}, c={4}, cval={5}", v1, v2, rv1, rv2, c, cval));

            return -1;
        }

        public Biome[] GetAllBiomes()
        {
            Biome[] ret = new Biome[mapKeys.Length];

            for (int i = 0; i < mapKeys.Length; i++)
                ret[i] = mapKeys[i].biome;

            return ret;
        }


        [System.Serializable]
        struct BiomeKeyEntry
        {
            public int colorVal;
            public Biome biome;
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