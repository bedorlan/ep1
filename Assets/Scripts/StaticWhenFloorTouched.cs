using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticWhenFloorTouched : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        var floor = collision.GetComponent<Floor>();
        if (floor == null) return;

        var rigidbody = transform.root.GetComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        rigidbody.velocity = Vector2.zero;
    }
}
