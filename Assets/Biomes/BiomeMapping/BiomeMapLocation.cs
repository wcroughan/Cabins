using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BiomeMapLocation : MonoBehaviour
{
    public string biomeName;
    public Color color;
    [SerializeField, Range(0, 2)]
    public float maxDist = 1f;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawSphere(transform.position, 0.02f);
        Handles.Label(transform.position, biomeName);
    }
}
