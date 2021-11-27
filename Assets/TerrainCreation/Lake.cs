using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lake : Biome
{
    public override int heightMapFillOrder => 10;

    public override void PopulateHeightMap(float[,,] heightMap, bool[,] mask, int idx, Vector2 center)
    {
        base.PopulateHeightMap(heightMap, mask, idx, center);
    }
}
