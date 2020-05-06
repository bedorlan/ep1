using System;
using UnityEngine;

public class ProjectileButtonsScrollBehaviour : MonoBehaviour
{
  internal event Action<GameObject> OnProjectileSelected;
  internal GameObject defaultProjectile;

  void Start()
  {
    defaultProjectile = GetComponentInChildren<ButtonProjectileBehaviour>().projectilePrefab;

    foreach (var buttonBehaviour in GetComponentsInChildren<ButtonProjectileBehaviour>())
    {
      buttonBehaviour.OnMyProjectileSelected += OnSomeProjectileSelected;
      buttonBehaviour.OnMyProjectileFired += OnSomeProjectileFired;
    }
  }

  internal void SelectDefaultProjectile()
  {
    OnSomeProjectileSelected(defaultProjectile);
  }

  private void OnSomeProjectileSelected(GameObject projectilePrefab)
  {
    OnProjectileSelected?.Invoke(projectilePrefab);
    foreach (var buttonBehaviour in GetComponentsInChildren<ButtonProjectileBehaviour>())
      buttonBehaviour.OnSomeProjectileSelected(projectilePrefab);
  }

  private void OnSomeProjectileFired(GameObject obj)
  {
    OnSomeProjectileSelected(defaultProjectile);
  }
}
