using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cabins/Camera Follow Info")]
public class CameraFollowInfo : ScriptableObject
{
    public float lookHorizontalAngle;
    public float lookVerticalAngle;
    public float preferredCameraFollowDistance;
    public float preferredCameraPivotHeight;
    public float cameraFollowSpeed;
}
