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
        StartCoroutine(ActivateGravityAfterDelay(3));
    }
    IEnumerator ActivateGravityAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        rb.useGravity = true;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
