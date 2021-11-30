using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class PlaneAnimationEventHandler : MonoBehaviour
{

    VisualEffect smokePuffsVFX;
    int generateSmokeEventID;

    void Start()
    {
        smokePuffsVFX = transform.parent.Find("PlaneSmokePuffs").GetComponent<VisualEffect>();
        generateSmokeEventID = Shader.PropertyToID("CreateSmokePuff");
    }

    public void CreateSmokePuff()
    {
        smokePuffsVFX.SendEvent(generateSmokeEventID);
    }
}
