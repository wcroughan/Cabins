using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NoiseTexture))]
public class NoiseTextureEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // base.OnInspectorGUI();

        NoiseTexture noiseTexture = (NoiseTexture)target;
        bool remakeTexture = false;
        if (DrawDefaultInspector())
        {
            if (noiseTexture.autoUpdate)
                remakeTexture = true;
        }

        if (GUILayout.Button("Remake"))
            remakeTexture = true;

        if (remakeTexture)
            noiseTexture.GenerateTexture();
    }
}
