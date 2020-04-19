using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurrentTargetIndicatorBehaviour : MonoBehaviour
{
    public GameObject flagObject;
    public GameObject targetObject;

    static internal CurrentTargetIndicatorBehaviour singleton { get; private set; }

    private Rigidbody2D myRigidbody;
    private GameObject currentTarget;

    internal void NewTargetPosition(Vector3 newPosition)
    {
        flagObject.SetActive(true);
        targetObject.SetActive(false);

        myRigidbody.bodyType = RigidbodyType2D.Dynamic;
        myRigidbody.velocity = Vector2.zero;
        newPosition.z = 0f;
        transform.position = newPosition;
        currentTarget = null;
    }

    internal void NewTargetObject(GameObject target)
    {
        flagObject.SetActive(false);
        targetObject.SetActive(true);

        myRigidbody.bodyType = RigidbodyType2D.Kinematic;
        myRigidbody.velocity = Vector2.zero;
        if (target != null) currentTarget = target.transform.root.gameObject;
    }

    private void Awake()
    {
        singleton = this;
        myRigidbody = transform.root.GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!targetObject.activeSelf) return;
        if (currentTarget == null || !currentTarget.activeSelf)
        {
            targetObject.SetActive(false);
            return;
        }

        var position = currentTarget.transform.position;
        position.z = 0f;
        transform.position = position;
    }
}
