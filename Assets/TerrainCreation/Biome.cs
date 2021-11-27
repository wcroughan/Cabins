using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Biomes/Biome")]
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
    public virtual int heightMapFillOrder => 1;

    public Color GetColorForHeight(float h)
    {
        return gradient.Evaluate(h / heightMultiplier);
    }

    // Fill in heightMap[x,y,idx] with height values wherever mask[x,y] is true.
    // May also fill in where mask is false, but not guaranteed
    // Will not modify any values heightMap[x,y,i] for i!=idx
    // heightMap[x,y,idx] may depend on values heightMap[x,y,i] for i < idx, but will never read the values for i > idx
    public virtual void PopulateHeightMap(float[,,] heightMap, bool[,] mask, int idx, Vector2 center)
    {
        AnimationCurve heightCurve_ThreadSafe = new AnimationCurve(heightCurve.keys);

        System.Random rngesus = new System.Random(TerrainGeneratorV2.randomSeed);
        Vector2[] noiseSampleOffsets = new Vector2[numNoiseScales];
        for (int i = 0; i < numNoiseScales; i++)
        {
            float x = (float)rngesus.NextDouble() * TerrainGeneratorV2.randomOffsetRange;
            float y = (float)rngesus.NextDouble() * TerrainGeneratorV2.randomOffsetRange;
            noiseSampleOffsets[i] = new Vector2(x, y);
        }

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2f;
        float topLeftY = (height - 1) / -2f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 samplePoint = new Vector2(topLeftX + x, topLeftY + y) + center;
                float height01 = GetHeightMapSample(samplePoint, noiseSampleOffsets);
                heightMap[x, y, idx] = heightCurve_ThreadSafe.Evaluate(height01) * heightMultiplier;
            }
        }
    }

    private float GetHeightMapSample(Vector2 sample, Vector2[] noiseSampleOffsets)
    {
        float ret = 0;

        float scale = Mathf.Exp(baseNoiseScale);
        float falloffFactor = 1f;
        float denom = 0f;
        for (int s = 0; s < numNoiseScales; s++)
        {
            Vector2 scaleSample = (sample + noiseSampleOffsets[s]) * scale;
            ret += falloffFactor * Mathf.PerlinNoise(scaleSample.x, scaleSample.y);
            denom += falloffFactor;
            scale *= scaleScalingFactor;
            falloffFactor *= scaleFalloff;
        }

        ret /= denom;

        return Mathf.Clamp01(ret / denom);
    }
}
