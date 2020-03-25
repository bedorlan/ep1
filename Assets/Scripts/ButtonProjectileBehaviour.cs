using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonProjectileBehaviour : MonoBehaviour
{
    public GameObject projectilePrefab;
    public int cooldown = 0;

    private bool isSelected = false;
    private bool isCoolingDown = false;

    void Start()
    {
        NetworkManager.singleton.OnProjectileSelected += NetworkManager_OnProjectileSelected;
    }

    public void OnProjectileSelected()
    {
        NetworkManager.singleton.ProjectileSelected(projectilePrefab);
    }

    private void NetworkManager_OnProjectileSelected(GameObject selected)
    {
        isSelected = selected == projectilePrefab;
        var button = GetComponentInChildren<Button>();
        button.interactable = !isCoolingDown && !isSelected;
    }

    public void OnYourProjectileFired()
    {
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
