using UnityEngine;

interface IManuallyFiredProjectile
{
    Vector3 AimAtTarget(float distanceX, float distanceY, float minVelocity);
    void FireProjectile();
    void FireProjectileImmediate(Vector3 origin, Vector3 target);
}
