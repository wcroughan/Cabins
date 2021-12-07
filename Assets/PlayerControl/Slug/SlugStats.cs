using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cabins/SlugStats")]
public class SlugStats : ScriptableObject
{
    public float targetSearchRadius;
    public float targetMaxAngle;
    public float preferredCameraFollowDistance;
    public float cameraVerticalAngle;

}
