using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainDisplayV2))]
public class TerrainDisplayV2Editor : Editor
{
    public override void OnInspectorGUI()
    {
        TerrainDisplayV2 terrainDisplay = (TerrainDisplayV2)target;
        bool remakeTerrain = false;
        if (DrawDefaultInspector())
        {
            if (terrainDisplay.autoUpdate)
            {
                remakeTerrain = true;
            }
        }

        if (GUILayout.Button("Generate"))
        {
            remakeTerrain = true;
        }

        if (remakeTerrain)
            terrainDisplay.RemakeTerrain();

        if (GUILayout.Button("Clear Children"))
        {
            terrainDisplay.ClearChildren();
        }
    }

}