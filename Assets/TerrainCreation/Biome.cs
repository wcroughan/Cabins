using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class Biome : UpdatableTerrainInfo
{
    [SerializeField, Range(1, 5)]
    public int numNoiseScales = 3;
    [SerializeField, Range(-8f, -1f)]
    public float baseNoiseScale = -5f;
    [SerializeField, Range(0f, 1f)]
    public float scaleFalloff = 0.5f;
    [SerializeField, Range(1f, 5f)]
    public float scaleScalingFactor = 2f;
    [SerializeField]
    public AnimationCurve heightCurve;
    [SerializeField]
    public float heightMultiplier = 10f;
    [SerializeField]
    public Gradient gradient;

    protected override void OnValidate()
    {
        base.OnValidate();
    }
}
