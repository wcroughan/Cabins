using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainDisplayV2 : MonoBehaviour
{
    [SerializeField]
    GameObject sectionPrefab;
    [SerializeField]
    public bool autoUpdate = true;
    [SerializeField]
    Vector2 coord;
    [SerializeField, Range(TerrainGeneratorV2.LOD_MIN, TerrainGeneratorV2.LOD_MAX)]
    int levelOfDetail;
    [SerializeField, Range(1, 4)]
    int sectionSize;
    [SerializeField]
    int numChunksPerSide;
    [SerializeField, Range(0, 4)]
    int chunkSectionSubdivisions;

    private TerrainGeneratorV2 terrainGenerator;
    private int chunkSize;
    private int numSectionsPerDim;


    // Start is called before the first frame update
    void Update()
    {
        numSectionsPerDim = Mathf.RoundToInt(Mathf.Pow(2f, chunkSectionSubdivisions));
        chunkSize = numSectionsPerDim * 24 * sectionSize;
        terrainGenerator = FindObjectOfType<TerrainGeneratorV2>();
    }

    public void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject go = transform.GetChild(i).gameObject;
            if (go.CompareTag("TerrainDisplay"))
                DestroyImmediate(go);

        }
        foreach (Transform t in transform)
        {
        }
    }

    public void RemakeTerrain()
    {
        ClearChildren();
        for (int x = 0; x < numChunksPerSide; x++)
        {
            for (int y = 0; y < numChunksPerSide; y++)
            {

                terrainGenerator.RequestNewChunkData(OnNewChunkDataReceived, coord + new Vector2(x, y) * chunkSize, chunkSize);
            }
        }
    }

    private void OnNewChunkDataReceived(TerrainChunkData terrainChunkData)
    {
        for (int x = 0; x < numSectionsPerDim; x++)
        {
            for (int y = 0; y < numSectionsPerDim; y++)
            {
                terrainGenerator.RequestSectionMesh(tsmd => OnTerrainSectionMeshReceived(tsmd, terrainChunkData), terrainChunkData, new Vector2(x, y), chunkSize / numSectionsPerDim, levelOfDetail);
            }
        }
    }

    private void OnTerrainSectionMeshReceived(TerrainSectionMeshData terrainSectionMeshData, TerrainChunkData terrainChunkData)
    {
        GameObject g = Instantiate(sectionPrefab, transform.position, Quaternion.identity);
        g.tag = "TerrainDisplay";
        g.transform.SetParent(transform, true);
        g.GetComponent<MeshFilter>().mesh = terrainSectionMeshData.CreateMesh();
        MeshRenderer meshRenderer = g.GetComponent<MeshRenderer>();
        // meshRenderer.sharedMaterial.mainTexture = terrainChunkData.GetTexture();
        Material sm = new Material(meshRenderer.sharedMaterial);
        sm.mainTexture = terrainChunkData.GetTexture();
        meshRenderer.sharedMaterial = sm;
    }

}
