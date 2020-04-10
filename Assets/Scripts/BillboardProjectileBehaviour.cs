using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardProjectileBehaviour : MonoBehaviour, IProjectile
{
    public GameObject playerFace;

    private bool isLocal;
    private PlayerBehaviour playerTarget;
    private int playerTargetNumber;
    private HashSet<VoterBehaviour> votersUnderInfluence = new HashSet<VoterBehaviour>();
    private BoxCollider2D myCollider;
    private bool influencing = false; // todo: this should be an int

    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        var validTarget = target != null && target.GetComponentInChildren<PlayerBehaviour>() != null;
        return validTarget;
    }

    private void Start()
    {
        var projectile = GetComponent<Projectile>();
        var targetObject = projectile.targetObject;
        isLocal = projectile.isLocal;
        playerTarget = targetObject.GetComponent<PlayerBehaviour>();
        playerTargetNumber = playerTarget.GetPlayerNumber();
        myCollider = GetComponent<BoxCollider2D>();

        var color = Common.playerColors[playerTargetNumber];
        playerFace.GetComponent<SpriteRenderer>().color = color;
    }

    // todo: do not use huge collider. easy to hit
    private void OnTriggerEnter2D(Collider2D other)
    {
        var voter = other.GetComponent<VoterBehaviour>();
        if (voter != null)
        {
            votersUnderInfluence.Add(voter);
            voter.BeIndifferent();
            return;
        }

        var floor = other.GetComponent<Floor>();
        if (floor == null || influencing) return;
        influencing = true;

        var rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Static;

        StartCoroutine(StopInfluenceAfter(7));
        if (isLocal) StartCoroutine(SubstractTargetPlayerVotes());
    }

    private IEnumerator StopInfluenceAfter(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        influencing = false;

        foreach (var voter in votersUnderInfluence)
        {
            voter.StopBeingIndifferent();
        }

        Destroy(transform.root.gameObject, 1f);
    }

    private IEnumerator SubstractTargetPlayerVotes()
    {
        while (influencing)
        {
            var atRange = myCollider.bounds.Contains(playerTarget.transform.root.position);
            if (atRange) playerTarget.AddVotes(-1);

            yield return new WaitForSeconds(.5f);
        }

    }
}
