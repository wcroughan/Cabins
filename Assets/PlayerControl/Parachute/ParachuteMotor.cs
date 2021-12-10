using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(Animator))]
public class ParachuteMotor : MonoBehaviour
{
    [SerializeField]
    ParachuteStats stats;
    [SerializeField]
    float glideRotationAmount;
    [SerializeField]
    float rotationSpeed;

    Rigidbody rb;
    Animator animator;
    CapsuleCollider capsuleCollider;

    private bool shouldJump = false;
    private bool isJumping = false;
    private bool hitAThing = false;
    private bool isGliding = false;
    private bool shouldGlide = false;
    private bool shouldSpike = false;
    private bool glideModeEnabled = true;

    private int landTrigger;
    private int spikeTrigger;
    private int glideTrigger;
    private int jumpTrigger;
    private int hitAThingBool;
    private RaycastHit[] raycastResults;
    private int numRayCastResults = 4;

    private Quaternion targetRotation;
    private Vector2 movementVector;

    public bool IsInTheAir { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        raycastResults = new RaycastHit[numRayCastResults];
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        targetRotation = Quaternion.identity;
        movementVector = Vector2.zero;

        landTrigger = Animator.StringToHash("LandTrigger");
        spikeTrigger = Animator.StringToHash("FallToJumpTrigger");
        glideTrigger = Animator.StringToHash("JumpToFallTrigger");
        jumpTrigger = Animator.StringToHash("JumpTrigger");
        hitAThingBool = Animator.StringToHash("ShouldEat");
    }

    public void Jump()
    {
        if (!isJumping)
            shouldJump = true;
    }

    public void Move(Vector2 moveVec)
    {
        movementVector = moveVec;
    }


    void FixedUpdate()
    {
        if (shouldJump)
        {
            Debug.Log("Jumping");
            shouldJump = false;
            rb.AddForce(stats.jumpForce * Vector3.up, ForceMode.Force);
            isJumping = true;
            animator.SetTrigger(jumpTrigger);
        }
        else if (isJumping)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed);

            int numHit = Physics.SphereCastNonAlloc(transform.position, capsuleCollider.radius, Vector3.down, raycastResults, stats.jumpLandDistance);
            if (numHit > 0)
            {
                //landed or hit something
                for (int i = 0; i < numHit; i++)
                {
                    Debug.Log($"hit a thing: {raycastResults[i].transform.gameObject.name}");
                    RaycastHit hit = raycastResults[i];
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Terrain") && rb.velocity.y < 0)
                    {
                        animator.SetTrigger(landTrigger);
                        isJumping = false;
                        isGliding = false;
                        animator.SetBool(hitAThingBool, hitAThing);
                        glideModeEnabled = true;
                        transform.rotation = Quaternion.identity;
                    }
                    else if (hit.collider.gameObject != gameObject)
                    {
                        //hit a thing
                        if (!isGliding)
                        {
                            if (isJumping) // have to check this in case also hit the ground
                                glideModeEnabled = false;
                            // hit.transform.SetParent(transform);
                            hitAThing = true;
                        }
                    }
                }
            }
            else
            {
                Debug.Log("No hits");
                //is still falling
                if (isGliding && shouldSpike)
                {
                    shouldSpike = false;
                    isGliding = false;
                    animator.SetTrigger(spikeTrigger);
                    // rb.drag = stats.spikeDrag;
                }
                else if (!isGliding && shouldGlide && glideModeEnabled)
                {
                    shouldGlide = false;
                    isGliding = true;
                    animator.SetTrigger(glideTrigger);
                    // rb.drag = stats.glideDrag;
                }
            }

            if (isGliding)
            {
                targetRotation = Quaternion.Euler(movementVector.y * glideRotationAmount, 0f, movementVector.x * glideRotationAmount);
                // rb.AddForce((new Vector3(movementVector.x, 0f, movementVector.y)) * stats.movementSpeed, ForceMode.Force);
            }
        }
    }
}
