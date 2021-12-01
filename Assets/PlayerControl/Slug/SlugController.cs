using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlugController : MonoBehaviour
{
    [SerializeField]
    CameraManager cameraManager;
    [SerializeField]
    PlayerManager playerManager;
    [SerializeField]
    float speed;
    [SerializeField]
    float idleAnimationFrequency = 10f;
    private float idleAnimationProbability;

    private InputActions inputActions;
    private Vector2 userMovementInput;
    private Rigidbody rb;
    private Animator animator;
    private int idleAnimationTriggerID;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        idleAnimationTriggerID = Animator.StringToHash("IdleAnimationTrigger");
        idleAnimationProbability = 1f / idleAnimationFrequency;
    }

    void OnEnable()
    {
        Debug.Log("The slug is becoming active!");
        if (inputActions == null)
        {
            inputActions = new InputActions();
            inputActions.WorldMovement.Attack.performed += ctx => OnAttackPerformed();
            inputActions.WorldMovement.Move.performed += ctx => userMovementInput = ctx.ReadValue<Vector2>();
        }

        inputActions.WorldMovement.Attack.Enable();
        inputActions.WorldMovement.Move.Enable();
    }

    void OnDisable()
    {
        inputActions.WorldMovement.Attack.Disable();
        inputActions.WorldMovement.Move.Disable();
    }

    void OnAttackPerformed()
    {
        animator.SetTrigger("TransitionToNextPlayer");
    }

    public void OnExitTransitionAnimationFinished()
    {
        playerManager.TransitionToNextPlayerObject();
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetFloat("Speed", rb.velocity.magnitude);
        if (Random.Range(0f, 1f) < idleAnimationProbability * Time.deltaTime)
        {
            animator.SetTrigger(idleAnimationTriggerID);
        }
    }

    void FixedUpdate()
    {
        rb.AddForce(new Vector3(userMovementInput.x, 0f, userMovementInput.y) * speed, ForceMode.VelocityChange);
    }
}
