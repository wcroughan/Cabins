using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SlugMotor))]
public class SlugController : MonoBehaviour
{
    [SerializeField]
    PlayerManager playerManager;

    [SerializeField]
    List<GameObject> targets;
    private GameObject nextTarget;
    private int nextTargetIdx;

    float counter = 0;

    private SlugMotor motor;
    private InputActions inputActions;
    private Vector2 userMovementInput;
    private bool shouldAttack;

    // Start is called before the first frame update
    void Start()
    {
        motor = GetComponent<SlugMotor>();
    }

    void OnEnable()
    {
        Debug.Log("The player slug is becoming active!");
        if (inputActions == null)
        {
            inputActions = new InputActions();
            inputActions.WorldMovement.Attack.performed += ctx => OnAttackPerformed();
            inputActions.WorldMovement.Move.performed += ctx => userMovementInput = ctx.ReadValue<Vector2>();
        }

        inputActions.WorldMovement.Attack.Enable();
        inputActions.WorldMovement.Move.Enable();
        shouldAttack = false;
    }

    void OnDisable()
    {
        inputActions.WorldMovement.Attack.Disable();
        inputActions.WorldMovement.Move.Disable();
    }

    void OnAttackPerformed()
    {
        // animator.SetTrigger("TransitionToNextPlayer");
        shouldAttack = true;
        nextTarget = targets[nextTargetIdx++];
        if (nextTargetIdx >= targets.Count)
            nextTargetIdx = 0;
    }

    public void OnExitTransitionAnimationFinished()
    {
        playerManager.TransitionToNextPlayerObject();
    }

    // Update is called once per frame
    void Update()
    {
        counter += Time.deltaTime;
        if (counter > 3)
        {
            counter = 0;
            shouldAttack = true;
            nextTarget = targets[nextTargetIdx++];
            if (nextTargetIdx >= targets.Count)
                nextTargetIdx = 0;
        }

        if (shouldAttack)
        {
            shouldAttack = false;
            motor.SetNextLungeTarget(nextTarget);
            motor.PerformAction(SlugMotor.SlugAction.Lunge);
        }
        else if (userMovementInput.y > 0)
        {
            motor.PerformAction(SlugMotor.SlugAction.MoveForward);
        }
        else if (userMovementInput.y < 0)
        {
            motor.PerformAction(SlugMotor.SlugAction.MoveBackward);
        }
        else if (userMovementInput.x > 0)
        {
            motor.PerformAction(SlugMotor.SlugAction.TurnRight);
        }
        else if (userMovementInput.x < 0)
        {
            motor.PerformAction(SlugMotor.SlugAction.TurnLeft);
        }
    }
}
