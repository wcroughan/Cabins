using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseTexture : MonoBehaviour
{
    [SerializeField]
    int resolution = 1024;
    [SerializeField]
    float noiseScale = 1.0f;
    [SerializeField]
    public bool autoUpdate = true;
    [SerializeField]
    int seed;
    [SerializeField, Range(3, 20)]
    int numChannels = 6;
    [SerializeField]
    float ch1Offset = 0f, ch1Mult = 1f;
    // [SerializeField]
    // float ch1Offset2 = 0f, ch1Mult2 = 1f;

    void OnEnable()
    {
        GenerateTexture();
    }

    public void GenerateTexture()
    {
        // bool useSecondInput = gameObject.name.Equals("MyQuad");

        int randomOffsetRange = 100000;
        System.Random rngesus = new System.Random(seed);
        Vector2[] channelOffsets = new Vector2[numChannels];
        float[] channelHues = new float[numChannels];
        for (int i = 0; i < numChannels; i++)
        {
            // float x = rngesus.Next(-randomOffsetRange, randomOffsetRange);
            float x = (float)rngesus.NextDouble() * (float)randomOffsetRange;
            float y = (float)rngesus.NextDouble() * (float)randomOffsetRange;
            channelOffsets[i] = new Vector2(x, y);
            channelHues[i] = (float)i / (float)numChannels;
        }

        Color[] colorMap = new Color[resolution * resolution];
        float[] chVals = new float[numChannels];
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                int cmi = x + y * resolution;
                float sampX = (float)x / (resolution - 1) * noiseScale;
                float sampY = (float)y / (resolution - 1) * noiseScale;

                float maxval = -1f;
                float maxval2 = -1f;
                float maxval3 = -1f;
                int maxch = -1;
                int maxch2 = -1;
                int maxch3 = -1;
                for (int i = 0; i < numChannels; i++)
                {
                    chVals[i] = NoiseFunc(sampX + channelOffsets[i].x, sampY + channelOffsets[i].y);
                    if (i == 0)
                    {
                        // if (useSecondInput)
                        // chVals[i] = ch1Offset2 + chVals[i] * ch1Mult2;
                        // else
                        chVals[i] = ch1Offset + chVals[i] * ch1Mult;
                    }
                    if (maxval < chVals[i])
                    {
                        maxch3 = maxch2;
                        maxch2 = maxch;
                        maxch = i;
                        maxval3 = maxval2;
                        maxval2 = maxval;
                        maxval = chVals[i];
                    }
                    else if (maxval2 < chVals[i])
                    {
                        maxch3 = maxch2;
                        maxch2 = i;
                        maxval3 = maxval2;
                        maxval2 = chVals[i];
                    }
                    else if (maxval3 < chVals[i])
                    {
                        maxch3 = i;
                        maxval3 = chVals[i];
                    }
                }

                // Debug.Log(maxval + " > " + maxval2 + " > " + maxval3);
                Vector2 chv = new Vector2(maxval, maxval2) - Vector2.one * maxval3;
                chv.Normalize();
                if (chv.magnitude < 0.1f)
                    chv = Vector2.right;

                colorMap[cmi] = Color.Lerp(Color.HSVToRGB(channelHues[maxch2], 1f, 1f), Color.HSVToRGB(channelHues[maxch], 1f, 1f), chv.x * chv.x);
            }
        }

        Texture2D texture = new Texture2D(resolution, resolution);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.sharedMaterial.mainTexture = texture;
    }


    float NoiseFunc(float sampX, float sampY)
    {
        return Mathf.Clamp01(Mathf.PerlinNoise(sampX, sampY));
    }
}
