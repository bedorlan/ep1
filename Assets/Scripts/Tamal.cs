using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tamal : MonoBehaviour
{
    public void OnTriggerEnter2D(Collider2D other)
    {
        var projectile = gameObject.GetComponent<Projectile>();
        var playerOwner = projectile.playerOwner;
        var isLocal = projectile.isLocal;
        var endAnimationPrefab = projectile.endAnimationPrefab;

        var voter = other.gameObject.GetComponent<VoterBehaviour>();
        if (voter == null) return;
        voter.TryConvertTo(playerOwner, isLocal);

        var endAnimation = Instantiate(endAnimationPrefab);
        endAnimation.transform.position = other.transform.position;

        Destroy(transform.root.gameObject);
    }
}
