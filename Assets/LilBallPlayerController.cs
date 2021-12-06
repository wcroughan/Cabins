using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LilBallPlayerController : MonoBehaviour
{
    [SerializeField]
    LilBallStats stats;

    private Rigidbody rb;
    private Animator animator;
    private InputActions inputActions;
    private Vector2 userMovementInput;
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
        }

        inputActions.WorldMovement.Move.Enable();
        inputActions.WorldMovement.Jump.Enable();
        shouldJump = false;
        isJumping = false;
        isLanding = false;
    }

    void OnDisable()
    {
        inputActions.WorldMovement.Move.Disable();
        inputActions.WorldMovement.Jump.Disable();
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

    void FixedUpdate()
    {
        Vector2 moveDir = userMovementInput.normalized;
        Vector3 worldMoveDir = new Vector3(moveDir.x, 0f, moveDir.y);
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
