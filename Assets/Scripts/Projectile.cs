using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float projectileVelocity;
    public float initialPositionOffsetY = 3f;
    public GameObject endAnimationPrefab;
    public int projectileTypeId;

    internal bool isLocal;
    internal int playerOwner;

    public Vector3 AimAtTarget(Transform origin, Vector3 currentTarget, float minVelocity)
    {
        var velocity = Mathf.Max(projectileVelocity, minVelocity);
        var distanceX = Mathf.Abs(origin.position.x - currentTarget.x);
        var distanceY = currentTarget.y - origin.position.y - initialPositionOffsetY;
        var shootingAngle = MyMath.CalcAngle(velocity, distanceX, distanceY);
        if (float.IsNaN(shootingAngle)) return Vector3.zero;

        var velocityX = velocity * Mathf.Cos(shootingAngle);
        var velocityY = velocity * Mathf.Sin(shootingAngle);
        var velocityVector = new Vector3(velocityX, velocityY, 0f);

        return velocityVector;
    }

    public Vector3 AimAtTargetAnyVelocity(Transform origin, Vector3 currentTarget)
    {
        var distanceX = Mathf.Abs(origin.position.x - currentTarget.x);
        var distanceY = currentTarget.y - origin.position.y - initialPositionOffsetY;
        var minVelocity = MyMath.CalcMinVelocity(distanceX, distanceY);
        var velocityVector = AimAtTarget(origin, currentTarget, minVelocity);
        return velocityVector;
    }

    public void FireProjectile(
        int playerNumber,
        bool isLocal,
        Transform player,
        Vector3 velocity,
        bool toTheLeft)
    {
        playerOwner = playerNumber;
        this.isLocal = isLocal;

        var position = player.position;
        position.y += initialPositionOffsetY;
        transform.position = position;

        var direction = toTheLeft ? -1 : 1;
        velocity.x *= direction;
        GetComponent<Rigidbody2D>().velocity = velocity;
    }

    public void FireProjectileImmediate(int playerNumber, bool isLocal, Vector3 currentTarget)
    {
        playerOwner = playerNumber;
        this.isLocal = isLocal;
        transform.position = currentTarget;
    }

    public float CalcMaxReach(float offsetY)
    {
        var y = offsetY + initialPositionOffsetY;
        return MyMath.CalcMaxReach(projectileVelocity, y);
    }
}
