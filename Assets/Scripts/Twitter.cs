using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Twitter : MonoBehaviour, IProjectile, IManuallyFiredProjectile
{
    public int maxInfluence;
    public float playerDamagePercentage;

    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        var validTarget = target != null && (
            target.GetComponentInChildren<IPartySupporter>() != null
            || target.GetComponent<PlayerBehaviour>() != null
        );
        return validTarget;
    }

    public bool IsPowerUp()
    {
        return false;
    }

    public Vector3 AimAtTarget(float distanceX, float distanceY, float minVelocity)
    {
        var velocity = new Vector3(distanceX, distanceY, 0f);
        velocity.Normalize();
        velocity *= minVelocity;
        return velocity;
    }

    public void FireProjectile()
    {
        var rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;

        var direction = Mathf.Sign(rigidbody.velocity.x);
        var scale = transform.localScale;
        scale.x *= direction;
        transform.localScale = scale;

        Destroy(transform.root.gameObject, 10f);
    }

    public void FireProjectileImmediate(Vector3 origin, Vector3 target)
    {
        var rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;

        var projectile = GetComponent<Projectile>();
        var velocity = projectile.AimAtTarget(origin, target, 99f);
        var direction = Mathf.Sign(target.x - origin.x);
        velocity.Normalize();
        velocity.x *= direction;
        velocity *= projectile.projectileVelocity;
        rigidbody.velocity = velocity;

        var scale = transform.localScale;
        scale.x *= direction;
        transform.localScale = scale;

        Destroy(transform.root.gameObject, 10f);
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (maxInfluence <= 0) return;

        var player = other.gameObject.GetComponent<PlayerBehaviour>();
        var voter = other.gameObject.GetComponentInChildren<IPartySupporter>();
        var projectile = GetComponent<Projectile>();

        if (player != null)
        {
            if (player.GetPlayerNumber() == projectile.playerOwnerNumber) return;
            if(projectile.isLocal) player.DoDamagePercentage(playerDamagePercentage);
            maxInfluence = 0;
        }
        else if (voter != null)
        {
            var playerOwner = projectile.playerOwnerNumber;
            var isLocal = projectile.isLocal;

            if (voter.TryConvertTo(playerOwner, isLocal))
            {
                --maxInfluence;
            }
        }
        else return;

        if (maxInfluence > 0) return;

        var endAnimationPrefab = projectile.endAnimationPrefab;
        var endAnimation = Instantiate(endAnimationPrefab);
        endAnimation.transform.position = other.transform.position;
        Destroy(transform.root.gameObject);
    }
}
