using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CraneMotor))]
public class CranePlayerController : MonoBehaviour
{
    [SerializeField]
    CraneStats stats;
    [SerializeField]
    CameraFollowInfo cameraFollowInfo;

    private GameObject nextTarget;
    private CraneMotor motor;
    private InputActions inputActions;
    private Vector2 userMovementInput;
    private bool shouldAttack;

    void Awake()
    {
        motor = GetComponent<CraneMotor>();
    }

    void OnEnable()
    {
        Debug.Log("The player crane is becoming active!");
        if (inputActions == null)
        {
            inputActions = new InputActions();
            inputActions.WorldMovement.Attack.started += ctx => OnAttackPerformed();
            inputActions.WorldMovement.Move.performed += ctx => userMovementInput = ctx.ReadValue<Vector2>();
        }

        inputActions.WorldMovement.Attack.Enable();
        inputActions.WorldMovement.Move.Enable();
        shouldAttack = false;
        cameraFollowInfo.preferredCameraFollowDistance = stats.preferredCameraFollowDistance;
        cameraFollowInfo.preferredCameraPivotHeight = stats.preferredCameraPivotHeight;
        cameraFollowInfo.lookVerticalAngle = stats.cameraVerticalAngle;
        AlignCamera();
    }

    void OnDisable()
    {
        inputActions.WorldMovement.Attack.Disable();
        inputActions.WorldMovement.Move.Disable();
    }

    void OnAttackPerformed()
    {
        //look for possible targets
        SelectNextTarget();
        if (nextTarget != null)
        {
            shouldAttack = true;
        }
    }

    private void SelectNextTarget()
    {
        // Collider[] possibleTargets = Physics.OverlapSphere(transform.position, stats.targetSearchRadius);
        // float minDist = float.PositiveInfinity;
        // nextTarget = null;
        // for (int i = 0; i < possibleTargets.Length; i++)
        // {
        //     Transform t = possibleTargets[i].transform;
        //     Vector3 d = t.position - transform.position;
        //     if (d == Vector3.zero)
        //     {
        //         continue;
        //     }
        //     float a = Vector3.Angle(transform.forward, d);
        //     if (a < stats.targetMaxAngle)
        //     {
        //         float dds = d.sqrMagnitude;
        //         if (dds < minDist)
        //         {
        //             nextTarget = possibleTargets[i].gameObject;
        //             minDist = dds;
        //         }
        //     }
        // }

        nextTarget = null;

    }


    void AlignCamera()
    {
        cameraFollowInfo.lookHorizontalAngle = transform.rotation.eulerAngles.y;
    }



    // Update is called once per frame
    void Update()
    {
        AlignCamera();

        if (shouldAttack)
        {
            shouldAttack = false;
            motor.SetNextTarget(nextTarget);
            motor.PerformAction(CraneMotor.CraneAction.Attack);
        }
        else if (userMovementInput.y > 0)
        {
            motor.PerformAction(CraneMotor.CraneAction.MoveForward);
        }
        else if (userMovementInput.y < 0)
        {
            // motor.PerformAction(CraneMotor.CraneAction.Attack);
        }
        else if (userMovementInput.x > 0)
        {
            motor.PerformAction(CraneMotor.CraneAction.TurnRight);
        }
        else if (userMovementInput.x < 0)
        {
            motor.PerformAction(CraneMotor.CraneAction.TurnLeft);
        }
    }
}
