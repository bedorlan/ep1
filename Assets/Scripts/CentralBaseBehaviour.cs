using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CentralBaseBehaviour : MonoBehaviour, IProjectile
{
    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        return true;
    }

    private void Start()
    {
        var playerOwner = GetComponent<Projectile>().playerOwner;
        GetComponent<SpriteRenderer>().color = Common.playerColors[playerOwner];
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var floor = other.GetComponent<Floor>();
        if (floor == null) return;

        var rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.velocity = Vector3.zero;
        rigidbody.bodyType = RigidbodyType2D.Static;

        GetComponent<Collider2D>().enabled = false;
    }
}
