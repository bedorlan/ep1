using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    const float VELOCITY_RUNNING = 10f;

    public GameObject tamalPrefab;

    private int playerNumber;
    private bool isLocal;
    private new Rigidbody2D rigidbody;
    private Animator animator;
    private float currentDestination;
    private Transform currentTarget;
    private bool busy = false;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (busy) return;

        if (isLocal)
        {
            readInputs();
        }

        fireCurrentTarget();
        stopWhenArriveToDestination();
    }

    private void fireCurrentTarget()
    {
        if (currentTarget == null) return;

        var direction = tamalPrefab.GetComponent<Projectile>().AimAtTarget(transform, currentTarget.transform);
        if (direction == Vector3.zero) return;

        StartCoroutine(Fire(direction));
    }

    IEnumerator Fire(Vector3 direction)
    {
        busy = true;
        animator.SetBool("firing", true);
        stop();
        var targetAtMyRight = currentTarget.position.x > transform.position.x;
        if (IsFacingLeft() && targetAtMyRight || !IsFacingLeft() && !targetAtMyRight)
        {
            Flip();
        }

        // wait for animation to raise hands
        yield return new WaitForSeconds(.15f);
        var newProjectile = Instantiate(tamalPrefab);
        newProjectile.GetComponent<Projectile>().FireProjectile(playerNumber, transform, direction, IsFacingLeft());
        currentTarget = null;

        // wait for animation to finish
        yield return new WaitForSeconds(.35f);
        animator.SetBool("firing", false);
        busy = false;
    }

    private void readInputs()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        currentDestination = Camera.main.ScreenToWorldPoint(Input.mousePosition).x;
        moveToCurrentDestination(float.MaxValue);

        var distance = Mathf.Abs(currentDestination - transform.position.x);
        var timeToReach = distance / VELOCITY_RUNNING;
        NetworkManager.singleton.NewLocalPlayerDestination(currentDestination, timeToReach);
    }

    private void moveToCurrentDestination(float timeToReach)
    {
        if (timeToReach < 0)
        {
            stop();

            var justGoThere = transform.position;
            justGoThere.x = currentDestination;
            transform.position = justGoThere;
            return;
        }

        var distance = Mathf.Abs(transform.position.x - currentDestination);
        var velocityToReach = distance / timeToReach;

        var newVelocityX = Mathf.Max(VELOCITY_RUNNING, velocityToReach);
        if (currentDestination < transform.position.x) newVelocityX *= -1;
        setVelocityX(newVelocityX);
    }

    private void stopWhenArriveToDestination()
    {
        var distanceToDestination = Math.Abs(currentDestination - transform.position.x);
        if (distanceToDestination < 0.25f)
        {
            stop();
        }
    }

    private void stop()
    {
        currentDestination = 0f;
        setVelocityX(0);
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

        foreach (var child in GetComponentsInChildren<SpriteRenderer>())
        {
            child.color = Common.playerColors[playerNumber];
        }
    }

    public bool IsFacingLeft()
    {
        return transform.localScale.x > 0;
    }

    public void Remote_NewDestination(float newDestination, float timeToReach)
    {
        currentDestination = newDestination;
        moveToCurrentDestination(timeToReach);
    }

    public void ChaseVoter(VoterBehaviour voter)
    {
        currentTarget = voter.transform;
        // the method readInputs will set the currentDestination
    }
}
