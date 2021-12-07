using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//handles the physics and animation associated with performing actions as a slug
[RequireComponent(typeof(Rigidbody), typeof(Animator), typeof(CapsuleCollider))]
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
    private SlugAction nextAction;
    private bool hasNextAction;
    private bool waitingOnPreviousActionAnimation;

    [SerializeField]
    private float timeToReachTarget = 1f;
    [SerializeField]
    private float timeToRotateToTarget = 1f;
    [SerializeField]
    private float lungeTargetOffset = 1;
    private Vector3 smoothDampVelocity = Vector3.zero;
    private bool isLunging = false;
    [SerializeField]
    private GameObject lungeTarget;
    private float timeIntoLunge = 0;
    private Vector3 lungeEndPosition;
    private Vector3 lungeStartPosition;
    private Quaternion lungeEndRotation;
    private Quaternion lungeStartRotation;

    private Animator animator;
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Dictionary<SlugAction?, int> animatorTriggerKeys;

    public event System.Action controllerCallback;

    void Awake()
    {
        hasNextAction = false;
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

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
    }

    void Start()
    {
    }

    public void SetNextLungeTarget(GameObject target)
    {
        if (!isLunging)
            lungeTarget = target;
    }

    public void PerformAction(SlugAction action)
    {
        if (!waitingOnPreviousActionAnimation)
        {
            // Debug.Log($"{name} got action {action}", this.gameObject);
            nextAction = action;
            hasNextAction = true;
        }
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
        if (controllerCallback != null)
            controllerCallback();
    }

    public void OnTurnAnimationFinished()
    {
        waitingOnPreviousActionAnimation = false;
        if (controllerCallback != null)
            controllerCallback();
    }

    public void OnLungeAnimationFinished()
    {
        waitingOnPreviousActionAnimation = false;
        rb.isKinematic = false;
        capsuleCollider.enabled = true;
        if (controllerCallback != null)
            controllerCallback();
    }

    void FixedUpdate()
    {
        if (isLunging)
        {
            timeIntoLunge += Time.deltaTime;
            float lval = Mathf.InverseLerp(0, timeToReachTarget, timeIntoLunge);
            float lvalRotation = Mathf.InverseLerp(0, timeToRotateToTarget, timeIntoLunge);
            if (lval >= 1)
            {
                isLunging = false;
                rb.MovePosition(lungeEndPosition);
            }
            else
            {
                rb.MovePosition(Vector3.Lerp(lungeStartPosition, lungeEndPosition, lval));
                rb.MoveRotation(Quaternion.Slerp(lungeStartRotation, lungeEndRotation, lvalRotation));
            }
        }

        if (waitingOnPreviousActionAnimation)
        {
            return;
        }

        if (hasNextAction)
        {
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

                lungeStartRotation = transform.rotation;
                lungeEndRotation = Quaternion.LookRotation(vecToTarget);

                timeIntoLunge = 0;
                Destroy(lungeTarget, timeToReachTarget);
            }

            hasNextAction = false;
            waitingOnPreviousActionAnimation = true;
        }
    }
}
