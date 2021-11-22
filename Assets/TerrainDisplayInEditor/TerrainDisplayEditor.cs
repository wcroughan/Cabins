using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainDisplay))]
public class TerrainDisplayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        TerrainDisplay terrainDisplay = (TerrainDisplay)target;
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
            terrainDisplay.InitTerrain();
    }
}
