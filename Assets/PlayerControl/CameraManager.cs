using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField]
    Transform cameraPivot;
    [SerializeField]
    Transform cameraEndpoint;
    [SerializeField]
    float cameraFollowSpeed = 1f;
    [SerializeField]
    float cameraRotateSpeed = 1f;
    [SerializeField]
    float minVertAngle = -35f, maxVertAngle = 35f;
    [SerializeField]
    public float defaultCameraFollowDistance = 4.25f;
    [SerializeField]
    LayerMask collisionLayers;

    [SerializeField]
    Transform player;
    InputActions inputActions;

    private Vector3 cameraFollowVelocity = Vector3.zero;
    private float lookHorizontalAngle = 0f;
    private float lookVerticalAngle = 0f;
    private Vector2 lookInput;

    float camCollisionRadius = 0.2f;
    float camCollisionOffset = 0.2f;
    float minimumCamFollowOffset = 0.2f;


    // Start is called before the first frame update
    void Start()
    {
        // player = FindObjectOfType<ThirdPersonMovement>().transform;
    }

    void OnEnable()
    {

        if (inputActions == null)
        {
            inputActions = new InputActions();

            inputActions.WorldMovement.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();

            inputActions.WorldMovement.Look.Enable();
        }
    }

    void OnDisable()
    {
        inputActions.WorldMovement.Look.Disable();
    }

    void LateUpdate()
    {
        //main camera object follows position of player
        Vector3 targetPosition = Vector3.SmoothDamp(transform.position, player.position, ref cameraFollowVelocity, cameraFollowSpeed);
        transform.position = targetPosition;

        //camera pivot object rotates around player according to mouse
        lookHorizontalAngle += lookInput.x * cameraRotateSpeed;
        lookVerticalAngle -= lookInput.y * cameraRotateSpeed;
        lookVerticalAngle = Mathf.Clamp(lookVerticalAngle, minVertAngle, maxVertAngle);

        Quaternion targetRotation = Quaternion.Euler(lookVerticalAngle, lookHorizontalAngle, 0f);
        cameraPivot.localRotation = targetRotation;

        //camera endpoint (which is where camera itself is, circling around the pivot) gets closer if there's anything between the player and its prefered position
        RaycastHit hit;
        Vector3 direction = (cameraEndpoint.position - cameraPivot.position).normalized;

        float targetFollowDistance = -defaultCameraFollowDistance;
        if (Physics.SphereCast(cameraPivot.position, camCollisionRadius, direction, out hit, defaultCameraFollowDistance, collisionLayers))
        {
            //something is in between the person and the camera's preferred location
            float distance = Vector3.Distance(cameraPivot.position, hit.point);
            targetFollowDistance = -distance + camCollisionOffset;
        }
        if (Mathf.Abs(targetFollowDistance) < minimumCamFollowOffset)
        {
            targetFollowDistance -= minimumCamFollowOffset;
        }

        Vector3 camLocalPos = cameraEndpoint.localPosition;
        camLocalPos.z = Mathf.Lerp(camLocalPos.z, targetFollowDistance, 0.2f * Time.deltaTime);
        cameraEndpoint.localPosition = camLocalPos;

    }
}
