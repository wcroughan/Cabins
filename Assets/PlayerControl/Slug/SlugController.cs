using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SlugMotor))]
public class SlugController : MonoBehaviour
{
    [SerializeField]
    PlayerManager playerManager;

    [SerializeField]
    float targetSearchRadius;
    [SerializeField]
    float targetMaxAngle;
    private GameObject nextTarget;

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
        //look for possible targets

        Collider[] possibleTargets = Physics.OverlapSphere(transform.position, targetSearchRadius);
        float minDist = float.PositiveInfinity;
        nextTarget = null;
        for (int i = 0; i < possibleTargets.Length; i++)
        {
            Transform t = possibleTargets[i].transform;
            Vector3 d = t.position - transform.position;
            if (d == Vector3.zero)
            {
                continue;
            }
            float a = Vector3.Angle(transform.forward, d);
            if (a < targetMaxAngle)
            {
                float dds = d.sqrMagnitude;
                if (dds < minDist)
                {
                    nextTarget = possibleTargets[i].gameObject;
                    minDist = dds;
                }
            }

        }

        if (nextTarget != null)
        {
            shouldAttack = true;
        }
    }

    public void OnExitTransitionAnimationFinished()
    {
        playerManager.TransitionToNextPlayerObject();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        // Gizmos.DrawWireSphere(transform.position, targetSearchRadius);
        Gizmos.DrawFrustum(transform.position, targetMaxAngle, targetSearchRadius, 0, 1f);
    }

    // Update is called once per frame
    void Update()
    {

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
