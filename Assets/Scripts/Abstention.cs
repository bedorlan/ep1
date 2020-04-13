using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abstention : MonoBehaviour, IProjectile, IPowerUp
{
    public bool IsPowerUp()
    {
        return true;
    }

    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        return false;
    }

    public void FirePowerUp()
    {
        var playerOwner = GetComponent<Projectile>().playerOwner;
        MagicFlame.createFlameBurst(playerOwner.transform.root.gameObject);

        // nothing else to do here. the server will detect the projectile
        // and it will start generating voters on my position
    }
}
