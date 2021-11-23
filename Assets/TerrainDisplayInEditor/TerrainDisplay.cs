using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainDisplay : MonoBehaviour
{
    [SerializeField]
    public bool autoUpdate;
    [SerializeField]
    Vector2 coord;
    [SerializeField, Range(0, 6)]
    int levelOfDetail;
    [SerializeField]
    Biome centerBiome;
    [SerializeField]
    Biome borderBiomeX, borderBiomeY, borderBiomeCorner;

    // Start is called before the first frame update
    void Start()
    {
        InitTerrain();
    }

    void OnValidate()
    {
        if (centerBiome != null)
        {
            centerBiome.OnValuesUpdated -= OnValuesUpdated;
            centerBiome.OnValuesUpdated += OnValuesUpdated;
        }
        if (borderBiomeX != null)
        {
            borderBiomeX.OnValuesUpdated -= OnValuesUpdated;
            borderBiomeX.OnValuesUpdated += OnValuesUpdated;
        }
        if (borderBiomeY != null)
        {
            borderBiomeY.OnValuesUpdated -= OnValuesUpdated;
            borderBiomeY.OnValuesUpdated += OnValuesUpdated;
        }
        if (borderBiomeCorner != null)
        {
            borderBiomeCorner.OnValuesUpdated -= OnValuesUpdated;
            borderBiomeCorner.OnValuesUpdated += OnValuesUpdated;
        }
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            InitTerrain();
        }
    }

    public void InitTerrain()
    {
        Dictionary<Vector2, Biome> neighborBiomes = new Dictionary<Vector2, Biome>();
        neighborBiomes[new Vector2(-1, -1)] = borderBiomeCorner;
        neighborBiomes[new Vector2(0, -1)] = borderBiomeY;
        neighborBiomes[new Vector2(1, -1)] = borderBiomeCorner;
        neighborBiomes[new Vector2(-1, 0)] = borderBiomeX;
        neighborBiomes[new Vector2(0, 0)] = centerBiome;
        neighborBiomes[new Vector2(1, 0)] = borderBiomeX;
        neighborBiomes[new Vector2(-1, 1)] = borderBiomeCorner;
        neighborBiomes[new Vector2(0, 1)] = borderBiomeY;
        neighborBiomes[new Vector2(1, 1)] = borderBiomeCorner;

        TerrainGenerator tgen = TerrainGenerator.Instance;
        TerrainChunkHeightData terrainChunkHeightData = tgen.GenerateTerrainChunkHeightData(coord, neighborBiomes);
        TerrainChunkMeshData tc = tgen.GenerateTerrainChunkMesh(terrainChunkHeightData, levelOfDetail);
        float[,] heightMap = terrainChunkHeightData.heightMap;
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        Texture2D texture = new Texture2D(width, height);

        Color[] colorMap = new Color[width * height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < width; y++)
            {
                colorMap[x + y * width] = centerBiome.GetColorForHeight(heightMap[x, y]);
            }
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();

        MeshFilter filter = GetComponent<MeshFilter>();
        filter.sharedMesh = tc.CreateMesh();

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.sharedMaterial.mainTexture = texture;
        // renderer.transform.localScale = new Vector3(width / 10f, 1, height / 10f);
        // renderer.transform.localScale = new Vector3(width, 1, height);
    }

}
