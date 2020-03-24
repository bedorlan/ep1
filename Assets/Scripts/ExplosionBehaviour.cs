using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionBehaviour : MonoBehaviour
{
    const float ANIMATION_DURATION = 1.017f;

    void Start()
    {
        StartCoroutine(WaitAndDestroy());
        
    }

    IEnumerator WaitAndDestroy()
    {
        yield return new WaitForSeconds(ANIMATION_DURATION);
        Destroy(transform.root.gameObject);
    }
}
