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
        while (!EndlessTerrainV2.hasAnyTerrainCollider)
            yield return new WaitForSeconds(1);
        // Debug.Log(EndlessTerrainV2.hasAnyTerrainCollider);
        rb.useGravity = true;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
