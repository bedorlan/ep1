using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour
{
    public GameObject objectToFollow;

    private Camera myCamera;
    private int cameraLimitOffset;

    const float speed = 2f;
    const float offsetSideOfView = 7f;
    const float limitLeft = -104.2f;
    const float limitRight = 102.5f;

    private void Start()
    {
        myCamera = GetComponent<Camera>();
        myCamera.eventMask = LayerMask.GetMask(new string[] {
            "Default",
            "Player",
            "UI",
            "Voters",
        });

        var halfHeight = myCamera.orthographicSize;
        var halfWidth = myCamera.aspect * halfHeight;
        cameraLimitOffset = (int)halfWidth + 1;
    }

    void Update()
    {
        if (!objectToFollow) return;

        var player = objectToFollow.GetComponent<PlayerBehaviour>();
        var desiredCameraPosition = objectToFollow.transform.position.x;
        desiredCameraPosition += player.IsFacingLeft() ? offsetSideOfView * -1 : offsetSideOfView;

        var newCameraPosition = transform.position;
        newCameraPosition.x = Mathf.Lerp(transform.position.x, desiredCameraPosition, speed * Time.deltaTime);
        newCameraPosition.x = Mathf.Max(newCameraPosition.x, limitLeft + cameraLimitOffset);
        newCameraPosition.x = Mathf.Min(newCameraPosition.x, limitRight - cameraLimitOffset);
        transform.position = newCameraPosition;
    }
}
