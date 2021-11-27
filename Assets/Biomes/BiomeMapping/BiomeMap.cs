using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class BiomeMap : MonoBehaviour
{
    [SerializeField]
    int resolution;
    BiomeMapLocation[] locations;


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        locations = FindObjectsOfType<BiomeMapLocation>();
        UpdateMapTexture();
    }

    public void UpdateMapTexture()
    {
        Color[,] mapColors = new Color[resolution, resolution];

        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                float mindist = resolution * resolution * 2;
                float px = (float)x / resolution - 0.5f;
                float py = (float)y / resolution - 0.5f;
                Vector3 p = new Vector3(px, py, 0);
                for (int i = 0; i < locations.Length; i++)
                {
                    BiomeMapLocation l = locations[i];
                    Vector3 lp = l.transform.position;
                    float d = (p - lp).sqrMagnitude;
                    if (d < mindist && d < l.maxDist * l.maxDist)
                    {
                        mindist = d;
                        mapColors[x, y] = l.color;
                    }
                }
                // mapColors[x, y] = new Color(p.x, p.y, p.z);
            }
        }


        Color[] colorMap = Convert2DColormapTo1D(mapColors);

        Texture2D texture = new Texture2D(resolution, resolution);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.sharedMaterial.mainTexture = texture;
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

}
