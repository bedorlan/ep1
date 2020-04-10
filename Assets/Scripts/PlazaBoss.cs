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
        projectile = transform.root.GetComponent<Projectile>();
        transform.root.GetComponent<SpriteRenderer>().color = Common.playerColors[projectile.playerOwner];

        if (projectile.isLocal)
        {
            StartCoroutine(StartConvertingOthers());
        }

        Projectile.RegisterProjectile(projectile.projectileId, gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var voter = collision.GetComponentInChildren<IPartySupporter>();
        if (voter == null) return;

        votersInRange.Enqueue(collision.gameObject);
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

            voter.GetComponentInChildren<IPartySupporter>().TryConvertTo(projectile.playerOwner, projectile.isLocal);
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
        NetworkManager.singleton.DestroyProjectile(projectile.projectileId);
        Instantiate(projectile.endAnimationPrefab, transform.position, Quaternion.identity);
        Destroy(transform.root.gameObject);
        return true;
    }

    public void TryConvertAndConvertOthers(int playerOwner, bool isLocal)
    {
        TryConvertTo(playerOwner, isLocal);
    }
}
