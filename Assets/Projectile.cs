using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float projectileVelocity;
    public float initialPositionOffsetY = 3f;
    public GameObject endAnimationPrefab;

    private int playerOwner;

    public Vector3 AimAtTarget(Transform origin, Vector3 currentTarget, float minVelocity)
    {
        var velocity = Mathf.Max(projectileVelocity, minVelocity);
        var distanceX = Mathf.Abs(origin.position.x - currentTarget.x);
        var distanceY = currentTarget.y - origin.position.y - initialPositionOffsetY;
        var shootingAngle = CalcAngle(velocity, distanceX, distanceY);
        if (float.IsNaN(shootingAngle)) return Vector3.zero;

        var velocityX = velocity * Mathf.Cos(shootingAngle);
        var velocityY = velocity * Mathf.Sin(shootingAngle);
        var velocityVector = new Vector3(velocityX, velocityY, 0f);

        return velocityVector;
    }

    public void FireProjectile(
        int playerNumber,
        Transform player,
        Vector3 velocity,
        bool toTheLeft)
    {
        playerOwner = playerNumber;

        var position = player.position;
        position.y += initialPositionOffsetY;
        transform.position = position;

        var direction = toTheLeft ? -1 : 1;
        velocity.x *= direction;
        GetComponent<Rigidbody2D>().velocity = velocity;
    }

    float CalcAngle(float v, float x, float y)
    {
        var g = Mathf.Abs(Physics2D.gravity.y);
        var a = (g * x * x) / (2 * v * v);
        var b = -x;
        var c = a + y;
        var (x1, x2) = Quadratic(a, b, c);
        var angle1 = Mathf.Atan(x1);
        var angle2 = Mathf.Atan(x2);
        return Mathf.Min(angle1, angle2);
    }

    public float CalcMaxReach(float offsetY)
    {
        var v = projectileVelocity;
        var g = Mathf.Abs(Physics2D.gravity.y);
        var y = offsetY + initialPositionOffsetY;
        return (v / g) * Mathf.Sqrt(v * v + 2 * g * y);
    }

    (float x1, float x2) Quadratic(float a, float b, float c)
    {
        var x1 = (-b + Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);
        var x2 = (-b - Mathf.Sqrt(b * b - 4 * a * c)) / (2 * a);
        return (x1, x2);
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        var voter = other.gameObject.GetComponent<VoterBehaviour>();
        if (voter == null) return;
        voter.ConvertTo(playerOwner);

        var endAnimation = Instantiate(endAnimationPrefab);
        endAnimation.transform.position = other.transform.position;

        Destroy(gameObject);
    }
}
