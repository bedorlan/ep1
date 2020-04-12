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

    internal string projectileId { get; private set; }
    internal bool isLocal { get; private set; }
    internal PlayerBehaviour playerOwner { get; private set; }
    internal int playerOwnerNumber { get; private set; }
    internal GameObject targetObject { get; private set; }

    public Vector3 AimAtTarget(Vector3 origin, Vector3 currentTarget, float minVelocity)
    {
        var velocity = Mathf.Max(projectileVelocity, minVelocity);
        var distanceX = Mathf.Abs(origin.x - currentTarget.x);
        var distanceY = currentTarget.y - origin.y - initialPositionOffsetY;

        var manuallyFired = GetComponent<IManuallyFiredProjectile>();
        if (manuallyFired != null)
        {
            return manuallyFired.AimAtTarget(distanceX, distanceY, velocity);
        }


        var shootingAngle = MyMath.CalcAngle(velocity, distanceX, distanceY);
        if (float.IsNaN(shootingAngle)) return Vector3.zero;

        var velocityX = velocity * Mathf.Cos(shootingAngle);
        var velocityY = velocity * Mathf.Sin(shootingAngle);
        var velocityVector = new Vector3(velocityX, velocityY, 0f);

        return velocityVector;
    }

    public Vector3 AimAtTargetAnyVelocity(Vector3 origin, Vector3 currentTarget)
    {
        var distanceX = Mathf.Abs(origin.x - currentTarget.x);
        var distanceY = currentTarget.y - origin.y - initialPositionOffsetY;
        var minVelocity = MyMath.CalcMinVelocity(distanceX, distanceY);
        var velocityVector = AimAtTarget(origin, currentTarget, minVelocity);
        return velocityVector;
    }

    static private int projectileSeq = 0;
    static private Dictionary<string, GameObject> projectiles = new Dictionary<string, GameObject>();

    static internal string NewId(int playerNumber)
    {
        return string.Format("{0}-{1}", playerNumber, projectileSeq++);
    }

    static internal void RegisterProjectile(string projectileId, GameObject projectile)
    {
        projectiles.Add(projectileId, projectile);
    }

    static internal void DestroyProjectile(string projectileId)
    {
        // todo: IDestroyable ?
        Destroy(projectiles[projectileId].transform.root.gameObject);
    }

    public void FireProjectile(
        PlayerBehaviour playerOwner,
        int playerOwnerNumber,
        bool isLocal,
        Transform playerOrigin,
        Vector3 velocity,
        bool toTheLeft,
        GameObject targetObject,
        string projectileId)
    {
        this.playerOwner = playerOwner;
        this.playerOwnerNumber = playerOwnerNumber;
        this.isLocal = isLocal;
        this.targetObject = targetObject;
        this.projectileId = projectileId;

        var position = playerOrigin.position;
        position.y += initialPositionOffsetY;
        transform.position = position;

        var direction = toTheLeft ? -1 : 1;
        velocity.x *= direction;
        GetComponent<Rigidbody2D>().velocity = velocity;

        var manuallyFired = GetComponent<IManuallyFiredProjectile>();
        if (manuallyFired != null)
        {
            manuallyFired.FireProjectile();
        }
    }

    public void FireProjectileImmediate(
        PlayerBehaviour playerOwner,
        int playerOwnerNumber,
        bool isLocal,
        Vector3 origin,
        Vector3 currentTarget,
        GameObject targetObject,
        string projectileId)
    {
        this.playerOwner = playerOwner;
        this.playerOwnerNumber = playerOwnerNumber;
        this.isLocal = isLocal;
        this.targetObject = targetObject;
        this.projectileId = projectileId;
        transform.position = currentTarget;

        var manuallyFired = GetComponent<IManuallyFiredProjectile>();
        if (manuallyFired != null)
        {
            manuallyFired.FireProjectileImmediate(origin, currentTarget);
        }
    }

    internal void FirePowerUp(PlayerBehaviour playerOwner, int playerOwnerNumber, bool isLocal, string newProjectileId)
    {
        this.playerOwner = playerOwner;
        this.playerOwnerNumber = playerOwnerNumber;
        this.isLocal = isLocal;
        this.targetObject = playerOwner.transform.root.gameObject;

        var powerUp = transform.root.GetComponentInChildren<IPowerUp>();
        powerUp.Fire();
    }

    public float CalcMaxReach(float offsetY)
    {
        var y = offsetY + initialPositionOffsetY;
        return MyMath.CalcMaxReach(projectileVelocity, y);
    }

    private void OnMouseDown()
    {
        if (Common.IsPointerOverUIObject()) return;
        var position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        NetworkManager.singleton.ObjectiveClicked(gameObject, position);
    }
}
