using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    const float VELOCITY_RUNNING = 10f;
    const float TIME_ANIMATION_PRE_FIRE = .15f;

    public GameObject tamalPrefab;

    private int playerNumber;
    private bool isLocal;
    private new Rigidbody2D rigidbody;
    private Animator animator;
    private float currentDestination;
    private Vector3 currentTarget;
    private bool isBusy = false;

    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isLocal && isBusy) return;

        if (isLocal)
        {
            tryFireAtCurrentTarget();
        }

        stopWhenArriveToDestination();
    }

    private void tryFireAtCurrentTarget()
    {
        if (currentTarget == Vector3.zero) return;

        var velocity = tamalPrefab.GetComponent<Projectile>().AimAtTarget(transform, currentTarget, 0f);
        if (velocity == Vector3.zero) return;

        StartCoroutine(Fire(velocity, false));
    }

    IEnumerator Fire(Vector3 velocity, bool immediate)
    {
        if (immediate)
        {
            var immediateProjectile = Instantiate(tamalPrefab);
            immediateProjectile.GetComponent<Projectile>().FireProjectileImmediate(
                playerNumber,
                currentTarget);
            currentTarget = Vector3.zero;
            yield break;
        }

        if (isLocal)
        {
            var distance = Mathf.Abs(transform.position.x - currentTarget.x);
            var timeToReach = TIME_ANIMATION_PRE_FIRE + (distance / Mathf.Abs(velocity.x));
            NetworkManager.singleton.ProjectileFired(playerNumber, currentTarget, timeToReach);
        }

        isBusy = true;
        animator.SetBool("firing", true);
        stop();

        var targetAtMyRight = currentTarget.x > transform.position.x;
        if (IsFacingLeft() && targetAtMyRight || !IsFacingLeft() && !targetAtMyRight)
        {
            Flip();
        }

        // wait for animation to raise hands
        yield return new WaitForSeconds(TIME_ANIMATION_PRE_FIRE);
        var newProjectile = Instantiate(tamalPrefab);
        newProjectile.GetComponent<Projectile>().FireProjectile(
            playerNumber,
            transform,
            velocity,
            IsFacingLeft());
        currentTarget = Vector3.zero;

        // wait for animation to finish
        yield return new WaitForSeconds(.35f);
        animator.SetBool("firing", false);
        isBusy = false;
    }

    private void moveToCurrentDestination(float timeToReach)
    {
        if (timeToReach < 0)
        {
            teletransport();
            return;
        }

        var distance = Mathf.Abs(transform.position.x - currentDestination);
        var velocityToReach = distance / timeToReach;

        var newVelocityX = Mathf.Max(VELOCITY_RUNNING, velocityToReach);
        if (currentDestination < transform.position.x) newVelocityX *= -1;
        setVelocityX(newVelocityX);
    }

    private void teletransport()
    {
        var toTheRight = currentDestination > transform.position.x;
        if (toTheRight && IsFacingLeft() || !toTheRight && !IsFacingLeft())
        {
            Flip();
        }

        var justGoThere = transform.position;
        justGoThere.x = currentDestination;
        transform.position = justGoThere;

        stop();
    }

    private void stopWhenArriveToDestination()
    {
        var distanceToDestination = currentDestination - transform.position.x;
        var direction = Mathf.Sign(rigidbody.velocity.x);
        if (direction > 0 && distanceToDestination <= 0
            || direction < 0 && distanceToDestination >= 0)
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

    public void OnNewDestination(float positionX)
    {
        if (isLocal && isBusy) return;

        currentDestination = positionX;
        moveToCurrentDestination(float.MaxValue);

        var distance = Mathf.Abs(currentDestination - transform.position.x);
        var timeToReach = distance / VELOCITY_RUNNING;
        NetworkManager.singleton.NewLocalPlayerDestination(currentDestination, timeToReach);
    }

    public void Remote_NewDestination(float newDestination, float timeToReach)
    {
        currentDestination = newDestination;
        moveToCurrentDestination(timeToReach);
    }

    public void Remote_FireProjectile(Vector3 destination, float timeToReach)
    {
        currentTarget = destination;
        timeToReach -= TIME_ANIMATION_PRE_FIRE;

        var velocity = tamalPrefab.GetComponent<Projectile>().AimAtTargetAnyVelocity(transform, currentTarget);
        var immediate = timeToReach <= 0;

        StartCoroutine(Fire(velocity, immediate));
    }

    public void ChaseVoter(VoterBehaviour voter)
    {
        if (isLocal && isBusy) return;
        currentTarget = voter.transform.position;

        var offsetY = transform.position.y - currentTarget.y;
        var maxReach = tamalPrefab.GetComponent<Projectile>().CalcMaxReach(offsetY) - .1f;
        var distance = Mathf.Abs(currentTarget.x - transform.position.x);
        var distanceToMove = distance - maxReach;
        if (distanceToMove <= 0) return;

        distanceToMove *= Mathf.Sign(currentTarget.x - transform.position.x);
        var newPositionToFire = transform.position.x + distanceToMove;
        OnNewDestination(newPositionToFire);
    }
}
