using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CentralBaseBehaviour : MonoBehaviour, IProjectile
{
    public bool CanYouFireAt(Vector3 position, GameObject target)
    {
        var otherBase = target?.GetComponentInChildren<CentralBaseBehaviour>() ?? null;
        return otherBase == null;
    }

    private void Start()
    {
        var playerOwner = GetComponent<Projectile>().playerOwner;
        foreach (var sprite in GetComponentsInChildren<SpriteRenderer>())
        {
            sprite.color = Common.playerColors[playerOwner];
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var floor = other.GetComponent<Floor>();
        if (floor == null) return;

        var rigidbody = GetComponent<Rigidbody2D>();
        rigidbody.velocity = Vector3.zero;
        rigidbody.bodyType = RigidbodyType2D.Static;
    }

    private void OnMouseDown()
    {
        var position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        NetworkManager.singleton.ObjectiveClicked(gameObject, position);
    }
}
