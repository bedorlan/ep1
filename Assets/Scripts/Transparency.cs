using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Transparency : MonoBehaviour, IProjectile, IPowerUp, IDestroyable
{
    public bool IsPowerUp()
    {
        return true;
    }

    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        return false;
    }

    private Projectile projectile;

    public void FirePowerUp()
    {
        projectile = GetComponent<Projectile>();
        Projectile.RegisterProjectile(projectile.projectileId, transform.root.gameObject);

        MagicFlame.createFlameBurst(projectile.playerOwner.transform.root.gameObject, 0f);

        projectile.playerOwner.BeTransparent(projectile.isLocal);
        projectile.playerOwner.OnProjectileFired += Player_OnProjectileFired;
    }

    private void Player_OnProjectileFired()
    {
        projectile.playerOwner.OnProjectileFired -= Player_OnProjectileFired;
        projectile.playerOwner.StopBeingTransparent();

        NetworkManager.singleton.DestroyProjectile(projectile.projectileId);
    }

    public void Destroy()
    {
        // this method is remote called only
        projectile.playerOwner.StopBeingTransparent();
    }
}
