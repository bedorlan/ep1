using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CentralBaseBehaviour : MonoBehaviour, IProjectile
{
    public Common.Parties party;

    private int playerOwnerNumber;

    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        var otherBase = target?.GetComponentInChildren<CentralBaseBehaviour>() ?? null;
        return otherBase == null;
    }

    private void Start()
    {
        playerOwnerNumber = GetComponent<Projectile>().playerOwner;
        foreach (var sprite in GetComponentsInChildren<SpriteRenderer>())
        {
            sprite.color = Common.playerColors[playerOwnerNumber];
        }

        NetworkManager.singleton.PartyChose(playerOwnerNumber, party);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var floor = other.GetComponent<Floor>();
        if (floor == null) return;

        var rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.velocity = Vector3.zero;
        rigidbody.bodyType = RigidbodyType2D.Static;
    }
}
