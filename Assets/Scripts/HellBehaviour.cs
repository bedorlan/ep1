﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HellBehaviour : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // the hell!
        Destroy(collision.transform.root.gameObject);
    }
}
