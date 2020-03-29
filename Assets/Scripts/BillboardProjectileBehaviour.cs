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
        if (floor == null) return;

        var rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.velocity = Vector3.zero;
        rigidbody.bodyType = RigidbodyType2D.Kinematic;

        StartCoroutine(StopInfluenceAfter(7));
        if (isLocal) StartCoroutine(SubstractTargetPlayerVotes());
    }

    private IEnumerator StopInfluenceAfter(int seconds)
    {
        yield return new WaitForSeconds(seconds);

        foreach (var voter in votersUnderInfluence)
        {
            voter.StopBeingIndifferent();
        }

        StopAllCoroutines();
        Destroy(transform.root.gameObject);
    }

    private IEnumerator SubstractTargetPlayerVotes()
    {
        while (true)
        {
            var atRange = myCollider.bounds.Contains(playerTarget.transform.root.position);
            if (atRange) playerTarget.AddVotes(-1);

            yield return new WaitForSeconds(.5f);
        }
    }
}
