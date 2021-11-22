using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]
public class TerrainGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TerrainGenerator terrainGenerator = (TerrainGenerator)target;
        // TerrainGenerator terrainGenerator = TerrainGenerator.Instance;

        List<TerrainDisplay> remakeChildren = new List<TerrainDisplay>();
        if (DrawDefaultInspector())
        {
            TerrainDisplay[] terrainDisplays = terrainGenerator.GetComponentsInChildren<TerrainDisplay>();
            foreach (TerrainDisplay td in terrainDisplays)
            {
                if (td.autoUpdate)
                {
                    remakeChildren.Add(td);
                }
            }
        }

        if (GUILayout.Button("Generate"))
        {
            remakeChildren = new List<TerrainDisplay>(terrainGenerator.GetComponentsInChildren<TerrainDisplay>());
        }

        foreach (TerrainDisplay td in remakeChildren)
        {
            td.InitTerrain();
        }
    }
}