using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    const float DEFAULT_VELOCITY_RUNNING = 10f;
    const float TIME_ANIMATION_PRE_FIRE = .15f;

    public GameObject playerIndicatorObject;
    public List<GameObject> headsPrefabs;
    public List<AudioClip> partyIntroductions;
    public GameObject changeHeadAnimationPrefab;
    public List<AudioClip> projectileIntroductions;

    internal Common.Parties party { get; private set; }
    internal event Action OnProjectileFired;

    private int playerNumber;
    private bool isLocal;
    private int votesCount = 0;
    private float currentVelocityRunning = DEFAULT_VELOCITY_RUNNING;
    private GameObject currentProjectilePrefab;
    private Rigidbody2D myRigidbody;
    private Animator animator;
    private AudioSource audioSource;
    private float movingDestination;
    private GameObject firingTargetObject;
    private Vector3 firingTargetPosition;
    private bool isFiring = false;
    private bool uribeIsHelping = false;

    internal int GetPlayerNumber()
    {
        return playerNumber;
    }

    void Awake()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (isLocal && isFiring) return;

        if (isLocal)
        {
            if (firingTargetObject != null)
            {
                firingTargetPosition = firingTargetObject.transform.root.position;
            }
            tryFireAtCurrentTarget();
        }

        stopWhenArrivedToDestinationOrKeepChasing();
    }

    public void Initialize(int playerNumber, bool isLocal)
    {
        var PLAYER_INITIAL_POSITION = new Vector3(0f, -2.7f, transform.position.z);

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
        else if (playerNumber == 2)
        {
            newPosition.x = -5f;
            Flip();
        }
        else if (playerNumber == 3)
        {
            newPosition.x = 5f;
        }
        newPosition.x += -33f;
        transform.position = newPosition;

        if (isLocal) playerIndicatorObject.SetActive(true);

        foreach (var child in GetComponentsInChildren<SpriteRenderer>())
        {
            child.color = Common.playerColors[playerNumber];
        }
    }

    private void tryFireAtCurrentTarget()
    {
        if (firingTargetPosition == Vector3.zero) return;

        var velocity = currentProjectilePrefab.GetComponentInChildren<Projectile>().AimAtTarget(transform.position, firingTargetPosition, 0f);
        if (velocity == Vector3.zero) return;

        StartCoroutine(Fire(currentProjectilePrefab, velocity, false, null));
    }

    private IEnumerator Fire(GameObject projectileToFire, Vector3 velocity, bool immediate, string projectileId)
    {
        var newProjectileId = projectileId ?? Projectile.NewId(playerNumber);
        var firingTargetObject = this.firingTargetObject;
        var firingTargetPosition = this.firingTargetPosition;
        this.firingTargetObject = null;
        this.firingTargetPosition = Vector3.zero;

        if (immediate)
        {
            var immediateProjectile = Instantiate(projectileToFire);
            immediateProjectile.GetComponentInChildren<Projectile>().FireProjectileImmediate(
                this,
                playerNumber,
                isLocal,
                transform.position,
                firingTargetPosition,
                firingTargetObject,
                newProjectileId);
            yield break;
        }


        if (isLocal)
        {
            var distance = Mathf.Abs(transform.position.x - firingTargetPosition.x);
            var timeToReach = TIME_ANIMATION_PRE_FIRE + (distance / Mathf.Abs(velocity.x));
            var projectileType = projectileToFire.GetComponentInChildren<Projectile>().projectileTypeId;
            NetworkManager.singleton.ProjectileFired(
                playerNumber,
                firingTargetPosition,
                timeToReach,
                projectileType,
                firingTargetObject,
                newProjectileId);
            OnProjectileFired?.Invoke();
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
            this,
            playerNumber,
            isLocal,
            transform,
            velocity,
            IsFacingLeft(),
            firingTargetObject,
            newProjectileId);

        yield return new WaitForSeconds(.35f);
        animator.SetBool("firing", false);
        isFiring = false;
    }

    private IEnumerator FirePowerUp(GameObject projectile, string projectileId)
    {
        isFiring = true;
        animator.SetBool("firing", true);
        stop();

        var newProjectileId = projectileId ?? Projectile.NewId(playerNumber);
        if (isLocal)
        {
            var projectileType = projectile.GetComponentInChildren<Projectile>().projectileTypeId;
            NetworkManager.singleton.ProjectileFired(
                playerNumber,
                firingTargetPosition,
                0f,
                projectileType,
                firingTargetObject,
                newProjectileId);
            OnProjectileFired?.Invoke();
        }

        yield return new WaitForSeconds(TIME_ANIMATION_PRE_FIRE);

        var newProjectile = Instantiate(projectile, transform.root.position, Quaternion.identity);
        var projectileBehaviour = newProjectile.GetComponentInChildren<Projectile>();
        projectileBehaviour.FirePowerUp(
            this,
            playerNumber,
            isLocal,
            newProjectileId);

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

        var newVelocityX = Mathf.Max(currentVelocityRunning, velocityToReach);
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

    private void stopWhenArrivedToDestinationOrKeepChasing()
    {
        var distanceToDestination = movingDestination - transform.position.x;
        var direction = Mathf.Sign(myRigidbody.velocity.x);
        var arrived = direction > 0 && distanceToDestination <= 0
            || direction < 0 && distanceToDestination >= 0;
        if (!arrived) return;

        if (isLocal && firingTargetPosition != Vector3.zero)
        {
            ChaseAndFireToPosition(firingTargetPosition);
        }
        else stop();
    }

    private void stop()
    {
        movingDestination = 0f;
        setVelocityX(0);
    }

    private void setVelocityX(float x)
    {
        myRigidbody.velocity = new Vector2(x, 0);
        animator.SetFloat("velocityX", myRigidbody.velocity.x);
        animator.SetFloat("absVelocityX", Math.Abs(myRigidbody.velocity.x));

        flipIfNeeded();
    }

    private void flipIfNeeded()
    {
        var facingLeft = IsFacingLeft();
        if (myRigidbody.velocity.x > 0 && facingLeft
            || myRigidbody.velocity.x < 0 && !facingLeft)
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
        if (float.IsNaN(positionX)) return;
        if (isLocal && isFiring) return;

        movingDestination = positionX;
        moveToCurrentDestination(float.MaxValue);

        var distance = Mathf.Abs(movingDestination - transform.position.x);
        var timeToReach = distance / currentVelocityRunning;
        NetworkManager.singleton.NewLocalPlayerDestination(movingDestination, timeToReach);
    }

    public void Remote_NewDestination(float newDestination, float timeToReach)
    {
        movingDestination = newDestination;
        moveToCurrentDestination(timeToReach);
    }

    public void Remote_FireProjectile(GameObject projectile, Vector3 destination, float timeToReach, GameObject targetObject, string projectileId)
    {
        var isPowerUp = TryFirePowerUp(projectile, projectileId);
        if (isPowerUp) return;

        firingTargetObject = targetObject;
        firingTargetPosition = destination;
        timeToReach -= TIME_ANIMATION_PRE_FIRE;

        var velocity = projectile.GetComponentInChildren<Projectile>().AimAtTargetAnyVelocity(transform.position, firingTargetPosition);
        var immediate = timeToReach <= 0;

        StartCoroutine(Fire(projectile, velocity, immediate, projectileId));
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

        var other = collision.transform.root.gameObject;
        if (uribeIsHelping)
        {
            var voter = other.GetComponentInChildren<IPartySupporter>();
            if (voter != null) voter.TryConvertTo(playerNumber, isLocal);
        }

        var collectable = other.GetComponentInChildren<ICollectable>();
        if (collectable == null) return;

        var byForce = uribeIsHelping;
        collectable.TryClaim(playerNumber, byForce);
    }

    internal void ChangeProjectile(GameObject projectile)
    {
        currentProjectilePrefab = projectile;
        stop();

        TryFirePowerUp(projectile);
    }

    private bool TryFirePowerUp(GameObject projectile, string projectileId = null)
    {
        var isPowerUp = projectile.GetComponentInChildren<IProjectile>().IsPowerUp();
        if (!isPowerUp) return false;

        firingTargetObject = null;
        firingTargetPosition = transform.root.position;
        StartCoroutine(FirePowerUp(projectile, projectileId));
        return true;
    }

    internal void AddVotes(int votes)
    {
        NetworkManager.singleton.AddVotes(playerNumber, votes);
    }

    internal void OnVotesChanges(int votes)
    {
        votesCount += votes;

        var position = transform.position;
        position.y += 2f;
        var votesIndicator = Index.singleton.votesChangesIndicatorPool.Spawn<VotesChangesBehaviour>(position, Quaternion.identity);
        votesIndicator.Show(votes);
    }

    private void OnMouseDown()
    {
        if (Common.IsPointerOverUIObject()) return;

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
            CurrentTargetIndicatorBehaviour.singleton.NewTargetPosition(position);
            return;
        }

        firingTargetObject = target;
        CurrentTargetIndicatorBehaviour.singleton.NewTargetObject(target);
        ChaseAndFireToPosition(position);
    }

    internal void PartyChose(Common.Parties party)
    {
        this.party = party;
        headsPrefabs[0].SetActive(false);
        headsPrefabs[(int)party].SetActive(true);
        headsPrefabs[(int)party].GetComponent<SpriteRenderer>().color = Common.playerColors[playerNumber];

        var animationPosition = transform.position;
        animationPosition.y += 2.5f;
        Instantiate(changeHeadAnimationPrefab, animationPosition, Quaternion.identity);

        audioSource.pitch = 1.5f;
        audioSource.PlayOneShot(partyIntroductions[(int)party]);
    }

    internal IEnumerator NewAlly(Common.Projectiles projectileType)
    {
        var clip = projectileIntroductions[(int)projectileType];
        if (clip == null) yield break;

        yield return new WaitForSeconds(Common.NEW_ALLY_CLIP_DURATION);
        audioSource.PlayOneShot(clip);
    }

    internal int DoDamagePercentage(float playerDamagePercentage)
    {
        var votesLost = (int)Mathf.Ceil(votesCount * playerDamagePercentage);
        AddVotes(-votesLost);
        return votesLost;
    }

    internal void UribeIsHelping()
    {
        currentVelocityRunning = DEFAULT_VELOCITY_RUNNING * 1.2f;
        uribeIsHelping = true;
    }

    internal void UribeIsNotHelpingAnymore()
    {
        currentVelocityRunning = DEFAULT_VELOCITY_RUNNING;
        uribeIsHelping = false;
    }

    internal void BeTransparent(bool isLocal)
    {
        transform.root.gameObject.layer = LayerMask.NameToLayer("PlayerTransparent");
        foreach (var child in GetComponentsInChildren<SpriteRenderer>())
        {
            var color = child.color;
            color.a = isLocal ? 0.25f : 0f;
            child.color = color;
        }
    }

    internal void StopBeingTransparent()
    {
        transform.root.gameObject.layer = LayerMask.NameToLayer("Player");
        foreach (var child in GetComponentsInChildren<SpriteRenderer>())
        {
            var color = child.color;
            color.a = 1f;
            child.color = color;
        }
    }
}
