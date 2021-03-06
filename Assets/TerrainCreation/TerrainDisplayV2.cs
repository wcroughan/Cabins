using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor.SceneManagement;
using System.Diagnostics;

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
    [SerializeField]
    bool saveBiomeValsToFile = false;

    private TerrainGeneratorV2 terrainGenerator;
    private int chunkSize;
    private int numSectionsPerDim;

    private Stopwatch sw1, sw2;


    // Start is called before the first frame update
    void Update()
    {
    }

    public void ClearChildren()
    {
        bool sceneIsDirty = false;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject go = transform.GetChild(i).gameObject;
            if (go.CompareTag("TerrainDisplay"))
            {
                DestroyImmediate(go);
                sceneIsDirty = true;
            }

        }
        if (sceneIsDirty)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    public void RemakeTerrain()
    {
        numSectionsPerDim = Mathf.RoundToInt(Mathf.Pow(2f, chunkSectionSubdivisions));
        chunkSize = numSectionsPerDim * 24 * sectionSize;
        terrainGenerator = FindObjectOfType<TerrainGeneratorV2>();

        sw1 = new Stopwatch();
        sw2 = new Stopwatch();
        sw1.Reset();
        sw2.Reset();

        StreamWriter perlinValuesOut = null;
        if (saveBiomeValsToFile)
            perlinValuesOut = new StreamWriter("perlinValues.txt", false);

        ClearChildren();
        for (int x = 0; x < numChunksPerSide; x++)
        {
            for (int y = 0; y < numChunksPerSide; y++)
            {

                terrainGenerator.RequestNewChunkData(OnNewChunkDataReceived, coord + new Vector2(x, y) * chunkSize, chunkSize, startParallelTask: false, perlinValuesOut: perlinValuesOut);
            }
        }

        if (saveBiomeValsToFile)
            perlinValuesOut.Close();

        UnityEngine.Debug.Log("mesh creation time: " + sw1.Elapsed.TotalSeconds);
        UnityEngine.Debug.Log("texture creation time: " + sw2.Elapsed.TotalSeconds);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private void OnNewChunkDataReceived(TerrainChunkData terrainChunkData)
    {
        for (int x = 0; x < numSectionsPerDim; x++)
        {
            for (int y = 0; y < numSectionsPerDim; y++)
            {
                terrainGenerator.RequestSectionMesh(tsmd => OnTerrainSectionMeshReceived(tsmd, terrainChunkData), terrainChunkData, new Vector2(x, y), chunkSize / numSectionsPerDim, levelOfDetail, startParallelTask: false);
            }
        }
    }

    private void OnTerrainSectionMeshReceived(TerrainSectionMeshData terrainSectionMeshData, TerrainChunkData terrainChunkData)
    {
        GameObject g = Instantiate(sectionPrefab, transform.position, Quaternion.identity);
        g.tag = "TerrainDisplay";
        g.name = terrainChunkData.chunkCenter + "";
        g.transform.SetParent(transform, true);
        sw1.Start();
        g.GetComponent<MeshFilter>().mesh = terrainSectionMeshData.CreateMesh();
        sw1.Stop();
        MeshRenderer meshRenderer = g.GetComponent<MeshRenderer>();
        // meshRenderer.sharedMaterial.mainTexture = terrainChunkData.GetTexture();
        Material sm = new Material(meshRenderer.sharedMaterial);
        sw2.Start();
        sm.mainTexture = terrainChunkData.GetTexture();
        sw2.Stop();
        meshRenderer.sharedMaterial = sm;
    }

}
