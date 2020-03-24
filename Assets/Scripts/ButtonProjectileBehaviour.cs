using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonProjectileBehaviour : MonoBehaviour
{
    public GameObject projectilePrefab;

    void Start()
    {
        NetworkManager.singleton.OnProjectileSelected += NetworkManager_OnProjectileSelected;
    }

    private void NetworkManager_OnProjectileSelected(GameObject obj)
    {
        Debug.Log("NetworkManager_OnProjectileSelected");
    }

    public void OnProjectileSelected()
    {
        NetworkManager.singleton.ProjectileSelected(projectilePrefab);
    }
}
