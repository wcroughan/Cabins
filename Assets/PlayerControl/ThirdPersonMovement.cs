using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    InputActions inputActions;

    Rigidbody rb;
    [SerializeField]
    float speed = 0.6f;
    [SerializeField]
    Transform cam;
    [SerializeField]
    bool alwaysMove = false;

    Vector2 movementDir;
    bool godModeEnabled;
    bool shouldJump;
    bool shouldCrouch;
    bool sprintEnabled;

    void OnEnable()
    {
        if (inputActions == null)
        {
            Debug.Log("Hi from enable!");
            inputActions = new InputActions();

            inputActions.GameControl.Pause.performed += ctx => Debug.Break();

            godModeEnabled = false;
            inputActions.WorldMovement.ToggleFlying.performed += ctx => godModeEnabled = !godModeEnabled;

            shouldCrouch = false;
            inputActions.WorldMovement.Crouch.performed += ctx => shouldCrouch = ctx.ReadValueAsButton();
            // inputActions.WorldMovement.Crouch.canceled += ctx => shouldCrouch = false;
            shouldJump = false;
            inputActions.WorldMovement.Jump.performed += ctx => shouldJump = ctx.ReadValueAsButton();
            // inputActions.WorldMovement.Jump.canceled += ctx => shouldJump = false;
            sprintEnabled = false;
            inputActions.WorldMovement.Sprint.performed += ctx => sprintEnabled = !sprintEnabled;

            inputActions.WorldMovement.Move.performed += ctx => movementDir = ctx.ReadValue<Vector2>();

            inputActions.Enable();
        }
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Start()
    {
        // controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        rb = GetComponent<Rigidbody>();
    }


    // Update is called once per frame
    void Update()
    {
        Vector3 vel = rb.velocity;
        vel.x += movementDir.x * speed;
        vel.z += movementDir.y * speed;
        // movementDir = Vector2.zero;

        if (godModeEnabled)
        {
            // Debug.Log("In god mode");
            vel.y = 0;
            vel.y += shouldJump ? speed : 0;
            vel.y += shouldCrouch ? -speed : 0;
        }
        else
        {
            if (shouldJump)
            {
                shouldJump = false;
                vel.y += speed;
            }
        }

        rb.velocity = vel;

        // Debug.Log(vel);
    }
}
