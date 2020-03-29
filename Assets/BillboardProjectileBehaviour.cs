using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardProjectileBehaviour : MonoBehaviour, IProjectile
{
    public GameObject playerFace;

    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        var validTarget = target != null && target.GetComponentInChildren<PlayerBehaviour>() != null;
        return validTarget;
    }

    private void Start()
    {
        var targetObject = GetComponent<Projectile>().targetObject;
        var playerTarget = targetObject.GetComponent<PlayerBehaviour>();
        var playerTargetNumber = playerTarget.GetPlayerNumber();
        var color = Common.playerColors[playerTargetNumber];
        playerFace.GetComponent<SpriteRenderer>().color = color;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var floor = other.GetComponent<Floor>();
        if (floor == null) return;

        var rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.velocity = Vector3.zero;
        rigidbody.simulated = false;

        StartCoroutine(KillSelfAfter(7));
    }

    private IEnumerator KillSelfAfter(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        Destroy(transform.root.gameObject);
    }
}
