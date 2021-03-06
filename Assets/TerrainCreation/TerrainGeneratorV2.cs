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
    int biomeTransitionRadius = 10;
    [SerializeField]
    BiomeSelector biomesInfo;
    public static Biome[] biomes;
    public const int randomOffsetRange = 100;
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
        float biomeNoiseOffsetX1 = (float)rngesus.NextDouble() * (float)randomOffsetRange;
        float biomeNoiseOffsetY1 = (float)rngesus.NextDouble() * (float)randomOffsetRange;
        float biomeNoiseOffsetX2 = (float)rngesus.NextDouble() * (float)randomOffsetRange;
        float biomeNoiseOffsetY2 = (float)rngesus.NextDouble() * (float)randomOffsetRange;

        float x01 = biomeMapNoiseScale * ((float)(dim - 1) / -2f + chunkCenter.x) + biomeNoiseOffsetX1;
        float y01 = biomeMapNoiseScale * ((float)(dim - 1) / -2f + chunkCenter.y) + biomeNoiseOffsetY1;
        float x02 = biomeMapNoiseScale * ((float)(dim - 1) / -2f + chunkCenter.x) + biomeNoiseOffsetX2;
        float y02 = biomeMapNoiseScale * ((float)(dim - 1) / -2f + chunkCenter.y) + biomeNoiseOffsetY2;

        int[,] ret = new int[dim, dim];

        if (sw != null)
            sw.Write(chunkCenter.x + " " + chunkCenter.y + " " + x01 + " " + y01 + " " + x02 + " " + y02 + " " + biomeMapNoiseScale + " ");

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

                float rv1 = Mathf.Clamp01(Mathf.InverseLerp(-0.8f, 0.8f, v1));
                float rv2 = Mathf.Clamp01(Mathf.InverseLerp(-0.8f, 0.8f, v2));

                ret[x, y] = biomesInfo.GetBiomeForVals(rv1, rv2);
                if (ret[x, y] == -1)
                {
                    Debug.LogWarning("Got -1 as a biome");
                    ret[x, y] = 0;
                }

                if (sw != null)
                    sw.Write(x + " " + y + " " + sampX1 + " " + sampY1 + " " + sampX2 + " " + sampY1 + " " + v1 + " " + v2 + " " + rv1 + " " + rv2 + " " + ret[x, y] + " ");
            }

        }

        if (sw != null)
            sw.WriteLine();

        return ret;
    }

    private TerrainChunkData GenerateNewTerrainChunkData(Vector2 chunkCenter, int chunkSideLength, StreamWriter perlinValuesOut)
    {
        int numMarginPts = biomeTransitionRadius + 3;
        int dim = chunkSideLength + 1 + 2 * numMarginPts;


        // int[,] chunkBiomeMap = GenerateChunkBiomeMap(chunkCenter, dim, perlinValuesOut);
        int[,] chunkBiomeMap = GenerateChunkBiomeMap(chunkCenter, dim);


        if (perlinValuesOut != null)
        {
            perlinValuesOut.Write(chunkCenter.x + " " + chunkCenter.y + " " + chunkSideLength + " " + numMarginPts + " " + dim + " ");
            for (int x = 0; x < dim; x++)
            {
                for (int y = 0; y < dim; y++)
                {
                    perlinValuesOut.Write(x + " " + y + " " + chunkBiomeMap[x, y] + " ");
                }
            }
            perlinValuesOut.WriteLine();
        }

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
                    // if (perlinValuesOut != null && i == 0)
                    //     biomes[i].PopulateHeightMap(allHeightMaps, mask, numBiomesFilled, chunkCenter, perlinValuesOut);
                    // else
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

        int[,] chunkBiomeTextureMap = new int[dim, dim];
        float[,] heightMap = new float[dim, dim];
        Dictionary<int, int> biomeCountWithinRadius = new Dictionary<int, int>();
        for (int x = 0; x < dim; x++)
        {
            for (int y = 0; y < dim; y++)
            {
                for (int b = 0; b < numBiomes; b++)
                    biomeCountWithinRadius[b] = 0;
                float radiusDenom = 0;

                for (int xo = -biomeTransitionRadius; xo <= biomeTransitionRadius; xo++)
                {
                    if (x + xo < 0 || x + xo >= dim)
                        continue;
                    for (int yo = -biomeTransitionRadius; yo <= biomeTransitionRadius; yo++)
                    {
                        if (y + yo < 0 || y + yo >= dim)
                            continue;

                        biomeCountWithinRadius[chunkBiomeMap[x + xo, y + yo]]++;
                        radiusDenom++;
                    }
                }

                heightMap[x, y] = 0;
                float highestRepresentedHeight = float.NegativeInfinity;
                for (int b = 0; b < numBiomes; b++)
                {
                    int count = biomeCountWithinRadius[b];
                    if (count > 0 && Mathf.Abs(biomes[b].heightMultiplier) > highestRepresentedHeight)
                    {
                        highestRepresentedHeight = Mathf.Abs(biomes[b].heightMultiplier);
                        chunkBiomeTextureMap[x, y] = b;
                    }
                    int ahmi = heightMapIndexForBiomeIndex[b];
                    heightMap[x, y] += allHeightMaps[x, y, ahmi] * (float)biomeCountWithinRadius[b] / radiusDenom;

                }

                float sum = 0;
                for (int i = 0; i < numBiomes; i++)
                    sum += biomeCountWithinRadius[i];
                if (sum != radiusDenom)
                    throw new Exception("Found the glitch");

                // if (perlinValuesOut != null)
                // perlinValuesOut.Write(chunkCenter.x + " " + chunkCenter.y + " " + x + " " + y + " " + heightMap[x, y] + " ");
            }
        }
        // if (perlinValuesOut != null)
        //     perlinValuesOut.WriteLine();

        Color[] colorMap = new Color[dim * dim];
        //copying gradients to new ones for thread safety
        Gradient[] gradients = new Gradient[numBiomes];
        for (int i = 0; i < gradients.Length; i++)
        {
            gradients[i] = new Gradient();
            gradients[i].SetKeys(biomes[i].gradient.colorKeys, biomes[i].gradient.alphaKeys);
        }

        for (int x = 0; x < dim; x++)
        {
            for (int y = 0; y < dim; y++)
            {
                int texBiome = chunkBiomeTextureMap[x, y];
                colorMap[x + y * dim] = gradients[texBiome].Evaluate(heightMap[x, y] / biomes[texBiome].heightMultiplier);
            }
        }


        return new TerrainChunkData(heightMap, chunkBiomeMap, colorMap, chunkSideLength, numMarginPts, chunkCenter);
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
        bool overrideMapWithValue;
        [SerializeField]
        int overrideValue;

        private int numPrintedWarnings;

        private int textureWidth, textureHeight;
        private Color[] biomeValues;

        public void InitSelector()
        {
            numPrintedWarnings = 0;
            textureWidth = biomeValueMap.width;
            textureHeight = biomeValueMap.height;
            biomeValues = biomeValueMap.GetPixels(0, 0, textureWidth, textureHeight);
        }

        public int GetBiomeForVals(float v1, float v2)
        {
            if (overrideMapWithValue)
                return overrideValue;

            int x = Mathf.RoundToInt(v1 * (textureWidth - 1));
            int y = Mathf.RoundToInt(v2 * (textureHeight - 1));
            Color c = biomeValues[x + textureWidth * y];
            int cval = Mathf.RoundToInt(c.r * 255);
            // int cval = v1 > v2 ? 0 : 1;
            for (int i = 0; i < mapKeys.Length; i++)
            {
                if (mapKeys[i].colorVal == cval)
                    return i;
            }


            if (numPrintedWarnings++ < 10)
                Debug.LogWarning(string.Format("Couldn't get biome: v1={0}, v2={1}, c={2}, cval={3}", v1, v2, c, cval));

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

    public TerrainChunkData(float[,] heightMap, int[,] biomeMap, Color[] colorMap, int chunkSideLength, int numMarginPts, Vector2 chunkCenter)
    {
        this.heightMap = heightMap;
        this.biomeMap = biomeMap;
        this.chunkSideLength = chunkSideLength;
        this.numMarginPts = numMarginPts;
        this.chunkCenter = chunkCenter;
        textureCreated = false;
        texture = null;

        this.colorMap = colorMap;

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