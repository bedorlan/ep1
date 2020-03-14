using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    const float VELOCITY_RUNNING = 5.0f;

    private new Rigidbody2D rigidbody;
    private Animator animator;
    private Vector3 currentDestination;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        readInputs();
        moveToCurrentDestination();
    }

    private void readInputs()
    {
        if (Input.GetMouseButtonDown(0))
        {
            currentDestination = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
    }

    private void moveToCurrentDestination()
    {
        if (currentDestination == Vector3Int.zero) return;

        var distanceToDestination = Math.Abs(currentDestination.x - transform.position.x);
        if (distanceToDestination < 0.25f)
        {
            currentDestination = Vector3.zero;
            setVelocityX(0);
            return;
        }

        var newVelocityX = VELOCITY_RUNNING;
        if (currentDestination.x < transform.position.x)
            newVelocityX *= -1;
        setVelocityX(newVelocityX);
    }

    private void setVelocityX(float x)
    {
        rigidbody.velocity = new Vector2(x, 0);
        animator.SetFloat("velocityX", rigidbody.velocity.x);
        animator.SetFloat("absVelocityX", Math.Abs(rigidbody.velocity.x));

        flipIfNeeded();
    }

    private void flipIfNeeded()
    {
        var facingLeft = transform.localScale.x > 0;
        if (rigidbody.velocity.x > 0 && facingLeft
            || rigidbody.velocity.x < 0 && !facingLeft)
        {
            var flipped = transform.localScale;
            flipped.x *= -1;
            transform.localScale = flipped;
        }
    }
}
