using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cabins/LilBallStats")]
public class LilBallStats : ScriptableObject
{
    public float speed;
    public float jumpSpeed;
    public float maxJumpAngle;
    public float maxJumpDistFromGround;
    public float timeBeforeGroundMeasure;

    public float cameraRotateSpeed;
    public float minVertAngle;
    public float maxVertAngle;
    public float preferredCameraFollowDistance;
}
