using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lechona : MonoBehaviour, IProjectile
{
    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        var validTarget = target != null && target.GetComponentInChildren<VoterBehaviour>() != null;
        return validTarget;
    }

    private List<VoterBehaviour> votersAtRange = new List<VoterBehaviour>();
    private bool onTheFloor = false;

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Floor>() != null)
        {
            onTheFloor = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        var voter = other.GetComponent<VoterBehaviour>();
        if (voter != null)
        {
            votersAtRange.Add(voter);
        }
    }

    private void Update()
    {
        if (!onTheFloor)
        {
            votersAtRange.Clear();
            return;
        }

        var projectile = gameObject.GetComponent<Projectile>();
        var playerOwner = projectile.playerOwner;
        var isLocal = projectile.isLocal;
        var endAnimationPrefab = projectile.endAnimationPrefab;

        foreach (var voter in votersAtRange)
        {
            voter.TryConvertTo(playerOwner, isLocal);
        }

        var endAnimation = Instantiate(endAnimationPrefab);
        endAnimation.transform.position = transform.position;
        endAnimation.transform.localScale.Scale(new Vector3(2, 2, 1));

        Destroy(transform.root.gameObject);
    }
}
