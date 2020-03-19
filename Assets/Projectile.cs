using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float projectileVelocity;
    public float initialPositionOffsetY = 3f;

    private PlayerBehaviour owner;

    public Vector3 AimAtTarget(Transform origin, Transform currentTarget)
    {
        var distanceX = Mathf.Abs(origin.position.x - currentTarget.position.x);
        var distanceY = currentTarget.position.y - origin.position.y - initialPositionOffsetY;
        var shootingAngle = CalcAngle(projectileVelocity, distanceX, distanceY);
        if (float.IsNaN(shootingAngle)) return Vector3.zero;

        var velocityX = projectileVelocity * Mathf.Cos(shootingAngle);
        var velocityY = projectileVelocity * Mathf.Sin(shootingAngle);
        var velocityVector = new Vector3(velocityX, velocityY, 0f);

        return velocityVector;
    }

    public void FireProjectile(Transform from, Vector3 velocity, bool toTheLeft)
    {
        owner = from.GetComponent<PlayerBehaviour>();

        var projectile = Instantiate(gameObject);
        var position = from.position;
        position.y += initialPositionOffsetY;
        projectile.transform.position = position;

        var direction = toTheLeft ? -1 : 1;
        velocity.x *= direction;
        projectile.GetComponent<Rigidbody2D>().velocity = velocity;
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
        Debug.Log("hit!");
    }
}
