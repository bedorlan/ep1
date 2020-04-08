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
            Debug.Log("r=" + r);
            Debug.Log("x=" + velocity.x);
            Debug.Log("w=" + w);
            rigidbody.angularVelocity = Mathf.Rad2Deg * w;
        }
    }
}
