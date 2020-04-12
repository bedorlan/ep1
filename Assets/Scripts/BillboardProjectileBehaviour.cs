using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardProjectileBehaviour : MonoBehaviour, IProjectile
{
    private bool isLocal;
    private PlayerBehaviour playerTarget;
    private int playerTargetNumber;
    private bool alive = true;
    private bool playerAtRange = false;

    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        var validTarget = target != null && target.GetComponentInChildren<PlayerBehaviour>() != null;
        return validTarget;
    }

    public bool IsPowerUp()
    {
        return false;
    }

    private void Start()
    {
        var projectile = GetComponent<Projectile>();
        isLocal = projectile.isLocal;
        playerTarget = projectile.targetObject.GetComponent<PlayerBehaviour>();
        playerTargetNumber = playerTarget.GetPlayerNumber();

        var party = playerTarget.party;
        var playerFace = transform.root.GetChild((int)party).gameObject;
        var color = Common.playerColors[playerTargetNumber];
        playerFace.SetActive(true);
        playerFace.GetComponent<SpriteRenderer>().color = color;

        StartCoroutine(StopInfluenceAfter(9));
        if (isLocal) StartCoroutine(SubstractTargetPlayerVotes());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var voter = other.GetComponent<VoterBehaviour>();
        if (isLocal && voter != null)
        {
            voter.BeUndecided(isLocal);
        }

        var player = other.GetComponent<PlayerBehaviour>();
        if (player != null && player.GetPlayerNumber() == playerTargetNumber)
        {
            playerAtRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var player = collision.GetComponent<PlayerBehaviour>();
        if (player != null && player.GetPlayerNumber() == playerTargetNumber)
        {
            playerAtRange = false;
        }
    }

    private IEnumerator StopInfluenceAfter(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        alive = false;
        Destroy(transform.root.gameObject, 1f);
    }

    private IEnumerator SubstractTargetPlayerVotes()
    {
        while (alive)
        {
            if (playerAtRange) playerTarget.DoDamagePercentage(0.05f);
            yield return new WaitForSeconds(.5f);
        }

    }
}
