using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    Rigidbody rb;
    [SerializeField]
    float speed = 0.6f;
    [SerializeField]
    Transform cam;
    [SerializeField]
    bool alwaysMove = false;

    void Start()
    {
        // controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        rb = GetComponent<Rigidbody>();
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
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0, vertical).normalized;

        if (direction.magnitude >= 0.1f || alwaysMove)
        {
            if (alwaysMove && direction.magnitude < 0.1f)
                direction = Vector3.one;
            float moveAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            // controller.Move(direction * speed * Time.deltaTime);

            Vector3 forceDir = Quaternion.Euler(0f, moveAngle, 0f) * Vector3.forward;
            rb.AddForce(forceDir.normalized * speed, ForceMode.Force);
        }
    }
}
