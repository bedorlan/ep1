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
    private EdgeCollider2D myCollider;
    private bool alive = true;

    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        var validTarget = target != null && target.GetComponentInChildren<PlayerBehaviour>() != null;
        return validTarget;
    }

    private void Start()
    {
        var projectile = GetComponent<Projectile>();
        isLocal = projectile.isLocal;
        playerTarget = projectile.targetObject.GetComponent<PlayerBehaviour>();
        playerTargetNumber = playerTarget.GetPlayerNumber();
        myCollider = GetComponent<EdgeCollider2D>();

        var color = Common.playerColors[playerTargetNumber];
        playerFace.GetComponent<SpriteRenderer>().color = color;

        StartCoroutine(StopInfluenceAfter(9));
        if (isLocal) StartCoroutine(SubstractTargetPlayerVotes());
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
    }

    private IEnumerator StopInfluenceAfter(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        alive = false;

        foreach (var voter in votersUnderInfluence)
        {
            voter.StopBeingIndifferent();
        }

        Destroy(transform.root.gameObject, 1f);
    }

    private IEnumerator SubstractTargetPlayerVotes()
    {
        while (alive)
        {
            var atRange = myCollider.bounds.Contains(playerTarget.transform.root.position);
            if (atRange) playerTarget.AddVotes(-1);

            yield return new WaitForSeconds(.5f);
        }

    }
}
