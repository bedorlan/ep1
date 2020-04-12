using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Avocado : MonoBehaviour, IProjectile
{
    public float damagePercentage;

    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        if (target == null) return false;

        var voter = target.GetComponentInChildren<IPartySupporter>();
        var player = target.GetComponent<PlayerBehaviour>();
        var validTarget = voter != null || player != null;
        return validTarget;
    }

    public bool IsPowerUp()
    {
        return false;
    }

    private HashSet<IPartySupporter> votersAtRange = new HashSet<IPartySupporter>();
    private HashSet<PlayerBehaviour> playersAtRange = new HashSet<PlayerBehaviour>();
    private Projectile projectile;

    private void Start()
    {
        projectile = GetComponent<Projectile>();
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Floor>() != null)
        {
            Explode();
            return;
        }

        var voter = other.GetComponentInChildren<IPartySupporter>();
        if (voter != null)
        {
            votersAtRange.Add(voter);
            return;
        }

        var player = other.GetComponent<PlayerBehaviour>();
        if (player != null && player.GetPlayerNumber() != projectile.playerOwnerNumber)
        {
            playersAtRange.Add(player);
        }
    }

    private void Explode()
    {
        if (projectile.isLocal)
        {
            foreach (var player in playersAtRange)
            {
                player.DoDamagePercentage(damagePercentage);
            }
        }

        foreach (var voter in votersAtRange)
        {
            voter.TryConvertTo(projectile.playerOwnerNumber, projectile.isLocal);
        }

        Instantiate(projectile.endAnimationPrefab, transform.position, Quaternion.identity);
        Destroy(transform.root.gameObject);
    }
}
