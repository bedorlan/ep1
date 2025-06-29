﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CentralBaseBehaviour : MonoBehaviour, IProjectile
{
    public Common.Parties party;

    private int playerOwnerNumber;

    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        return target == null || target.GetComponentInChildren<CentralBaseBehaviour>() == null;
    }

    public bool IsPowerUp()
    {
        return false;
    }

    private void Start()
    {
        playerOwnerNumber = GetComponent<Projectile>().playerOwnerNumber;
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
        rigidbody.bodyType = RigidbodyType2D.Static;
    }
}
