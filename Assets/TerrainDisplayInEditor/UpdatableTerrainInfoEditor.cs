using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(UpdatableTerrainInfo), true)]
public class UpdatableTerrainInfoEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        UpdatableTerrainInfo data = (UpdatableTerrainInfo)target;
        if (GUILayout.Button("Update"))
        {
            data.NotifyOfUpdatedValues();
        }
    }
}
