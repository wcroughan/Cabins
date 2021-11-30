using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    InputActions inputActions;
    Animator wonkAnimator;
    Animator planeAnimator;
    Animator slugAnimator;
    GameObject wonkObject;
    GameObject planeObject;
    GameObject slugObject;
    GameObject waterTintObject;

    private int planeSwitchTriggerID;
    private int idleAnimationTriggerID;
    private bool isUnderwater;

    Rigidbody rb;
    [SerializeField]
    float speed = 1f;
    [SerializeField]
    float jumpSpeed = 100f;
    [SerializeField]
    float planeModeVertSpeed = 10f;
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
    [SerializeField, Range(0f, 1f)]
    float planeIdleAnimationProbability = 0.1f;

    public bool planeModeEnabled;
    public bool shouldJump;
    public bool shouldCrouch;
    public bool sprintEnabled;




    void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new InputActions();

            inputActions.GameControl.Pause.performed += ctx => Debug.Break();

            planeModeEnabled = false;
            inputActions.WorldMovement.ToggleFlying.performed += ctx => PlaneModePressed();

            shouldCrouch = false;
            inputActions.WorldMovement.Crouch.performed += ctx => shouldCrouch = ctx.ReadValueAsButton();
            shouldJump = false;
            inputActions.WorldMovement.Jump.performed += ctx => shouldJump = ctx.ReadValueAsButton();
            // inputActions.WorldMovement.Jump.canceled += ctx => shouldJump = false;
            sprintEnabled = false;
            inputActions.WorldMovement.Sprint.performed += ctx => sprintEnabled = !sprintEnabled;

            inputActions.WorldMovement.Move.performed += ctx => userMovement = ctx.ReadValue<Vector2>();

            // inputActions.WorldMovement.SwitchMoveAnimations.performed += ctx => SwitchMoveAnimations();

            inputActions.Enable();
        }
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Start()
    {
        stoppedVelThresh_sq = stoppedVelThresh * stoppedVelThresh;
        // controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        rb = GetComponent<Rigidbody>();
        rb.velocity = new Vector3(0f, 0f, 0.1f);

        wonkObject = transform.Find("WonkPlayer").gameObject;
        planeObject = transform.Find("PlanePlayer").gameObject;
        slugObject = transform.Find("SlugPlayer").gameObject;
        waterTintObject = Camera.main.transform.Find("TintOverlay").gameObject;
        wonkAnimator = wonkObject.GetComponentInChildren<Animator>();
        planeAnimator = planeObject.GetComponentInChildren<Animator>();
        slugAnimator = slugObject.GetComponentInChildren<Animator>();
        planeSwitchTriggerID = Animator.StringToHash("SwitchMoveAnimations");
        // idleAnimationTriggerID = Animator.StringToHash("PlaneIdleVariationTrigger");
        idleAnimationTriggerID = Animator.StringToHash("IdleAnimation");

        isUnderwater = false;
        waterTintObject.SetActive(false);

        wonkObject.SetActive(true);
        planeObject.SetActive(false);
        slugObject.SetActive(false);
    }

    void SwitchMoveAnimations()
    {
        if (planeModeEnabled)
        {
            planeAnimator.SetTrigger(planeSwitchTriggerID);
            // Debug.Log("Triggered plane switch anims");
        }
    }

    void PlaneModePressed()
    {
        planeModeEnabled = !planeModeEnabled;
        shouldCrouch = false;
        if (planeModeEnabled)
        {
            rb.useGravity = false;
            wonkObject.SetActive(false);
            // planeObject.SetActive(true);
            slugObject.SetActive(true);
        }
        else
        {
            rb.useGravity = true;
            wonkObject.SetActive(true);
            // planeObject.SetActive(false);
            slugObject.SetActive(false);
        }
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

        if (planeModeEnabled)
        {
            vel.y = 0;
            vel.y += shouldJump ? planeModeVertSpeed : 0;
            vel.y += shouldCrouch ? -planeModeVertSpeed : 0;
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
        if (planeModeEnabled)
        {
            // planeAnimator.SetFloat("Speed", speedPct);
            slugAnimator.SetFloat("Speed", speedPct);
            // Debug.Log("setting plane anim speed to " + speedPct);
            if (Random.Range(0f, 1f) < planeIdleAnimationProbability * Time.deltaTime)
            {
                slugAnimator.SetTrigger(idleAnimationTriggerID);
                // planeAnimator.SetTrigger(idleAnimationTriggerID);
            }

            //note obvious bug here that this won't work if player starts underwater...
            if (transform.position.y < 0 && !isUnderwater)
            {
                isUnderwater = true;
                SwitchMoveAnimations();
                waterTintObject.SetActive(true);
            }
            else if (transform.position.y >= 0 && isUnderwater)
            {
                isUnderwater = false;
                SwitchMoveAnimations();
                waterTintObject.SetActive(false);
            }
        }
        else
            wonkAnimator.SetFloat("SpeedPct", speedPct);


    }
}
