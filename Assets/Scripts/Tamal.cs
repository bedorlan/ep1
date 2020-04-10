using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tamal : MonoBehaviour, IProjectile
{
    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        var validTarget = target != null && target.GetComponentInChildren<IPartySupporter>() != null;
        return validTarget;
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        var voter = other.gameObject.GetComponentInChildren<IPartySupporter>();
        if (voter == null) return;

        var projectile = gameObject.GetComponent<Projectile>();
        var playerOwner = projectile.playerOwner;
        var isLocal = projectile.isLocal;
        var endAnimationPrefab = projectile.endAnimationPrefab;

        voter.TryConvertTo(playerOwner, isLocal);

        var endAnimation = Instantiate(endAnimationPrefab);
        endAnimation.transform.position = other.transform.position;

        Destroy(transform.root.gameObject);
    }
}
