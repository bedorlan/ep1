using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lechona : MonoBehaviour, IProjectile
{
  public bool CanYouFireAt(Vector3 position, GameObject target)
  {
    var validTarget = target != null && target.GetComponentInChildren<IPartySupporter>() != null;
    return validTarget;
  }

  public bool IsPowerUp()
  {
    return false;
  }

  private HashSet<IPartySupporter> votersAtRange = new HashSet<IPartySupporter>();

  public void OnTriggerEnter2D(Collider2D other)
  {
    if (other.GetComponent<Floor>() != null)
    {
      Explode();
      return;
    }

    var voter = other.GetComponentInChildren<IPartySupporter>();
    if (voter != null)
    {
      votersAtRange.Add(voter);
    }
  }

  private void Explode()
  {
    var projectile = gameObject.GetComponent<Projectile>();
    var playerOwner = projectile.playerOwnerNumber;
    var isLocal = projectile.isLocal;
    var endAnimationPrefab = projectile.endAnimationPrefab;

    foreach (var voter in votersAtRange)
    {
      if (voter == null) continue;
      voter.TryConvertTo(playerOwner, isLocal);
    }

    var endAnimation = Instantiate(endAnimationPrefab);
    endAnimation.transform.position = transform.position;
    endAnimation.transform.localScale.Scale(new Vector3(2, 2, 1));

    Destroy(transform.root.gameObject);
  }
}
