using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardProjectileBehaviour : MonoBehaviour, IProjectile, IPowerUp
{
    public bool IsPowerUp()
    {
        return true;
    }

    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        return false;
    }

    private Projectile projectile;
    private HashSet<PlayerBehaviour> playersAtRange = new HashSet<PlayerBehaviour>();
    private bool alive = true;

    private void Start()
    {
        projectile = GetComponent<Projectile>();

        var position = transform.position;
        position.y = 0f;
        transform.position = position;

        StartCoroutine(StopInfluenceAfter(9f));
        if (projectile.isLocal) StartCoroutine(SubstractEnemyPlayerVotes());
    }

    public void FirePowerUp() {}

    private void OnTriggerEnter2D(Collider2D other)
    {
        var voter = other.GetComponent<VoterBehaviour>();
        if (projectile.isLocal && voter != null)
        {
            voter.BeUndecided(projectile.isLocal);
            return;
        }

        var player = other.GetComponent<PlayerBehaviour>();
        if (player != null && player.GetPlayerNumber() != projectile.playerOwnerNumber)
        {
            playersAtRange.Add(player);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var player = collision.GetComponent<PlayerBehaviour>();
        if (player != null) playersAtRange.Remove(player);
    }

    private IEnumerator StopInfluenceAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        alive = false;
        Destroy(transform.root.gameObject, 1f);
    }

    private IEnumerator SubstractEnemyPlayerVotes()
    {
        while (alive)
        {
            foreach (var playerAtRange in playersAtRange)
            {
                if (playerAtRange) playerAtRange.DoDamagePercentage(0.05f);
            }
            yield return new WaitForSeconds(1f / 3f);
        }

    }
}
