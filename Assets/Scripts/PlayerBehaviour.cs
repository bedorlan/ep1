using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    const float VELOCITY_RUNNING = 10f;
    const float TIME_ANIMATION_PRE_FIRE = .15f;

    public GameObject currentProjectilePrefab; // todo: privatize this <
    public GameObject votesChangesIndicatorPrefab;

    private int playerNumber;
    private bool isLocal;
    private new Rigidbody2D rigidbody;
    private Animator animator;
    private float movingDestination;
    private GameObject firingTargetObject;
    private Vector3 firingTargetPosition;
    private bool isFiring = false;

    internal int GetPlayerNumber()
    {
        return playerNumber;
    }

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (isLocal && isFiring) return;

        if (isLocal)
        {
            tryFireAtCurrentTarget();
        }

        stopWhenArriveToDestination();
    }


    public void Initialize(int playerNumber, bool isLocal)
    {
        var PLAYER_INITIAL_POSITION = new Vector3(0, -4.197335f, 0);

        this.playerNumber = playerNumber;
        this.isLocal = isLocal;

        var newPosition = PLAYER_INITIAL_POSITION;
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

    private void tryFireAtCurrentTarget()
    {
        if (firingTargetPosition == Vector3.zero) return;

        var velocity = currentProjectilePrefab.GetComponentInChildren<Projectile>().AimAtTarget(transform, firingTargetPosition, 0f);
        if (velocity == Vector3.zero) return;

        StartCoroutine(Fire(currentProjectilePrefab, velocity, false));
    }

    private IEnumerator Fire(GameObject projectileToFire, Vector3 velocity, bool immediate)
    {
        var firingTargetObject = this.firingTargetObject;
        var firingTargetPosition = this.firingTargetPosition;
        this.firingTargetObject = null;
        this.firingTargetPosition = Vector3.zero;

        if (immediate)
        {
            var immediateProjectile = Instantiate(projectileToFire);
            immediateProjectile.GetComponentInChildren<Projectile>().FireProjectileImmediate(
                playerNumber,
                isLocal,
                firingTargetPosition,
                firingTargetObject);
            yield break;
        }

        if (isLocal)
        {
            var distance = Mathf.Abs(transform.position.x - firingTargetPosition.x);
            var timeToReach = TIME_ANIMATION_PRE_FIRE + (distance / Mathf.Abs(velocity.x));
            var projectileType = projectileToFire.GetComponentInChildren<Projectile>().projectileTypeId;
            NetworkManager.singleton.ProjectileFired(playerNumber, firingTargetPosition, timeToReach, projectileType, firingTargetObject);
        }

        isFiring = true;
        animator.SetBool("firing", true);
        stop();

        var targetAtMyRight = firingTargetPosition.x > transform.position.x;
        if (IsFacingLeft() && targetAtMyRight || !IsFacingLeft() && !targetAtMyRight)
        {
            Flip();
        }

        yield return new WaitForSeconds(TIME_ANIMATION_PRE_FIRE);
        var newProjectile = Instantiate(projectileToFire);
        newProjectile.GetComponentInChildren<Projectile>().FireProjectile(
            playerNumber,
            isLocal,
            transform,
            velocity,
            IsFacingLeft(),
            firingTargetObject);

        yield return new WaitForSeconds(.35f);
        animator.SetBool("firing", false);
        isFiring = false;
    }

    private void moveToCurrentDestination(float timeToReach)
    {
        if (timeToReach <= 0)
        {
            teletransport();
            return;
        }

        var distance = Mathf.Abs(transform.position.x - movingDestination);
        var velocityToReach = distance / timeToReach;

        var newVelocityX = Mathf.Max(VELOCITY_RUNNING, velocityToReach);
        if (movingDestination < transform.position.x) newVelocityX *= -1;
        setVelocityX(newVelocityX);
    }

    private void teletransport()
    {
        var toTheRight = movingDestination > transform.position.x;
        if (toTheRight && IsFacingLeft() || !toTheRight && !IsFacingLeft())
        {
            Flip();
        }

        var justGoThere = transform.position;
        justGoThere.x = movingDestination;
        transform.position = justGoThere;

        stop();
    }

    private void stopWhenArriveToDestination()
    {
        var distanceToDestination = movingDestination - transform.position.x;
        var direction = Mathf.Sign(rigidbody.velocity.x);
        if (direction > 0 && distanceToDestination <= 0
            || direction < 0 && distanceToDestination >= 0)
        {
            stop();
        }
    }

    private void stop()
    {
        movingDestination = 0f;
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

    public bool IsFacingLeft()
    {
        return transform.localScale.x > 0;
    }

    private void OnNewDestination(float positionX)
    {
        if (isLocal && isFiring) return;

        movingDestination = positionX;
        moveToCurrentDestination(float.MaxValue);

        var distance = Mathf.Abs(movingDestination - transform.position.x);
        var timeToReach = distance / VELOCITY_RUNNING;
        NetworkManager.singleton.NewLocalPlayerDestination(movingDestination, timeToReach);
    }

    public void Remote_NewDestination(float newDestination, float timeToReach)
    {
        movingDestination = newDestination;
        moveToCurrentDestination(timeToReach);
    }

    public void Remote_FireProjectile(GameObject projectile, Vector3 destination, float timeToReach, GameObject targetObject)
    {
        firingTargetObject = targetObject;
        firingTargetPosition = destination;
        timeToReach -= TIME_ANIMATION_PRE_FIRE;

        var velocity = projectile.GetComponentInChildren<Projectile>().AimAtTargetAnyVelocity(transform, firingTargetPosition);
        var immediate = timeToReach <= 0;

        StartCoroutine(Fire(projectile, velocity, immediate));
    }

    private void ChaseAndFireToPosition(Vector3 position)
    {
        if (isLocal && isFiring) return;
        firingTargetPosition = position;

        var offsetY = transform.position.y - firingTargetPosition.y;
        var maxReach = currentProjectilePrefab.GetComponentInChildren<Projectile>().CalcMaxReach(offsetY) - .1f;
        var distance = Mathf.Abs(firingTargetPosition.x - transform.position.x);
        var distanceToMove = distance - maxReach;
        if (distanceToMove <= 0) return;

        distanceToMove *= Mathf.Sign(firingTargetPosition.x - transform.position.x);
        var newPositionToFire = transform.position.x + distanceToMove;
        OnNewDestination(newPositionToFire);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isLocal) return;

        var voter = collision.GetComponent<VoterBehaviour>();
        if (!voter) return;

        voter.TryClaim(playerNumber);
    }

    internal void ChangeProjectile(GameObject projectile)
    {
        currentProjectilePrefab = projectile;
        stop();
    }

    internal void OnVotesChanges(int votes)
    {
        var position = transform.position;
        position.y += 3f;
        var votesIndicator = Instantiate(votesChangesIndicatorPrefab, position, Quaternion.identity);
        votesIndicator.GetComponent<VotesChangesBehaviour>().Show(votes);
    }

    private void OnMouseDown()
    {
        if (!isLocal)
        {
            NetworkManager.singleton.ObjectiveClicked(transform.root.gameObject);
        }
    }

    internal void NewObjective(Vector3 position, GameObject target)
    {
        if (isFiring) return;

        var validTarget = currentProjectilePrefab.GetComponentInChildren<IProjectile>().CanYouFireAt(position, target);
        if (!validTarget)
        {
            firingTargetPosition = Vector3.zero;
            firingTargetObject = null;
            OnNewDestination(position.x);
            return;
        }
        firingTargetObject = target;
        ChaseAndFireToPosition(position);
    }

    internal void AddVotes(int votes)
    {
        NetworkManager.singleton.AddVotes(playerNumber, votes);
    }
}
