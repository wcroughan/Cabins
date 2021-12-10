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
    LayerMask collisionLayers;

    [SerializeField]
    Transform player;

    private Vector3 cameraFollowVelocity = Vector3.zero;

    [SerializeField]
    CameraFollowInfo cameraFollowInfo;

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

    }

    void OnDisable()
    {
    }

    void LateUpdate()
    {
        //main camera object follows position of player
        Vector3 targetPosition = Vector3.SmoothDamp(transform.position, player.position, ref cameraFollowVelocity, cameraFollowInfo.cameraFollowSpeed);
        transform.position = targetPosition;

        Quaternion targetRotation = Quaternion.Euler(cameraFollowInfo.lookVerticalAngle, cameraFollowInfo.lookHorizontalAngle, 0f);
        cameraPivot.localRotation = targetRotation;
        Vector3 targetPivotLocation = cameraPivot.localPosition;
        targetPivotLocation.y = cameraFollowInfo.preferredCameraPivotHeight;
        cameraPivot.localPosition = targetPivotLocation;

        //camera endpoint (which is where camera itself is, circling around the pivot) gets closer if there's anything between the player and its prefered position
        RaycastHit hit;
        Vector3 direction = (cameraEndpoint.position - cameraPivot.position).normalized;

        float targetFollowDistance = -cameraFollowInfo.preferredCameraFollowDistance;
        if (Physics.SphereCast(cameraPivot.position, camCollisionRadius, direction, out hit, cameraFollowInfo.preferredCameraFollowDistance, collisionLayers))
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
