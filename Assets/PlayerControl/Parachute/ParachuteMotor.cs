using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(Animator))]
public class ParachuteMotor : MonoBehaviour
{
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

    public bool IsInTheAir { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        landTrigger = Animator.StringToHash()
    }

    void FixedUpdate()
    {
        if (shouldJump)
        {
            shouldJump = false;
            rb.AddForce(stats.jumpForce * Vector3.up, ForceMode.Force);
            isJumping = true;
            animator.SetTrigger(jumpTrigger);
        }
        else if (isJumping)
        {
            int numHit = Physics.SphereCast(transform.position, capsuleCollider.radius, Vector3.down, out hitInfo, stats.jumpLandDistance);
            if (numHit > 0)
            {
                //landed or hit something
                if (thingWasTheGround)
                {
                    animator.SetTrigger(landTrigger);
                    isJumping = false;
                    animator.SetBool(hitAThingBool, hitAThing);
                }
                else
                {
                    //hit a thing
                    if (!isGliding)
                    {
                        disableGlideMode;
                        thing.transform.setParent(this);
                        hitAThing = true;
                    }
                }
            }
            else
            {
                //is still falling
                if (isGliding && shouldSpike)
                {
                    shouldSpike = false;
                    isGliding = false;
                    animator.SetTrigger(spikeTrigger);
                    rb.drag = stats.spikeDrag;
                }
                else if (!isGliding && shouldGlide)
                {
                    shouldGlide = false;
                    isGliding = true;
                    animator.SetTrigger(glideTrigger);
                    rb.drag = stats.glideDrag;
                }
            }
        }
    }
}
