using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    InputActions inputActions;
    Animator animator;

    Rigidbody rb;
    [SerializeField]
    float speed = 1f;
    [SerializeField]
    float jumpSpeed = 100f;
    [SerializeField]
    float godModeVertSpeed = 10f;
    [SerializeField]
    float sprintFactor = 1.5f;
    [SerializeField]
    Transform cam;
    [SerializeField]
    public Vector2 userMovement;
    [SerializeField]
    float maxTurnSpeed = 10;
    [SerializeField]
    float stoppedVelThresh;
    private float stoppedVelThresh_sq;

    public bool godModeEnabled;
    public bool shouldJump;
    public bool shouldCrouch;
    public bool sprintEnabled;




    void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new InputActions();

            inputActions.GameControl.Pause.performed += ctx => Debug.Break();

            godModeEnabled = false;
            inputActions.WorldMovement.ToggleFlying.performed += ctx => GodModePressed();

            shouldCrouch = false;
            inputActions.WorldMovement.Crouch.performed += ctx => shouldCrouch = ctx.ReadValueAsButton();
            shouldJump = false;
            inputActions.WorldMovement.Jump.performed += ctx => shouldJump = ctx.ReadValueAsButton();
            // inputActions.WorldMovement.Jump.canceled += ctx => shouldJump = false;
            sprintEnabled = false;
            inputActions.WorldMovement.Sprint.performed += ctx => sprintEnabled = !sprintEnabled;

            inputActions.WorldMovement.Move.performed += ctx => userMovement = ctx.ReadValue<Vector2>();

            inputActions.Enable();
        }
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void GodModePressed()
    {
        godModeEnabled = !godModeEnabled;
        rb.useGravity = !godModeEnabled;
        shouldCrouch = false;
    }

    void Start()
    {
        stoppedVelThresh_sq = stoppedVelThresh * stoppedVelThresh;
        // controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        rb = GetComponent<Rigidbody>();
        rb.velocity = new Vector3(0f, 0f, 0.1f);
        animator = GetComponentInChildren<Animator>();
    }


    void FixedUpdate()
    {
        Vector3 vel = rb.velocity;
        Vector3 camdirForward = cam.forward;
        camdirForward.y = 0;
        camdirForward.Normalize();
        Vector3 camdirRight = cam.right;
        camdirRight.y = 0;
        camdirRight.Normalize();
        Vector3 movementDir = camdirForward * userMovement.y + camdirRight * userMovement.x;
        vel += movementDir * speed * (sprintEnabled ? sprintFactor : 1f);

        if (godModeEnabled)
        {
            // Debug.Log("In god mode");
            vel.y = 0;
            vel.y += shouldJump ? godModeVertSpeed : 0;
            vel.y += shouldCrouch ? -godModeVertSpeed : 0;
        }
        else
        {
            if (shouldJump)
            {
                shouldJump = false;
                vel.y += jumpSpeed;
            }
        }

        rb.velocity = vel;

        if (rb.velocity.sqrMagnitude >= stoppedVelThresh_sq)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(camdirForward, Vector3.up), maxTurnSpeed);

        float currentSpeed = (new Vector2(vel.x, vel.z)).magnitude;
        float speedPct = Mathf.Clamp01(Mathf.InverseLerp(0, 70, currentSpeed));
        animator.SetFloat("SpeedPct", speedPct);
    }
}
