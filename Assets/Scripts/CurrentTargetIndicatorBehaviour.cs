using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurrentTargetIndicatorBehaviour : MonoBehaviour
{
    public GameObject flagObject;
    public GameObject targetObject;

    static internal CurrentTargetIndicatorBehaviour singleton { get; private set; }

    private Rigidbody2D myRigidbody;

    internal void NewTargetPosition(Vector3 newPosition)
    {
        flagObject.SetActive(true);
        //targetObject.SetActive(false);

        myRigidbody.bodyType = RigidbodyType2D.Dynamic;
        myRigidbody.velocity = Vector2.zero;
        newPosition.z = 0f;
        transform.position = newPosition;
    }

    private void Awake()
    {
        singleton = this;
        myRigidbody = transform.root.GetComponent<Rigidbody2D>();
    }
}
