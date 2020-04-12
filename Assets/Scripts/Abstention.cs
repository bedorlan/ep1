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

    private Projectile projectile;

    private void Start()
    {
        projectile = GetComponent<Projectile>();
    }

    public void Fire()
    {
        Debug.Log("Firing abstention");
    }
}
