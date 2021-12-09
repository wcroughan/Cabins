using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParachuteMotor))]
public class ParachutePlayerController : MonoBehaviour
{
    [SerializeField]
    ParachuteStats stats;
    [SerializeField]
    CameraFollowInfo cameraFollowInfo;

    private InputActions inputActions;
    private ParachuteMotor motor;


    void Awake()
    {
        motor = GetComponent<ParachuteMotor>();
    }

    // Start is called before the first frame update
    void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new InputActions();
            inputActions.WorldMovement.Move.performed += ctx => Move(ctx.ReadValue<Vector2>());
            inputActions.WorldMovement.Jump.performed += ctx => Jump(ctx.ReadValueAsButton());
        }

        inputActions.WorldMovement.Move.Enable();
        inputActions.WorldMovement.Jump.Enable();
        cameraFollowInfo.preferredCameraFollowDistance = stats.preferredCameraFollowDistance;
        cameraFollowInfo.preferredCameraPivotHeight = stats.preferredCameraPivotHeight;
        cameraFollowInfo.lookVerticalAngle = stats.cameraVerticalAngle;
    }

    void OnDisable()
    {
        inputActions.WorldMovement.Move.Disable();
        inputActions.WorldMovement.Jump.Disable();
    }

    void Jump(bool shouldJump)
    {
        if (shouldJump)
            motor.Jump();
    }

    void Move(Vector2 userMovementInput)
    {
        motor.Move(userMovementInput);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
