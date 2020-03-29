using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lechona : MonoBehaviour, IProjectile
{
    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        var validTarget = target != null && target.GetComponentInChildren<VoterBehaviour>() != null;
        return validTarget;
    }

    private HashSet<VoterBehaviour> votersAtRange = new HashSet<VoterBehaviour>();

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Floor>() != null)
        {
            Explode();
            return;
        }

        var voter = other.GetComponent<VoterBehaviour>();
        if (voter != null)
        {
            votersAtRange.Add(voter);
        }
    }

    private void Explode()
    {
        var projectile = gameObject.GetComponent<Projectile>();
        var playerOwner = projectile.playerOwner;
        var isLocal = projectile.isLocal;
        var endAnimationPrefab = projectile.endAnimationPrefab;

        foreach (var voter in votersAtRange)
        {
            voter.TryConvertTo(playerOwner, isLocal);
        }

        var endAnimation = Instantiate(endAnimationPrefab);
        endAnimation.transform.position = transform.position;
        endAnimation.transform.localScale.Scale(new Vector3(2, 2, 1));

        Destroy(transform.root.gameObject);
    }
}
