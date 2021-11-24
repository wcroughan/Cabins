using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayGravity : MonoBehaviour
{
    Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        StartCoroutine(ActivateGravityAfterDelay());
    }
    IEnumerator ActivateGravityAfterDelay()
    {
        while (!EndlessTerrain.hasAnyTerrainCollider)
            yield return new WaitForSeconds(1);
        Debug.Log(EndlessTerrain.hasAnyTerrainCollider);
        rb.useGravity = true;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
