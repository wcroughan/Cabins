using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LilBallPlayerController : MonoBehaviour
{
    [SerializeField]
    LilBallStats stats;
    [SerializeField]
    CameraFollowInfo cameraFollowInfo;

    private Rigidbody rb;
    private Animator animator;
    private InputActions inputActions;
    private Vector2 userMovementInput;
    private Vector2 lookInput;
    private bool shouldJump;
    private bool isJumping;
    private bool isLanding;
    private bool jumpStarted;
    private float jumpTimer;
    private RaycastHit[] raycastResults;
    private const int numRaycastResults = 2;
    private int landedTriggerID;
    private int jumpingTriggerID;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        raycastResults = new RaycastHit[numRaycastResults];
        landedTriggerID = Animator.StringToHash("LandTrigger");
        jumpingTriggerID = Animator.StringToHash("JumpTrigger");
    }

    void OnEnable()
    {
        Debug.Log("The lil player ball is becoming active!");
        if (inputActions == null)
        {
            inputActions = new InputActions();
            inputActions.WorldMovement.Move.performed += ctx => userMovementInput = ctx.ReadValue<Vector2>();
            inputActions.WorldMovement.Jump.performed += ctx => shouldJump = ctx.ReadValueAsButton();
            inputActions.WorldMovement.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        }

        inputActions.WorldMovement.Move.Enable();
        inputActions.WorldMovement.Jump.Enable();
        inputActions.WorldMovement.Look.Enable();
        shouldJump = false;
        isJumping = false;
        isLanding = false;
        lookInput = Vector2.zero;
        userMovementInput = Vector2.zero;

        cameraFollowInfo.preferredCameraFollowDistance = stats.preferredCameraFollowDistance;
    }

    void OnDisable()
    {
        inputActions.WorldMovement.Move.Disable();
        inputActions.WorldMovement.Jump.Disable();
        inputActions.WorldMovement.Look.Disable();
    }

    void OnJumpAnimationJumpPointReached()
    {
        rb.AddForce(transform.up * stats.jumpSpeed, ForceMode.VelocityChange);
        jumpStarted = true;
        jumpTimer = 0;
    }

    void OnLandAnimationFinished()
    {
        isLanding = false;
    }

    void Update()
    {
        cameraFollowInfo.lookHorizontalAngle += lookInput.x * stats.cameraRotateSpeed;
        cameraFollowInfo.lookVerticalAngle -= lookInput.y * stats.cameraRotateSpeed;
        cameraFollowInfo.lookVerticalAngle = Mathf.Clamp(cameraFollowInfo.lookVerticalAngle, stats.minVertAngle, stats.maxVertAngle);
    }

    void FixedUpdate()
    {
        float horizAngleRads = -(cameraFollowInfo.lookHorizontalAngle - 90) / 180 * Mathf.PI;
        Vector3 lookDir = new Vector3(Mathf.Cos(horizAngleRads), 0f, Mathf.Sin(horizAngleRads));
        Vector3 lookRightDir = new Vector3(Mathf.Sin(horizAngleRads), 0f, -Mathf.Cos(horizAngleRads));
        Vector3 worldMoveDir = userMovementInput.x * lookRightDir + userMovementInput.y * lookDir;
        // Vector2 moveDir = userMovementInput.normalized;
        // Vector3 worldMoveDir = new Vector3(moveDir.x, 0f, moveDir.y);
        rb.AddForce(worldMoveDir * stats.speed, ForceMode.VelocityChange);

        if (shouldJump)
        {
            shouldJump = false;
            if (!isJumping && !isLanding)
            {
                //need to double check here in case we're falling without having jumped
                int numHits = Physics.SphereCastNonAlloc(transform.position, transform.localScale.x, Vector3.down, raycastResults, stats.maxJumpDistFromGround);
                if (numHits > 1)
                {
                    //we have something to jump off of
                    animator.SetTrigger(jumpingTriggerID);
                }
            }
        }
        else if (jumpStarted)
        {
            jumpTimer += Time.deltaTime;
            if (jumpTimer > stats.timeBeforeGroundMeasure)
            {
                isJumping = true;
                jumpStarted = false;
            }
        }
        else if (isJumping)
        {
            int numHits = Physics.SphereCastNonAlloc(transform.position, transform.localScale.x, Vector3.down, raycastResults, stats.maxJumpDistFromGround);
            if (numHits > 1)
            {
                isJumping = false;
                isLanding = true;
                animator.SetTrigger(landedTriggerID);
            }
        }
    }

}
