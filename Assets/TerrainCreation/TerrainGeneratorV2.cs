using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.IO;

public class TerrainGeneratorV2 : MonoBehaviour
{
    [SerializeField]
    int seed;
    public static int randomSeed;
    [SerializeField, Range(-10, -5)]
    float biomeMapNoiseScaleFactor = -6f;
    float biomeMapNoiseScale;
    [SerializeField]
    BiomeSelector biomesInfo;
    public static Biome[] biomes;
    public const int randomOffsetRange = 100000;
    public const int LOD_MAX = 4;
    public const int LOD_MIN = 0;

    Queue<TerrainCallbackInfo<TerrainChunkData>> newTerrainChunkCallbackQueue = new Queue<TerrainCallbackInfo<TerrainChunkData>>();
    Queue<TerrainCallbackInfo<TerrainSectionMeshData>> terrainSectionMeshCallbackQueue = new Queue<TerrainCallbackInfo<TerrainSectionMeshData>>();
    Queue<TerrainCallbackInfo<TerrainSectionMeshBakeData>> terrainSectionMeshBakeCallbackQueue = new Queue<TerrainCallbackInfo<TerrainSectionMeshBakeData>>();

    void OnValidate()
    {
        biomeMapNoiseScale = Mathf.Exp(biomeMapNoiseScaleFactor);
        randomSeed = seed;
        biomesInfo.InitSelector();
        biomes = biomesInfo.GetAllBiomes();
    }

    void Start()
    {
        biomeMapNoiseScale = Mathf.Exp(biomeMapNoiseScaleFactor);
        randomSeed = seed;
        biomesInfo.InitSelector();
        biomes = biomesInfo.GetAllBiomes();
    }

    private int[,] GenerateChunkBiomeMap(Vector2 chunkCenter, int dim, StreamWriter sw = null)
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


                // float v1 = Mathf.PerlinNoise(sampX1, sampY1);
                // float v2 = Mathf.PerlinNoise(sampX2, sampY2);
                //simplex noise seems pretty uniform between -0.7 and 0.7
                float v1 = Unity.Mathematics.noise.snoise(new Unity.Mathematics.float2(sampX1, sampY1));
                float v2 = Unity.Mathematics.noise.snoise(new Unity.Mathematics.float2(sampX2, sampY2));

                ret[x, y] = biomesInfo.GetBiomeForVals(Mathf.Clamp01(Mathf.InverseLerp(-0.8f, 0.8f, v1)), Mathf.Clamp01(Mathf.InverseLerp(-0.8f, 0.8f, v2)));
                if (ret[x, y] == -1)
                {
                    ret[x, y] = 0;
                }

                if (sw != null)
                {
                    // sw.WriteLine(v1 + " " + v2);
                }
            }

        }

        return ret;
    }

    private TerrainChunkData GenerateNewTerrainChunkData(Vector2 chunkCenter, int chunkSideLength, StreamWriter perlinValuesOut)
    {
        int numMarginPts = 5;
        int dim = chunkSideLength + 1 + 2 * numMarginPts;


        int[,] chunkBiomeMap = GenerateChunkBiomeMap(chunkCenter, dim, perlinValuesOut);

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

        float[,] heightMap = new float[dim, dim];
        for (int x = 0; x < dim; x++)
        {
            for (int y = 0; y < dim; y++)
            {
                int ahmi = heightMapIndexForBiomeIndex[chunkBiomeMap[x, y]];
                heightMap[x, y] = allHeightMaps[x, y, ahmi];
            }
        }

        return new TerrainChunkData(heightMap, chunkBiomeMap, chunkSideLength, numMarginPts, chunkCenter);
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
                float height = terrainChunkData.heightMap[xi0 + x, yi0 + y];
                ret.vertices[vi] = new Vector3(xoffset + x, height, yoffset + y);
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

    public void RequestNewChunkData(Action<TerrainChunkData> callback, Vector2 chunkCenter, int chunkSideLength, bool startParallelTask = true, StreamWriter perlinValuesOut = null)
    {
        if (startParallelTask)
            Task.Run(() =>
            {
                NewChunkRequestThread(callback, chunkCenter, chunkSideLength, startParallelTask, perlinValuesOut);
            });
        else
            NewChunkRequestThread(callback, chunkCenter, chunkSideLength, startParallelTask, perlinValuesOut);
    }

    private void NewChunkRequestThread(Action<TerrainChunkData> callback, Vector2 chunkCenter, int chunkSideLength, bool parallelTask, StreamWriter perlinValuesOut)
    {
        TerrainChunkData terrainChunkData = GenerateNewTerrainChunkData(chunkCenter, chunkSideLength, perlinValuesOut);

        if (parallelTask)
            lock (newTerrainChunkCallbackQueue)
            {
                newTerrainChunkCallbackQueue.Enqueue(new TerrainCallbackInfo<TerrainChunkData>(callback, terrainChunkData));
            }
        else
            callback(terrainChunkData);
    }


    public void RequestSectionMesh(Action<TerrainSectionMeshData> callback, TerrainChunkData terrainChunkData, Vector2 sectionCoord, int sectionSize, int levelOfDetail, bool startParallelTask = true)
    {
        if (startParallelTask)
            Task.Run(() =>
            {
                SectionMeshRequestThread(callback, terrainChunkData, sectionCoord, sectionSize, levelOfDetail, startParallelTask);
            });
        else
            SectionMeshRequestThread(callback, terrainChunkData, sectionCoord, sectionSize, levelOfDetail, startParallelTask);
    }

    private void SectionMeshRequestThread(Action<TerrainSectionMeshData> callback, TerrainChunkData terrainChunkData, Vector2 sectionCoord, int sectionSize, int levelOfDetail, bool parallelTask)
    {
        TerrainSectionMeshData terrainSectionMeshData = GenerateTerrainSectionMeshData(terrainChunkData, sectionCoord, sectionSize, levelOfDetail);

        if (parallelTask)
            lock (terrainSectionMeshCallbackQueue)
            {
                terrainSectionMeshCallbackQueue.Enqueue(new TerrainCallbackInfo<TerrainSectionMeshData>(callback, terrainSectionMeshData));
            }
        else
            callback(terrainSectionMeshData);
    }

    public void RequestSectionMeshBake(Action<TerrainSectionMeshBakeData> callback, int meshID, bool startParallelTask = true)
    {
        if (startParallelTask)
            Task.Run(() =>
            {
                SectionMeshBakeThread(callback, meshID, startParallelTask);
            });
        else
            SectionMeshBakeThread(callback, meshID, startParallelTask);
    }

    private void SectionMeshBakeThread(Action<TerrainSectionMeshBakeData> callback, int meshID, bool parallelTask)
    {
        Physics.BakeMesh(meshID, false);

        if (parallelTask)
            lock (terrainSectionMeshBakeCallbackQueue)
            {
                terrainSectionMeshBakeCallbackQueue.Enqueue(new TerrainCallbackInfo<TerrainSectionMeshBakeData>(callback, new TerrainSectionMeshBakeData()));
            }
        else
            callback(new TerrainSectionMeshBakeData());
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
        [SerializeField]
        bool overrideMapWithValue;
        [SerializeField]
        int overrideValue;


        private float[] remapValues;

        private int numPrintedWarnings;

        private int textureWidth, textureHeight;
        private Color[] biomeValues;

        public void InitSelector()
        {
            LoadRemapValues();
            textureWidth = biomeValueMap.width;
            textureHeight = biomeValueMap.height;
            biomeValues = biomeValueMap.GetPixels(0, 0, textureWidth, textureHeight);
        }

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
            if (overrideMapWithValue)
                return overrideValue;

            // float rv1 = remapValues[Mathf.RoundToInt(v1 * (remapValues.Length - 1))];
            // float rv2 = remapValues[Mathf.RoundToInt(v2 * (remapValues.Length - 1))];
            float rv1 = v1;
            float rv2 = v2;
            int x = Mathf.RoundToInt(rv1 * (textureWidth - 1));
            int y = Mathf.RoundToInt(rv2 * (textureHeight - 1));
            Color c = biomeValues[x + textureWidth * y];
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
    public int[,] biomeMap;
    public int chunkSideLength;
    public int numMarginPts;
    public Vector2 chunkCenter;

    private bool textureCreated;
    private Texture2D texture;
    private Color[] colorMap;

    public TerrainChunkData(float[,] heightMap, int[,] biomeMap, int chunkSideLength, int numMarginPts, Vector2 chunkCenter)
    {
        this.heightMap = heightMap;
        this.biomeMap = biomeMap;
        this.chunkSideLength = chunkSideLength;
        this.numMarginPts = numMarginPts;
        this.chunkCenter = chunkCenter;
        textureCreated = false;
        texture = null;

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        colorMap = new Color[width * height];

        //copying gradients to new ones for thread safety
        Gradient[] gradients = new Gradient[TerrainGeneratorV2.biomes.Length];
        for (int i = 0; i < gradients.Length; i++)
        {
            gradients[i] = new Gradient();
            gradients[i].SetKeys(TerrainGeneratorV2.biomes[i].gradient.colorKeys, TerrainGeneratorV2.biomes[i].gradient.alphaKeys);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                colorMap[x + y * width] = gradients[biomeMap[x, y]].Evaluate(heightMap[x, y] / TerrainGeneratorV2.biomes[biomeMap[x, y]].heightMultiplier);
            }
        }
    }

    public Texture2D GetTexture()
    {
        if (!textureCreated)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);

            texture = new Texture2D(width, height);
            texture.wrapMode = TextureWrapMode.Clamp; // this shouldn't matter, since UV staying away from border
            texture.SetPixels(colorMap);
            texture.Apply();

            textureCreated = true;
        }
        return texture;
    }
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