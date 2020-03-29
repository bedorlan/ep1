using UnityEngine;

interface IProjectile
{
    bool CanYouFireAt(Vector3 position, GameObject target);
}
