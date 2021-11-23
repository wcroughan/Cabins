using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    [SerializeField]
    Transform viewer;
    [SerializeField]
    GameObject terrainChunkPrefab;
    static GameObject chunkPrefab;

    [SerializeField]
    Biome[] biomes;
    [SerializeField]
    LODThreshInfo[] detailLevels;

    static int maxMapViewDistance;

    const float viewerMoveThreshForChunkUpdate = 25f;
    const float viewerMoveThreshForChunkUpdate_sq = viewerMoveThreshForChunkUpdate * viewerMoveThreshForChunkUpdate;

    int chunkSize;
    int chunksVisibleInViewDistance;

    static Vector2 oldViewerPosition;
    static Vector2 viewerPosition;

    static TerrainGenerator terrainGenerator;

    Dictionary<Vector2, TerrainChunkGameObject> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunkGameObject>();
    static List<TerrainChunkGameObject> chunksVisibleLastUpdate = new List<TerrainChunkGameObject>();

    // Start is called before the first frame update
    void Start()
    {
        maxMapViewDistance = detailLevels[detailLevels.Length - 1].distThresh;
        chunkPrefab = terrainChunkPrefab;
        terrainGenerator = FindObjectOfType<TerrainGenerator>();
        chunkSize = TerrainGenerator.mapChunkNumVertices - 1;
        chunksVisibleInViewDistance = maxMapViewDistance / chunkSize;
        UpdateViewableChunks();
    }

    // Update is called once per frame
    void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if ((viewerPosition - oldViewerPosition).sqrMagnitude > viewerMoveThreshForChunkUpdate_sq)
        {
            oldViewerPosition = viewerPosition;
            UpdateViewableChunks();
        }
    }

    Biome GetBiomeForCoord(Vector2 coord)
    {
        // return biomes[0];
        return biomes[Mathf.Abs(Mathf.RoundToInt(coord.x / 2f + coord.y / 3f)) % biomes.Length];
    }

    Dictionary<Vector2, Biome> GetNeighborBiomesForCoord(Vector2 coord)
    {
        Dictionary<Vector2, Biome> ret = new Dictionary<Vector2, Biome>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2 vec = new Vector2(x, y);
                ret[vec] = GetBiomeForCoord(coord + vec);
            }
        }
        return ret;
    }

    void UpdateViewableChunks()
    {
        foreach (TerrainChunkGameObject tc in chunksVisibleLastUpdate)
            tc.SetVisible(false);

        int currentChunkX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int xOffset = -chunksVisibleInViewDistance; xOffset <= chunksVisibleInViewDistance; xOffset++)
        {
            for (int yOffset = -chunksVisibleInViewDistance; yOffset <= chunksVisibleInViewDistance; yOffset++)
            {
                Vector2 chunkCoord = new Vector2(currentChunkX + xOffset, currentChunkY + yOffset);

                if (terrainChunkDictionary.ContainsKey(chunkCoord))
                {
                    terrainChunkDictionary[chunkCoord].UpdateViewable();
                }
                else
                {
                    Dictionary<Vector2, Biome> neighborBiomes = GetNeighborBiomesForCoord(chunkCoord);
                    terrainChunkDictionary.Add(chunkCoord, new TerrainChunkGameObject(chunkCoord, chunkSize, transform, neighborBiomes[Vector2.zero], neighborBiomes, detailLevels));
                }
            }
        }
    }

    public class TerrainChunkGameObject
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        Biome biome;
        Dictionary<Vector2, Biome> neighborBiomes;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        Rigidbody rigidbody;

        LODMesh[] lODMeshes;
        LODThreshInfo[] lODThreshInfos;
        int previousLOD = -1;
        bool heightMapReceived = false;
        TerrainChunkHeightData terrainChunkHeightData;

        public TerrainChunkGameObject(Vector2 coord, int size, Transform parent, Biome biome, Dictionary<Vector2, Biome> neighborBiomes, LODThreshInfo[] detailLevels)
        {
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = Instantiate(chunkPrefab, positionV3, Quaternion.identity);
            meshObject.name = "Chunk " + coord;
            meshRenderer = meshObject.GetComponent<MeshRenderer>();
            meshFilter = meshObject.GetComponent<MeshFilter>();
            rigidbody = meshObject.GetComponent<Rigidbody>();
            meshObject.transform.position = positionV3;
            meshObject.transform.SetParent(parent, false);

            SetVisible(false);

            const int numLODs = TerrainGenerator.LOD_MAX - TerrainGenerator.LOD_MIN + 1;
            lODMeshes = new LODMesh[numLODs];
            for (int i = 0; i < numLODs; i++)
            {
                lODMeshes[i] = new LODMesh(i, UpdateViewable, neighborBiomes);
            }
            this.lODThreshInfos = detailLevels;

            this.biome = biome;
            this.neighborBiomes = neighborBiomes;

            terrainGenerator.RequestTerrainChunkHeightData(OnTerrainChunkHeightReceived, position, neighborBiomes);
        }

        void OnTerrainChunkHeightReceived(TerrainChunkHeightData terrainChunkHeightData)
        {
            this.terrainChunkHeightData = terrainChunkHeightData;

            int width = terrainChunkHeightData.heightMap.GetLength(0);
            int height = terrainChunkHeightData.heightMap.GetLength(1);
            Texture2D texture = new Texture2D(width, height);

            Color[] colorMap = new Color[width * height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    colorMap[x + y * width] = biome.GetColorForHeight(terrainChunkHeightData.heightMap[x, y]);
                }
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(colorMap);
            texture.Apply();
            meshRenderer.material.mainTexture = texture;

            heightMapReceived = true;
            UpdateViewable();
        }

        int GetLODForSqDist(float sqdist)
        {
            int lodi = 0;
            for (int i = 0; i < lODThreshInfos.Length; i++)
            {
                if (sqdist > lODThreshInfos[i].distThresh * lODThreshInfos[i].distThresh)
                {
                    lodi++;
                }
                else
                {
                    break;
                }
            }
            return lODThreshInfos[lodi].lod;
        }

        public void UpdateViewable()
        {
            if (!heightMapReceived)
                return;

            float viewDistance = bounds.SqrDistance(viewerPosition);
            bool viewable = viewDistance <= maxMapViewDistance * maxMapViewDistance;

            if (viewable)
            {
                // int lod = Mathf.FloorToInt();
                int lod = GetLODForSqDist(viewDistance);

                if (lod != previousLOD)
                {
                    LODMesh lm = lODMeshes[lod];
                    if (lm.meshReceived)
                    {
                        previousLOD = lod;
                        meshFilter.mesh = lm.mesh;
                    }
                    else if (!lm.meshRequested)
                    {
                        lm.RequestMesh(terrainChunkHeightData);
                    }
                }

                chunksVisibleLastUpdate.Add(this);
            }

            SetVisible(viewable);
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }
    }

    class LODMesh
    {
        int lod;
        public bool meshRequested;
        public bool meshReceived;
        public Mesh mesh;
        System.Action updateCallback;
        Dictionary<Vector2, Biome> neighborBiomes;

        public LODMesh(int lod, System.Action callback, Dictionary<Vector2, Biome> neighborBiomes)
        {
            this.lod = lod;
            this.updateCallback = callback;
            this.neighborBiomes = neighborBiomes;
        }

        void OnMeshDataReceived(TerrainChunkMeshData terrainChunkMeshData)
        {
            mesh = terrainChunkMeshData.CreateMesh();
            meshReceived = true;
            updateCallback();
        }

        public void RequestMesh(TerrainChunkHeightData terrainChunkHeightData)
        {
            meshRequested = true;
            terrainGenerator.RequestTerrainChunkMeshData(OnMeshDataReceived, terrainChunkHeightData, lod);
        }
    }

    [System.Serializable]
    public struct LODThreshInfo
    {
        public int lod;
        public int distThresh;
    }
}
