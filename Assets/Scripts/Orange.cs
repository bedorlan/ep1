using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Orange : MonoBehaviour, IProjectile
{
    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        var validTarget = target != null && target.GetComponentInChildren<IPartySupporter>() != null;
        return validTarget;
    }

    private Projectile projectile;
    private GameObject endAnimationPrefab;

    private void Start()
    {
        projectile = gameObject.GetComponent<Projectile>();
        endAnimationPrefab = projectile.endAnimationPrefab;
        StartCoroutine(DestroyAfter(2f));
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var floor = collision.GetComponent<Floor>();
        if (floor != null)
        {
            var rigidbody = GetComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Kinematic;

            var velocity = rigidbody.velocity;
            velocity.y = 0f;
            rigidbody.velocity = velocity;

            var r = GetComponent<CircleCollider2D>().radius;
            var w = -velocity.x / r;
            rigidbody.angularVelocity = Mathf.Rad2Deg * w;

            return;
        }

        var voter = collision.gameObject.GetComponentInChildren<IPartySupporter>();
        if (voter != null)
        {
            voter.TryConvertTo(projectile.playerOwner, projectile.isLocal);
        }
    }

    private IEnumerator DestroyAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        Instantiate(endAnimationPrefab, transform.position, Quaternion.identity);
        Destroy(transform.root.gameObject);
    }
}
