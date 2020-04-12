using UnityEngine;

interface IProjectile
{
    bool IsPowerUp();
    bool CanYouFireAt(Vector3 position, GameObject target);
}
