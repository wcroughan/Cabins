using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cabins/CraneStats")]
public class CraneStats : ScriptableObject
{
    public float maxTargetDistSq;
    public float cameraVerticalAngle;
    public float preferredCameraFollowDistance;
    public float preferredCameraPivotHeight;
}
