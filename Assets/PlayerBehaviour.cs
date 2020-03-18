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

    private readonly Dictionary<int, Color> playerColors = new Dictionary<int, Color> {
        {0, new Color(1, .5f, .5f) },
        {1, new Color(.5f, .5f, 1) },
        {2, new Color(.5f, 1, .5f) },
        {3, new Color(1, 1, 0) },
    };

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
        moveToCurrentDestination();
    }

    private void fireCurrentTarget()
    {
        if (currentTarget == null) return;
        if (Math.Abs(currentTarget.position.x - transform.position.x) > 10) return;

        StartCoroutine("Fire");
    }

    IEnumerator Fire()
    {
        const float FIRE_ANIM_DURATION = .5F;

        busy = true;
        animator.SetBool("firing", true);
        stop();
        var targetAtMyRight = currentTarget.position.x > transform.position.x;
        if (IsFacingLeft() && targetAtMyRight || !IsFacingLeft() && !targetAtMyRight)
        {
            Flip();
        }
        FireProjectile();
        currentTarget = null;
        yield return new WaitForSeconds(FIRE_ANIM_DURATION);

        animator.SetBool("firing", false);
        busy = false;
    }

    void FireProjectile()
    {
        var projectile = Instantiate(tamalPrefab);
        var position = transform.position;
        var direction = IsFacingLeft() ? -1 : 1;
        position.x += 0f * direction;
        position.y += 3f;
        projectile.transform.position = position;
        var velocity = new Vector3(9f, 4f, 0f);
        velocity.x *= direction;
        projectile.GetComponent<Rigidbody2D>().velocity = velocity;
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
            stop();
            return;
        }

        var newVelocityX = VELOCITY_RUNNING;
        if (currentDestination < transform.position.x)
            newVelocityX *= -1;
        setVelocityX(newVelocityX);
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
            child.color = playerColors[playerNumber];
        }
    }

    public bool IsFacingLeft()
    {
        return transform.localScale.x > 0;
    }

    public void Remote_NewDestination(float newDestination)
    {
        currentDestination = newDestination;
        moveToCurrentDestination();
    }

    public void ChaseVoter(VoterBehaviour voter)
    {
        currentTarget = voter.transform;
        // the method readInputs will set the currentDestination
    }
}
