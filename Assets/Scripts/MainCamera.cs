using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour
{
    public GameObject objectToFollow;

    private const float speed = 2f;
    private const float offsetSideOfView = 7f;

    private void Start()
    {
        Camera.main.eventMask = (1 << LayerMask.NameToLayer("Default"))
            | (1 << LayerMask.NameToLayer("UI"))
            | (1 << LayerMask.NameToLayer("Voters"));
    }

    void Update()
    {
        if (!objectToFollow) return;

        var player = objectToFollow.GetComponent<PlayerBehaviour>();
        var desiredCameraPosition = objectToFollow.transform.position.x;
        desiredCameraPosition += player.IsFacingLeft() ? offsetSideOfView * -1 : offsetSideOfView;

        var newCameraPosition = transform.position;
        newCameraPosition.x = Mathf.Lerp(transform.position.x, desiredCameraPosition, speed * Time.deltaTime);
        transform.position = newCameraPosition;
    }
}
