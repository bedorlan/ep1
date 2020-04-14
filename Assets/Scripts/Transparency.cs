using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Transparency : MonoBehaviour, IProjectile, IPowerUp
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
        MagicFlame.createFlameBurst(projectile.playerOwner.transform.root.gameObject, 0f);

        projectile.playerOwner.BeTransparent(projectile.isLocal);
    }
}
