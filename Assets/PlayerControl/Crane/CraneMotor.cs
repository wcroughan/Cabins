using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Animator), typeof(BoxCollider))]
public class CraneMotor : MonoBehaviour
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

    public enum CraneAction { TurnLeft, TurnRight, MoveForward, Attack };
    private CraneAction nextAction;
    private bool hasNextAction;
    private bool waitingOnPreviousActionAnimation;

    private Rigidbody rb;
    private Animator animator;
    private BoxCollider boxCollider;
    private Dictionary<CraneAction, int> animatorTriggerKeys;
    private int idleFlavorTriggerID;
    private int attackSuccessfulBoolID;

    private bool isAttacking;
    private GameObject nextTarget;

    void Awake()
    {
        hasNextAction = false;
        waitingOnPreviousActionAnimation = false;
        isAttacking = false;
        idleAnimationProbability = 1f / idleAnimationFrequency;

        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider>();

        animatorTriggerKeys = new Dictionary<CraneAction, int>();
        animatorTriggerKeys[CraneAction.MoveForward] = Animator.StringToHash("RollTrigger");
        animatorTriggerKeys[CraneAction.TurnLeft] = Animator.StringToHash("RotateLeftTrigger");
        animatorTriggerKeys[CraneAction.TurnRight] = Animator.StringToHash("RotateRightTrigger");
        animatorTriggerKeys[CraneAction.Attack] = Animator.StringToHash("LungeTrigger");

        idleAnimationTriggerID = Animator.StringToHash("IdleFlavorTrigger");
        attackSuccessfulBoolID = Animator.StringToHash("LungeSuccessful");
    }


    public void SetNextTarget(GameObject target)
    {
        if (!isAttacking)
            nextTarget = target;
    }

    public void PerformAction(CraneAction action)
    {
        if (!waitingOnPreviousActionAnimation)
        {
            Debug.Log($"{name} got action {action}", this.gameObject);
            nextAction = action;
            hasNextAction = true;
        }
    }

    public void OnIdleAnimationStateEntered()
    {
        waitingOnPreviousActionAnimation = false;
        rb.velocity = Vector3.zero;
        if (isAttacking)
        {
            isAttacking = false;
        }
    }

    void Update()
    {
        if (performIdleAnimations && !waitingOnPreviousActionAnimation && Random.Range(0f, 1f) < idleAnimationProbability * Time.deltaTime)
        {
            animator.SetTrigger(idleAnimationTriggerID);
        }
    }

    void FixedUpdate()
    {

        if (waitingOnPreviousActionAnimation)
        {
            return;
        }

        if (hasNextAction)
        {
            //if we're just moving, root node movement will take care of that
            animator.SetTrigger(animatorTriggerKeys[nextAction]);

            hasNextAction = false;
            waitingOnPreviousActionAnimation = true;
        }
    }

}
