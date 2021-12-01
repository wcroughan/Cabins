using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlugController : MonoBehaviour
{
    [SerializeField]
    CameraManager cameraManager;
    [SerializeField]
    PlayerManager playerManager;

    InputActions inputActions;

    // Start is called before the first frame update
    void Start()
    {

    }

    void OnEnable()
    {
        Debug.Log("The slug is becoming active!");
        if (inputActions == null)
        {
            inputActions = new InputActions();
            inputActions.WorldMovement.Attack.performed += ctx => OnAttackPerformed();
            inputActions.WorldMovement.Move.performed += ctx => OnMovePerformed();
        }

        // inputActions.WorldMovement.Attack.performed -= ctx => OnAttackPerformed();
        // inputActions.WorldMovement.Move.performed -= ctx => OnMovePerformed();
        inputActions.WorldMovement.Look.Enable();
        inputActions.WorldMovement.Attack.Enable();
        inputActions.WorldMovement.Move.Enable();
        // inputActions.Enable();
        Debug.Log("Enabled input");
    }

    void OnDisable()
    {
        inputActions.WorldMovement.Look.Disable();
        inputActions.WorldMovement.Attack.Disable();
        inputActions.WorldMovement.Move.Disable();
        // inputActions.WorldMovement.Attack.performed -= ctx => OnAttackPerformed();
        // inputActions.WorldMovement.Move.performed -= ctx => OnMovePerformed();
        // inputActions.Disable();
        // inputActions = null;
        Debug.Log("The slug is becoming not active!");
    }

    void OnAttackPerformed()
    {
        Debug.Log("Attack!");
        playerManager.TransitionToNextPlayerObject();
    }

    void OnMovePerformed()
    {
        Debug.Log("Move!");
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {

    }
}
