using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrainV2 : MonoBehaviour
{
    [SerializeField]
    Transform viewer;

    public const int chunkSideLength = 120;
    public const int chunkVtxPerSide = chunkSideLength + 1;

    public const int sectionSideLength = 24;
    public const int sectionVtxPerSide = sectionSideLength + 1;

    Dictionary<Vector2, TerrainChunk> terrainChunks;
    List<TerrainChunk> terrainChunksViewableLastUpdate;

    Queue<(MeshCollider meshCollider, Mesh mesh)> bakedColliderMeshReturnQueue = new Queue<(MeshCollider meshCollider, Mesh mesh)>();

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
    LODThreshInfo[] lodInfos { get; }
    public static LODThreshInfo[] lodInfos_static;
    public static float maxViewableDistance;
    public static float maxViewableDistance_sq;

    void OnValidate()
    {
        lodInfos_static = lodInfos;
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
    }

    // Start is called before the first frame update
    void Start()
    {
        maxViewableDistance = lodInfos[lodInfos.Length - 1].distThresh;
        maxViewableDistance_sq = maxViewableDistance * maxViewableDistance;
        oldViewerPosition_colliding = new Vector2(viewer.position.x, viewer.position.z);
        oldViewerPosition_viewing = new Vector2(viewer.position.x, viewer.position.z);

        UpdateViewableChunks();
        UpdateColliders();
    }

    // Update is called once per frame
    void Update()
    {
        //terrain chunks contain info about large map portions, including biome map and height map
        //When putting features like trees in the map, chunks will also have info for those so mesh can be generated
        //terrain sections are smaller, and contain various levels of detail meshes

        //ON UPDATE:
        //first first, check queues for returned baked meshes and apply, then other queues also
        //!!!!!!!
        //Actually should just do this in main thread from terrain generator class
        for (int i = 0; i < bakedColliderMeshReturnQueue.Count; i++)
        {
            var bakedMesh = bakedColliderMeshReturnQueue.Dequeue();
            bakedMesh.meshCollider.sharedMesh = bakedMesh.mesh;
        }
        //OTHER queues?


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



        //FOR GENERATION:
        //when a new chunk is created, make biome and height map
        //when a new section mesh is created
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
                    terrainChunks[checkCoord] = new TerrainChunk(checkCoord, chunkSideLength);
                }
            }
        }
    }

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

    public TerrainChunk(Vector2 coord, float size)
    {
        centerLocation = coord * size;
    }


    public void UpdateEnabledColliders()
    {
        Vector3 vp3 = new Vector3(EndlessTerrainV2.viewerPosition.x, 0f, EndlessTerrainV2.viewerPosition.y);
        if (bounds.SqrDistance(vp3) > EndlessTerrainV2.colliderEnableDistance_sq)
        {
            foreach (TerrainSection terrainSection in sectionsWithEnabledColliders)
                terrainSection.DisableCollider();

            sectionsWithEnabledColliders.Clear();
            return;
        }

        sectionsWithEnabledColliders.Clear();
        foreach (TerrainSection terrainSection in sections)
        {
            if ((terrainSection.centerLocation - EndlessTerrainV2.viewerPosition).sqrMagnitude < EndlessTerrainV2.colliderEnableDistance_sq)
            {
                terrainSection.EnableCollider();
                sectionsWithEnabledColliders.Add(terrainSection);
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

            int lod = -1;
            for (int i = 0; i < EndlessTerrainV2.lodInfos_static.Length; i++)
            {
                if (EndlessTerrainV2.lodInfos_static[i].distThresh * EndlessTerrainV2.lodInfos_static[i].distThresh > sd)
                {
                    lod = EndlessTerrainV2.lodInfos_static[i].lod;
                    break;
                }
            }
            //lod should never be -1 at this point!
            terrainSection.SetViewable(lod);
        }
    }
}


public class TerrainSection
{
    public Vector2 centerLocation;

    private GameObject meshObject;

    public void DisableCollider() { }
    public void EnableCollider() { }
    public void SetInvisible() { }
    public void SetViewable(int lod) { }
}

