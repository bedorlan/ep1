using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonProjectileBehaviour : MonoBehaviour
{
  public GameObject projectilePrefab;
  public int cooldown = 0;

  internal event Action<GameObject> OnMyProjectileSelected;
  internal event Action<GameObject> OnMyProjectileFired;

  private bool isSelected = false;
  private bool isCoolingDown = false;

  public void OnProjectileSelected()
  {
    OnMyProjectileSelected?.Invoke(projectilePrefab);
  }

  internal void OnSomeProjectileSelected(GameObject selected)
  {
    isSelected = selected == projectilePrefab;
    var button = GetComponentInChildren<Button>();
    button.interactable = !isCoolingDown && !isSelected;
  }

  public void OnYourProjectileFired()
  {
    OnMyProjectileFired?.Invoke(projectilePrefab);
    StartCoroutine(StartCooldownRoutine());
  }

  internal IEnumerator StartCooldownRoutine()
  {
    GetComponentInChildren<Button>().interactable = false;
    isCoolingDown = true;

    var cooldownBehaviour = GetComponentInChildren<CooldownBehaviour>();
    yield return StartCoroutine(cooldownBehaviour.StartCooldown(cooldown));

    GetComponentInChildren<Button>().interactable = !isSelected;
    isCoolingDown = false;
  }

  internal int GetProjectileTypeId()
  {
    return projectilePrefab.GetComponentInChildren<Projectile>().projectileTypeId;
  }
}
