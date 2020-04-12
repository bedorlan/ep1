using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gavel : MonoBehaviour, IProjectile
{
    public float damagePercentage;

    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        return target != null && target.GetComponent<PlayerBehaviour>() != null;
    }

    public bool IsPowerUp()
    {
        return false;
    }

    private Projectile projectile;

    private void Start()
    {
        projectile = GetComponent<Projectile>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var player = collision.GetComponent<PlayerBehaviour>();
        if (player == null || player.GetPlayerNumber() == projectile.playerOwnerNumber) return;

        if (projectile.isLocal)
        {
            var votesRemoved = player.DoDamagePercentage(damagePercentage);
            projectile.playerOwner.AddVotes(votesRemoved);
        }

        Instantiate(projectile.endAnimationPrefab, transform.root.position, Quaternion.identity);
        Destroy(transform.root.gameObject);
    }
}
