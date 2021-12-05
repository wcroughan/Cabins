using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Cabins/SlugStats")]
public class SlugStats : ScriptableObject
{
    [SerializeField]
    public float targetSearchRadius;
    [SerializeField]
    public float targetMaxAngle;

}
