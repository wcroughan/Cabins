using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpdatableTerrainInfo : ScriptableObject
{
    [SerializeField]
    bool autoUpdate;
    public event System.Action OnValuesUpdated;

    protected virtual void OnValidate()
    {
        if (autoUpdate)
            NotifyOfUpdatedValues();
    }

    public void NotifyOfUpdatedValues()
    {
        if (OnValuesUpdated != null)
            OnValuesUpdated();
    }


}
