using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    const float VELOCITY_RUNNING = 10f;

    private int playerNumber;
    private bool isLocal;
    private new Rigidbody2D rigidbody;
    private Animator animator;
    private float currentDestination;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isLocal)
        {
            readInputs();
        }

        moveToCurrentDestination();
    }

    private void readInputs()
    {
        if (Input.GetMouseButtonDown(0))
        {
            currentDestination = Camera.main.ScreenToWorldPoint(Input.mousePosition).x;
            NetworkManager.singleton.NewLocalPlayerDestination(currentDestination);
        }
    }

    private void moveToCurrentDestination()
    {
        if (currentDestination == 0f) return;

        var distanceToDestination = Math.Abs(currentDestination - transform.position.x);
        if (distanceToDestination < 0.25f)
        {
            currentDestination = 0;
            setVelocityX(0);
            return;
        }

        var newVelocityX = VELOCITY_RUNNING;
        if (currentDestination < transform.position.x)
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
        var facingLeft = IsFacingLeft();
        if (rigidbody.velocity.x > 0 && facingLeft
            || rigidbody.velocity.x < 0 && !facingLeft)
        {
            Flip();
        }
    }

    private void Flip()
    {
        var flipped = transform.localScale;
        flipped.x *= -1;
        transform.localScale = flipped;
    }

    private readonly Vector3 playerInitialPosition = new Vector3(0, -4.197335f, 0);

    public void Initialize(int playerNumber, bool isLocal)
    {
        this.playerNumber = playerNumber;
        this.isLocal = isLocal;

        var newPosition = playerInitialPosition;
        if (playerNumber == 0)
        {
            newPosition.x = -2.5f;
            Flip();
        }
        else if (playerNumber == 1)
        {
            newPosition.x = 2.5f;
        }
        transform.position = newPosition;
    }

    public bool IsFacingLeft()
    {
        return transform.localScale.x > 0;
    }

    public void NewDestination(float newDestination)
    {
        currentDestination = newDestination;
        moveToCurrentDestination();
    }
}
