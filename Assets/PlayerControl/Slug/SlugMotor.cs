using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//handles the physics and animation associated with performing actions as a slug
[RequireComponent(typeof(Rigidbody), typeof(Animator))]
public class SlugMotor : MonoBehaviour
{
    [SerializeField]
    SlugMotorStats stats;

    [SerializeField]
    bool performIdleAnimations;
    [SerializeField]
    int numIdleAnimations = 1;
    [SerializeField]
    float idleAnimationFrequency = 10f;
    private float idleAnimationProbability;
    private int idleAnimationTriggerID;
    private int idleAnimationIndexID;

    public enum SlugAction { TurnLeft, TurnRight, MoveForward, MoveBackward, Lunge };
    private SlugAction? nextAction;
    private bool waitingOnPreviousActionAnimation;

    [SerializeField]
    private float lungeSmoothVal = 1f;
    [SerializeField]
    private float destroyThreshold = 1;
    private Vector3 smoothDampVelocity = Vector3.zero;
    private Vector3 targetVelocity;
    private bool isLunging = false;
    private GameObject lungeTarget;

    private Animator animator;
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Dictionary<SlugAction?, int> animatorTriggerKeys;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        nextAction = null;
        waitingOnPreviousActionAnimation = false;
        animatorTriggerKeys = new Dictionary<SlugAction?, int>();
        animatorTriggerKeys[SlugAction.TurnLeft] = Animator.StringToHash("TurnLeftTrigger");
        animatorTriggerKeys[SlugAction.TurnRight] = Animator.StringToHash("TurnRightTrigger");
        animatorTriggerKeys[SlugAction.MoveForward] = Animator.StringToHash("MoveForwardTrigger");
        animatorTriggerKeys[SlugAction.MoveBackward] = Animator.StringToHash("MoveBackwardTrigger");
        animatorTriggerKeys[SlugAction.Lunge] = Animator.StringToHash("LungeTrigger");

        idleAnimationTriggerID = Animator.StringToHash("IdleAnimationTrigger");
        idleAnimationIndexID = Animator.StringToHash("IdleAnimationIndex");
        idleAnimationProbability = 1f / idleAnimationFrequency;
    }

    public void SetNextLungeTarget(GameObject target)
    {
        lungeTarget = target;
    }

    public void PerformAction(SlugAction action)
    {
        if (!waitingOnPreviousActionAnimation)
            nextAction = action;
    }

    void Update()
    {
        if (performIdleAnimations && Random.Range(0f, 1f) < idleAnimationProbability * Time.deltaTime)
        {
            animator.SetInteger(idleAnimationIndexID, Random.Range(0, numIdleAnimations));
            animator.SetTrigger(idleAnimationTriggerID);
        }
    }

    public void OnWalkAnimationFinished()
    {
        waitingOnPreviousActionAnimation = false;
    }

    public void OnTurnAnimationFinished()
    {
        waitingOnPreviousActionAnimation = false;
    }

    public void OnLungeAnimationFinished()
    {
        waitingOnPreviousActionAnimation = false;
        isLunging = false;
        rb.isKinematic = false;
        capsuleCollider.enabled = true;
    }

    void FixedUpdate()
    {
        //TODO:
        //when choosing target, run a capsulecast in physics to make sure can get there unobstructed. Maybe raycast for low bumps?
        // store target position before destroying object, or just stop updating lunging stuff once destroyed
        //Every other target is briefly approached and then canceled, maybe aproblem here or in controller script

        if (isLunging)
        {
            Vector3 vToTarget = lungeTarget.transform.position - transform.position;
            if (vToTarget.sqrMagnitude < destroyThreshold * destroyThreshold)
            {
                Destroy(lungeTarget);
            }
            targetVelocity = (vToTarget).normalized;
            // rb.AddForce(Vector3.SmoothDamp(rb.velocity, targetVelocity, ref smoothDampVelocity, lungeSmoothVal));
            // rb.velocity = Vector3.SmoothDamp(rb.velocity, targetVelocity, ref smoothDampVelocity, lungeSmoothVal);
            // rb.position = Vector3.Lerp(rb.position, lungeTarget.position, Time.deltaTime * lungeSmoothVal);
            // rb.MovePosition(Vector3.Lerp(rb.position, lungeTarget.position, Time.deltaTime * lungeSmoothVal));
            // rb.MovePosition(Vector3.SmoothDamp(rb.position, lungeTarget.position, ref smoothDampVelocity, lungeSmoothVal));
            rb.MovePosition(rb.position + targetVelocity * lungeSmoothVal * Time.deltaTime);

        }

        if (waitingOnPreviousActionAnimation)
        {
            return;
        }

        if (nextAction != null)
        {
            Debug.Log($"Slug doing {nextAction}");
            //animation
            animator.SetTrigger(animatorTriggerKeys[nextAction]);

            //if we're just moving, root node movement will take care of that
            if (nextAction == SlugAction.Lunge)
            {
                isLunging = true;
                rb.isKinematic = true;
                capsuleCollider.enabled = false;
            }

            nextAction = null;
            waitingOnPreviousActionAnimation = true;
        }
        else
        {

        }
    }
}
