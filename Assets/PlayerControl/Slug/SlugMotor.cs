using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//handles the physics and animation associated with performing actions as a slug
[RequireComponent(typeof(Rigidbody), typeof(Animator))]
public class SlugMotor : MonoBehaviour
{
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
    private float timeToReachTarget = 1f;
    [SerializeField]
    private float destroyThreshold = 1;
    [SerializeField]
    private float lungeTargetOffset = 1;
    private Vector3 smoothDampVelocity = Vector3.zero;
    private bool isLunging = false;
    private GameObject lungeTarget;
    private float timeIntoLunge = 0;
    private Vector3 lungeEndPosition;
    private Vector3 lungeStartPosition;

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
        if (!isLunging)
            lungeTarget = target;
    }

    public void PerformAction(SlugAction action)
    {
        if (!waitingOnPreviousActionAnimation)
            nextAction = action;
    }

    void Update()
    {
        if (performIdleAnimations && !waitingOnPreviousActionAnimation && Random.Range(0f, 1f) < idleAnimationProbability * Time.deltaTime)
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
        rb.isKinematic = false;
        capsuleCollider.enabled = true;
    }

    void FixedUpdate()
    {
        timeIntoLunge += Time.deltaTime;
        if (isLunging)
        {
            float lval = Mathf.InverseLerp(0, timeToReachTarget, timeIntoLunge);
            if (lval >= 1)
            {
                isLunging = false;
                rb.MovePosition(lungeEndPosition);
                Destroy(lungeTarget);
            }
            else
            {
                rb.MovePosition(Vector3.Lerp(lungeStartPosition, lungeEndPosition, lval));
            }
        }

        if (waitingOnPreviousActionAnimation)
        {
            return;
        }

        if (nextAction != null)
        {
            Debug.Log($"Slug doing {nextAction}");
            //if we're just moving, root node movement will take care of that
            animator.SetTrigger(animatorTriggerKeys[nextAction]);

            if (nextAction == SlugAction.Lunge)
            {
                isLunging = true;
                rb.isKinematic = true;
                capsuleCollider.enabled = false;
                Vector3 targetPosition = lungeTarget.transform.position;
                lungeStartPosition = transform.position;
                Vector3 vecToTarget = (targetPosition - lungeStartPosition).normalized;
                lungeEndPosition = targetPosition - vecToTarget * lungeTargetOffset;

                timeIntoLunge = 0;
            }

            nextAction = null;
            waitingOnPreviousActionAnimation = true;
        }
    }
}
