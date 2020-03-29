using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardProjectileBehaviour : MonoBehaviour, IProjectile
{
    public GameObject playerFace;

    private HashSet<VoterBehaviour> votersUnderInfluence = new HashSet<VoterBehaviour>();

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
        var voter = other.GetComponent<VoterBehaviour>();
        if (voter != null)
        {
            votersUnderInfluence.Add(voter);
            voter.BeIndifferent();
            return;
        }

        var floor = other.GetComponent<Floor>();
        if (floor == null) return;

        var rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.velocity = Vector3.zero;
        rigidbody.bodyType = RigidbodyType2D.Kinematic;

        StartCoroutine(StopInfluenceAfter(7));
    }

    private IEnumerator StopInfluenceAfter(int seconds)
    {
        yield return new WaitForSeconds(seconds);

        foreach (var voter in votersUnderInfluence)
        {
            voter.StopBeingIndifferent();
        }

        Destroy(transform.root.gameObject);
    }
}
