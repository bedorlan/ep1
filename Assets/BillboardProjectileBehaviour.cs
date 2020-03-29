using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardProjectileBehaviour : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        var floor = other.GetComponent<Floor>();
        if (floor == null) return;

        var rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.velocity = Vector3.zero;
        rigidbody.simulated = false;

        StartCoroutine(KillSelfAfter(5));
    }

    private IEnumerator KillSelfAfter(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        Destroy(transform.root.gameObject);
    }
}
