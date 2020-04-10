using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlazaBoss : MonoBehaviour, IProjectile, IPartySupporter
{
    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        return true;
    }

    private Projectile projectile;
    Queue<GameObject> votersInRange = new Queue<GameObject>();
    private bool alive = true;

    private void Start()
    {
        projectile = GetComponent<Projectile>();
        GetComponent<SpriteRenderer>().color = Common.playerColors[projectile.playerOwner];

        if (projectile.isLocal)
        {
            StartCoroutine(StartConvertingOthers());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var floor = collision.GetComponent<Floor>();
        var voter = collision.GetComponent<IPartySupporter>();

        if (floor != null)
        {
            var rigidbody = GetComponent<Rigidbody2D>();
            rigidbody.bodyType = RigidbodyType2D.Static;
            rigidbody.velocity = Vector2.zero;
        }
        else if (voter != null)
        {
            votersInRange.Enqueue(collision.gameObject);
        }
    }

    private IEnumerator StartConvertingOthers()
    {
        while (alive)
        {
            yield return new WaitForSeconds(1f);

            GameObject voter = null;
            while (votersInRange.Count > 0 && voter == null)
            {
                voter = votersInRange.Dequeue();
            }
            if (voter == null) continue;

            voter.GetComponent<IPartySupporter>().TryConvertTo(projectile.playerOwner, projectile.isLocal);
            var collectable = voter.GetComponent<ICollectable>();
            if (collectable == null) continue;

            yield return new WaitForSeconds(1f);
            collectable.TryClaim(projectile.playerOwner);
        }
    }

    public bool TryConvertTo(int playerOwner, bool isLocal)
    {
        if (!isLocal || playerOwner == projectile.playerOwner) return false;

        alive = false;
        Instantiate(projectile.endAnimationPrefab, transform.position, Quaternion.identity);
        Destroy(transform.root.gameObject);
        return true;
    }

    public void TryConvertAndConvertOthers(int playerOwner, bool isLocal)
    {
        TryConvertTo(playerOwner, isLocal);
    }
}
