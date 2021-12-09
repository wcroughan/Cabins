using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cabins/Parachute Stats")]
public class ParachuteStats : ScriptableObject
{
    public float jumpForce;
    public float jumpLandDistance;
    public float spikeDrag;
    public float glideDrag;
    public float movementSpeed;
    public float preferredCameraFollowDistance;
    public float preferredCameraPivotHeight;
    public float cameraVerticalAngle;
}
