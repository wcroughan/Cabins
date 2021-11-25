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

    enum GenFunc { Max, MaxWithSmooth, MaxEdges, Dilate };
    [SerializeField]
    GenFunc noiseGenFunc;
    [SerializeField, Range(0f, 0.25f)]
    float borderRadius = 0.1f;

    void OnEnable()
    {
        GenerateTexture();
    }

    public void GenerateTexture()
    {
        float[] channelHues = new float[numChannels];
        for (int i = 0; i < numChannels; i++)
            channelHues[i] = (float)i / (float)numChannels;

        float[,,] chVals = GetPerlinChannelMap();
        Color[,] maxValColors = GetMaxValColormap(chVals, channelHues);
        Color[,] maxValColorsSmooth = GetMaxWithSmoothToSecondColormap(chVals, channelHues);
        int pxlBorder = Mathf.CeilToInt(borderRadius * resolution);
        bool[,] borderMask = GetBorderMask(maxValColors, pxlBorder);
        Color[,] maxEdges = ApplyMask(maxValColors, borderMask);
        Color[,] dilatedMaxes = ChangeMaskedToNearest(maxValColors, borderMask);

        Color[] colorMap = null;
        if (noiseGenFunc == GenFunc.Max)
            colorMap = Convert2DColormapTo1D(maxValColors);
        else if (noiseGenFunc == GenFunc.MaxWithSmooth)
            colorMap = Convert2DColormapTo1D(maxValColorsSmooth);
        else if (noiseGenFunc == GenFunc.MaxEdges)
            colorMap = Convert2DColormapTo1D(maxEdges);
        else if (noiseGenFunc == GenFunc.Dilate)
            colorMap = Convert2DColormapTo1D(dilatedMaxes);

        Texture2D texture = new Texture2D(resolution, resolution);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.sharedMaterial.mainTexture = texture;
    }

    //Note here nearest is defined in terms of city block distance
    Color[,] ChangeMaskedToNearest(Color[,] colors, bool[,] mask)
    {
        int width = colors.GetLength(0);
        int height = colors.GetLength(1);
        Color[,] ret = new Color[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!mask[x, y])
                {
                    ret[x, y] = colors[x, y];
                }
                else
                {
                    int d = 1;
                    float minDist = width * height;
                    bool foundColor = false;
                    while (!foundColor)
                    {
                        for (int xo = -d; xo <= d; xo++)
                        {
                            int xx = x + xo;
                            int yy = y + d;
                            if (xx >= 0 && xx < width && yy >= 0 && yy < height && !mask[xx, yy])
                            {
                                foundColor = true;
                                float dist = (new Vector2(xo, d)).magnitude;
                                if (minDist > dist)
                                {
                                    minDist = dist;
                                    ret[x, y] = colors[xx, yy];
                                }
                            }

                            yy = y - d;
                            if (xx >= 0 && xx < width && yy >= 0 && yy < height && !mask[xx, yy])
                            {
                                foundColor = true;
                                float dist = (new Vector2(xo, d)).magnitude;
                                if (minDist > dist)
                                {
                                    minDist = dist;
                                    ret[x, y] = colors[xx, yy];
                                }
                            }
                        }

                        for (int yo = -d; yo <= d; yo++)
                        {
                            int xx = x + d;
                            int yy = y + yo;
                            if (xx >= 0 && xx < width && yy >= 0 && yy < height && !mask[xx, yy])
                            {
                                foundColor = true;
                                float dist = (new Vector2(d, yo)).magnitude;
                                if (minDist > dist)
                                {
                                    minDist = dist;
                                    ret[x, y] = colors[xx, yy];
                                }
                            }

                            xx = x - d;
                            if (xx >= 0 && xx < width && yy >= 0 && yy < height && !mask[xx, yy])
                            {
                                foundColor = true;
                                float dist = (new Vector2(d, yo)).magnitude;
                                if (minDist > dist)
                                {
                                    minDist = dist;
                                    ret[x, y] = colors[xx, yy];
                                }
                            }
                        }


                        d += 1;
                    }

                }

            }
        }

        return ret;
    }

    Color[,] ApplyMask(Color[,] colors, bool[,] mask)
    {
        Color[,] ret = new Color[colors.GetLength(0), colors.GetLength(1)];
        for (int x = 0; x < colors.GetLength(0); x++)
        {
            for (int y = 0; y < colors.GetLength(1); y++)
            {
                ret[x, y] = mask[x, y] ? colors[x, y] : Color.black;
            }
        }
        return ret;
    }

    bool[,] GetBorderMask(Color[,] colors, int radius)
    {
        bool[,] ret = new bool[colors.GetLength(0), colors.GetLength(1)];


        for (int x = 0; x < ret.GetLength(0); x++)
        {
            for (int y = 0; y < ret.GetLength(1); y++)
            {
                if (x < radius || x >= ret.GetLength(0) - radius - 1 || y < radius || y >= ret.GetLength(1) - radius - 1)
                {
                    ret[x, y] = true;
                    continue;
                }

                ret[x, y] = false;
                bool foundDiff = false;
                for (int xo = -radius; xo <= radius; xo++)
                {
                    for (int yo = -radius; yo <= radius; yo++)
                    {
                        if (colors[x, y] != colors[x + xo, y + yo])
                        {
                            ret[x, y] = true;
                            foundDiff = true;
                            break;
                        }
                    }
                    if (foundDiff)
                        break;
                }
            }
        }

        return ret;
    }

    Color[,] GetMaxValColormap(float[,,] chVals, float[] channelHues)
    {
        Color[,] colorMap = new Color[resolution, resolution];
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float maxval = -1f;
                int maxch = -1;

                for (int i = 0; i < numChannels; i++)
                {
                    float v = chVals[i, x, y];

                    if (maxval < v)
                    {
                        maxch = i;
                        maxval = v;
                    }
                }

                colorMap[x, y] = Color.HSVToRGB(channelHues[maxch], 1f, 1f);
            }
        }

        return colorMap;
    }

    Color[,] GetMaxWithSmoothToSecondColormap(float[,,] chVals, float[] channelHues)
    {
        Color[,] colorMap = new Color[resolution, resolution];
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float maxval = -1f;
                float maxval2 = -1f;
                float maxval3 = -1f;
                int maxch = -1;
                int maxch2 = -1;
                int maxch3 = -1;

                for (int i = 0; i < numChannels; i++)
                {
                    float v = chVals[i, x, y];

                    if (maxval < v)
                    {
                        maxch3 = maxch2;
                        maxch2 = maxch;
                        maxch = i;
                        maxval3 = maxval2;
                        maxval2 = maxval;
                        maxval = v;
                    }
                    else if (maxval2 < v)
                    {
                        maxch3 = maxch2;
                        maxch2 = i;
                        maxval3 = maxval2;
                        maxval2 = v;
                    }
                    else if (maxval3 < v)
                    {
                        maxch3 = i;
                        maxval3 = v;
                    }
                }

                Vector2 chv = new Vector2(maxval, maxval2) - Vector2.one * maxval3;
                chv.Normalize();
                if (chv.magnitude < 0.1f)
                    chv = Vector2.right;

                float lv = chv.x / (chv.x + chv.y);

                colorMap[x, y] = Color.Lerp(Color.HSVToRGB(channelHues[maxch2], 1f, 1f), Color.HSVToRGB(channelHues[maxch], 1f, 1f), lv);
            }
        }

        return colorMap;
    }


    Color[] Convert2DColormapTo1D(Color[,] colorMap)
    {
        Color[] ret = new Color[colorMap.GetLength(0) * colorMap.GetLength(1)];
        for (int x = 0; x < colorMap.GetLength(0); x++)
        {
            for (int y = 0; y < colorMap.GetLength(1); y++)
            {
                ret[x + y * colorMap.GetLength(0)] = colorMap[x, y];
            }
        }
        return ret;
    }

    float[,,] GetPerlinChannelMap()
    {
        float[,,] ret = new float[numChannels, resolution, resolution];

        int randomOffsetRange = 100000;
        System.Random rngesus = new System.Random(seed);
        Vector2[] channelOffsets = new Vector2[numChannels];
        for (int i = 0; i < numChannels; i++)
        {
            // float x = rngesus.Next(-randomOffsetRange, randomOffsetRange);
            float x = (float)rngesus.NextDouble() * (float)randomOffsetRange;
            float y = (float)rngesus.NextDouble() * (float)randomOffsetRange;
            channelOffsets[i] = new Vector2(x, y);
        }

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {

                float sampX = (float)x / (resolution - 1) * noiseScale;
                float sampY = (float)y / (resolution - 1) * noiseScale;

                for (int i = 0; i < numChannels; i++)
                    ret[i, x, y] = NoiseFunc(sampX + channelOffsets[i].x, sampY + channelOffsets[i].y);
            }
        }

        return ret;
    }


    float NoiseFunc(float sampX, float sampY)
    {
        return Mathf.Clamp01(Mathf.PerlinNoise(sampX, sampY));
    }
}
