using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Uribe : MonoBehaviour, IProjectile, IPowerUp
{
    public int powerUpDuration;

    public bool IsPowerUp()
    {
        return true;
    }

    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        return false;
    }

    private PlayerBehaviour playerOwner;
    private MagicFlame powerUp;

    public void FirePowerUp()
    {
        playerOwner = GetComponent<Projectile>().playerOwner;
        powerUp = MagicFlame.createFlameUribe(playerOwner.transform.root.gameObject);

        playerOwner.UribeIsHelping();
        StartCoroutine(Shutdown());
    }

    private IEnumerator Shutdown()
    {
        yield return new WaitForSeconds(powerUpDuration);

        powerUp.Shutdown();
        playerOwner.UribeIsNotHelpingAnymore();
    }
}
