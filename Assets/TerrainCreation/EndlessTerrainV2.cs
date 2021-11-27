using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrainV2 : MonoBehaviour
{
    [SerializeField]
    Transform viewer;

    [SerializeField]
    GameObject terrainSectionPrefab;
    public static GameObject sectionPrefab;

    public const int chunkSideLength = 120;
    public const int chunkVtxPerSide = chunkSideLength + 1;

    public const int sectionSideLength = 24;
    public const int sectionVtxPerSide = sectionSideLength + 1;

    Dictionary<Vector2, TerrainChunk> terrainChunks;
    List<TerrainChunk> terrainChunksViewableLastUpdate;

    public static Vector2 viewerPosition;
    Vector2 oldViewerPosition_viewing, oldViewerPosition_colliding;
    public const float viewerMoveThreshForViewableUpdate = 25f;
    public const float viewerMoveThreshForViewableUpdate_sq = viewerMoveThreshForViewableUpdate * viewerMoveThreshForViewableUpdate;
    public const float viewerMoveThreshForCollidingUpdate = 25f;
    public const float viewerMoveThreshForCollidingUpdate_sq = viewerMoveThreshForCollidingUpdate * viewerMoveThreshForCollidingUpdate;
    public const float colliderRequestDistance = 100f;
    public const float colliderRequestDistance_sq = colliderRequestDistance * colliderRequestDistance;
    public const float colliderEnableDistance = 50f;
    public const float colliderEnableDistance_sq = colliderEnableDistance * colliderEnableDistance;

    public const float chunkCreateDistance = 1000f;
    public const float chunkCreateDistance_sq = chunkCreateDistance * chunkCreateDistance;

    [SerializeField]
    LODThreshInfo[] lodInfos;
    public static LODThreshInfo[] lodInfos_static;

    [SerializeField]
    int colliderLOD;
    public static int colliderLOD_static;

    public static float maxViewableDistance;
    public static float maxViewableDistance_sq;

    public static TerrainGeneratorV2 terrainGenerator;

    void OnValidate()
    {
        lodInfos_static = lodInfos;
        colliderLOD_static = colliderLOD;
        maxViewableDistance = lodInfos[lodInfos.Length - 1].distThresh;

        float twoRootTwo = Mathf.Sqrt(2) / 2f;
        if (chunkCreateDistance < twoRootTwo * (float)chunkSideLength + maxViewableDistance)
        {
            Debug.LogAssertion("No guarantee that a chunk is created before it's viewed!");
        }
        if (maxViewableDistance < twoRootTwo * (float)chunkSideLength)
        {
            Debug.LogAssertion("Chunks aren't viewable when entered from the corner");
        }
        if (colliderEnableDistance < twoRootTwo * sectionSideLength)
        {
            Debug.LogAssertion("Can enter a section before it's collider is enabled");
        }
        if (colliderEnableDistance > colliderRequestDistance)
        {
#pragma warning disable CS0162
            Debug.LogAssertion("Requesting colliders too late");
#pragma warning restore CS0162
        }

        for (int i = 0; i < lodInfos.Length - 1; i++)
        {
            if (lodInfos[i].lod >= lodInfos[i + 1].lod)
            {
                Debug.LogAssertion("LODs are out of order");
            }
            if (lodInfos[i].distThresh >= lodInfos[i + 1].distThresh)
            {
                Debug.LogAssertion("LOD distances are out of order");
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        maxViewableDistance = lodInfos[lodInfos.Length - 1].distThresh;
        maxViewableDistance_sq = maxViewableDistance * maxViewableDistance;
        oldViewerPosition_colliding = new Vector2(viewer.position.x, viewer.position.z);
        oldViewerPosition_viewing = new Vector2(viewer.position.x, viewer.position.z);

        terrainGenerator = FindObjectOfType<TerrainGeneratorV2>();

        UpdateViewableChunks();
        UpdateColliders();
    }

    // Update is called once per frame
    void Update()
    {
        //terrain chunks contain info about large map portions, including biome map and height map
        //When putting features like trees in the map, chunks will also have info for those so mesh can be generated
        //terrain sections are smaller, and contain various levels of detail meshes

        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        //if player has moved
        //first, for nearest chunks, ask if sections that have collider active should be updated, if so update them
        if ((viewerPosition - oldViewerPosition_colliding).sqrMagnitude > viewerMoveThreshForCollidingUpdate_sq)
        {
            oldViewerPosition_colliding = viewerPosition;
            UpdateColliders();
        }
        if ((viewerPosition - oldViewerPosition_viewing).sqrMagnitude > viewerMoveThreshForViewableUpdate_sq)
        {
            oldViewerPosition_viewing = viewerPosition;
            UpdateViewableChunks();
        }
        //then, update which chunks have a visible section
        //Then, for each section in those chunks, check if should be visible and at what LOD
        //also check for those sections if they've passed a threshold for requesting the mesh be built, and/or baked
    }

    void UpdateColliders()
    {
        Vector2 currentChunkCoord = new Vector2(Mathf.RoundToInt(viewerPosition.x / chunkSideLength), Mathf.RoundToInt(viewerPosition.y / chunkSideLength));
        for (int xo = -1; xo <= 1; xo++)
        {
            for (int yo = -1; yo <= 1; yo++)
            {
                TerrainChunk terrainChunk = terrainChunks[currentChunkCoord];
                //terrainChunk should always exist since chunk spawn distance should be greater than sqrt(2)/2*chunksize

                terrainChunk.UpdateEnabledColliders();
            }
        }
    }

    void UpdateViewableChunks()
    {
        Vector2 currentChunkCoord = new Vector2(Mathf.RoundToInt(viewerPosition.x / chunkSideLength), Mathf.RoundToInt(viewerPosition.y / chunkSideLength));
        int chunkCoordCheckDist = Mathf.CeilToInt(chunkCreateDistance / chunkSideLength);
        for (int xo = -chunkCoordCheckDist; xo <= chunkCoordCheckDist; xo++)
        {
            for (int yo = -chunkCoordCheckDist; yo <= chunkCoordCheckDist; yo++)
            {
                Vector2 offset = new Vector2(xo, yo);
                if (offset.sqrMagnitude > chunkCreateDistance_sq)
                    continue;

                Vector2 checkCoord = currentChunkCoord + offset;
                if (terrainChunks.ContainsKey(checkCoord))
                {
                    TerrainChunk terrainChunk = terrainChunks[checkCoord];
                    terrainChunk.UpdateViewableSections();
                    if (terrainChunk.anyViewable)
                    {
                        terrainChunksViewableLastUpdate.Add(terrainChunk);
                    }

                }
                else
                {
                    terrainChunks[checkCoord] = new TerrainChunk(checkCoord, transform);
                }
            }
        }
    }

    [System.Serializable]
    public struct LODThreshInfo
    {
        public int lod;
        public float distThresh;
    }

}



public class TerrainChunk
{
    private TerrainSection[,] sections;
    private List<TerrainSection> sectionsWithEnabledColliders = new List<TerrainSection>();
    private List<TerrainSection> sectionsViewable = new List<TerrainSection>();

    public Vector2 centerLocation { get; }

    public Bounds bounds { get; }

    public bool anyViewable { get; private set; }

    public TerrainChunk(Vector2 coord, Transform parent)
    {
        centerLocation = coord * EndlessTerrainV2.chunkSideLength;
        bounds = new Bounds(new Vector3(centerLocation.x, 0f, centerLocation.y),
                            new Vector3(EndlessTerrainV2.chunkSideLength, 0f, EndlessTerrainV2.chunkSideLength));

        int sectionsPerSide = EndlessTerrainV2.chunkSideLength / EndlessTerrainV2.sectionSideLength;
        sections = new TerrainSection[sectionsPerSide, sectionsPerSide];

        for (int x = 0; x < sectionsPerSide; x++)
        {
            for (int y = 0; y < sectionsPerSide; y++)
            {
                sections[x, y] = new TerrainSection(coord, new Vector2(x, y), parent);
            }
        }

        EndlessTerrainV2.terrainGenerator.RequestNewChunkData(OnNewChunkDataReceived, centerLocation, EndlessTerrainV2.chunkSideLength);
    }

    private void OnNewChunkDataReceived(TerrainChunkData terrainChunkData)
    {
        foreach (TerrainSection terrainSection in sections)
        {
            terrainSection.terrainChunkData = terrainChunkData;
            terrainSection.terrainChunkDataAvailable = true;
        }
    }


    public void UpdateEnabledColliders()
    {
        Vector3 vp3 = new Vector3(EndlessTerrainV2.viewerPosition.x, 0f, EndlessTerrainV2.viewerPosition.y);
        if (bounds.SqrDistance(vp3) > EndlessTerrainV2.colliderRequestDistance_sq)
        {
            foreach (TerrainSection terrainSection in sectionsWithEnabledColliders)
                terrainSection.DisableCollider();

            sectionsWithEnabledColliders.Clear();
            return;
        }

        sectionsWithEnabledColliders.Clear();
        foreach (TerrainSection terrainSection in sections)
        {
            float sd = (terrainSection.centerLocation - EndlessTerrainV2.viewerPosition).sqrMagnitude;
            if (sd < EndlessTerrainV2.colliderEnableDistance_sq)
            {
                terrainSection.EnableCollider();
                sectionsWithEnabledColliders.Add(terrainSection);
            }
            else if (sd < EndlessTerrainV2.colliderRequestDistance_sq)
            {
                terrainSection.RequestCollider();
            }
            else
            {
                terrainSection.DisableCollider();
            }
        }
    }


    public void UpdateViewableSections()
    {
        Vector3 vp3 = new Vector3(EndlessTerrainV2.viewerPosition.x, 0f, EndlessTerrainV2.viewerPosition.y);
        if (bounds.SqrDistance(vp3) > EndlessTerrainV2.maxViewableDistance_sq)
        {
            foreach (TerrainSection terrainSection in sectionsViewable)
                terrainSection.SetInvisible();

            sectionsViewable.Clear();
            anyViewable = false;
            return;
        }

        anyViewable = false;
        sectionsViewable.Clear();
        foreach (TerrainSection terrainSection in sections)
        {
            float sd = (terrainSection.centerLocation - EndlessTerrainV2.viewerPosition).sqrMagnitude;
            if (sd >= EndlessTerrainV2.maxViewableDistance_sq)
            {
                terrainSection.SetInvisible();
                continue;
            }
            anyViewable = true;

            int lodi = -1;
            for (int i = 0; i < EndlessTerrainV2.lodInfos_static.Length; i++)
            {
                if (EndlessTerrainV2.lodInfos_static[i].distThresh * EndlessTerrainV2.lodInfos_static[i].distThresh > sd)
                {
                    lodi = i;
                    break;
                }
            }
            //lod should never be -1 at this point!
            terrainSection.SetViewable(lodi);
        }
    }


    public class TerrainSection
    {
        private bool waitingOnHeightMap = true;
        private int currentLOD = TerrainGeneratorV2.LOD_MAX;
        private bool _heightMapAvailable = false;
        public bool terrainChunkDataAvailable
        {
            get => _heightMapAvailable;
            set
            {
                _heightMapAvailable = value;
                if (waitingOnHeightMap)
                    RequestMesh(currentLOD);
            }
        }
        private bool viewable = false;

        public Vector2 centerLocation;

        private GameObject meshObject;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;
        private MeshFilter meshFilter;
        private Dictionary<int, Mesh> meshes;

        public TerrainChunkData terrainChunkData;

        private Dictionary<int, bool> hasMeshLOD = new Dictionary<int, bool>();
        private Dictionary<int, bool> requestedMeshLOD = new Dictionary<int, bool>();
        private bool hasColliderBakedMesh = false;
        private bool requestedColliderMesh => requestedMeshLOD[EndlessTerrainV2.colliderLOD_static];
        private bool waitingOnColliderMesh = false;

        private Vector2 sectionCoord;

        public TerrainSection(Vector2 chunkCoord, Vector2 sectionCoord, Transform parent)
        {
            this.centerLocation = (chunkCoord - 0.5f * Vector2.one) * EndlessTerrainV2.chunkSideLength //bottom left of chunk
                                    + (sectionCoord + 0.5f * Vector2.one) * EndlessTerrainV2.sectionSideLength; //middle of this section
            Vector3 positionV3 = new Vector3(this.centerLocation.x, 0f, this.centerLocation.y);
            this.sectionCoord = sectionCoord;

            meshObject = GameObject.Instantiate(EndlessTerrainV2.sectionPrefab, positionV3, Quaternion.identity);
            meshObject.SetActive(false);
            meshObject.name = "Section " + chunkCoord + ", " + sectionCoord;
            meshRenderer = meshObject.GetComponent<MeshRenderer>();
            meshFilter = meshObject.GetComponent<MeshFilter>();
            meshCollider = meshObject.GetComponent<MeshCollider>();
            meshCollider.enabled = false;
            meshObject.transform.position = positionV3;
            meshObject.transform.SetParent(parent, false);
        }

        public void DisableCollider()
        {
            waitingOnColliderMesh = false;
            meshCollider.enabled = false;
        }
        public void EnableCollider()
        {
            if (hasColliderBakedMesh)
            {
                meshCollider.enabled = true;
            }
            else
            {
                waitingOnColliderMesh = true;
                if (!requestedColliderMesh)
                {
                    RequestCollider();
                }
            }
        }
        public void RequestCollider()
        {
            RequestMesh(EndlessTerrainV2.colliderLOD_static);
        }

        public void SetInvisible()
        {
            viewable = false;
            meshObject.SetActive(false);
        }
        public void SetViewable(int lodInfoIndex)
        {
            viewable = true;
            int lod = EndlessTerrainV2.lodInfos_static[lodInfoIndex].lod;
            currentLOD = lod;
            if (hasMeshLOD[lod])
            {
                meshObject.SetActive(true);
                meshFilter.mesh = meshes[lod];
            }
            else if (!requestedMeshLOD[lod])
            {
                RequestMesh(lod);
            }

            if (lodInfoIndex > 0 && !requestedMeshLOD[EndlessTerrainV2.lodInfos_static[lodInfoIndex - 1].lod])
            {
                RequestMesh(EndlessTerrainV2.lodInfos_static[lodInfoIndex - 1].lod);
            }
        }

        private void RequestMesh(int lod)
        {
            if (!terrainChunkDataAvailable)
            {
                waitingOnHeightMap = true;
                return;
            }

            if (hasMeshLOD[lod])
            {
                return;
            }
            else if (!requestedMeshLOD[lod])
            {
                requestedMeshLOD[lod] = true;
                EndlessTerrainV2.terrainGenerator.RequestSectionMesh(OnMeshDataReceived, terrainChunkData, sectionCoord, EndlessTerrainV2.sectionSideLength, lod);
            }

        }

        private void OnMeshDataReceived(TerrainSectionMeshData terrainSectionMeshData)
        {
            meshes[terrainSectionMeshData.LOD] = terrainSectionMeshData.CreateMesh();
            if (terrainSectionMeshData.LOD == currentLOD)
            {
                meshFilter.mesh = meshes[terrainSectionMeshData.LOD];
                if (viewable)
                {
                    meshObject.SetActive(true);
                }
            }
            if (terrainSectionMeshData.LOD == EndlessTerrainV2.colliderLOD_static)
            {
                EndlessTerrainV2.terrainGenerator.RequestSectionMeshBake(OnMeshBakeReceived, meshes[terrainSectionMeshData.LOD].GetInstanceID());
            }
        }

        private void OnMeshBakeReceived(TerrainSectionMeshBakeData terrainSectionMeshBakeData)
        {
            meshCollider.sharedMesh = meshes[EndlessTerrainV2.colliderLOD_static];
            hasColliderBakedMesh = true;
            if (waitingOnColliderMesh)
                meshCollider.enabled = true;
        }
    }


}