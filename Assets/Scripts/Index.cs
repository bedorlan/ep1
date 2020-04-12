using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Index : MonoBehaviour
{
    public GameObject magicFlame;

    internal static Index singleton { get; private set; }
    private void Start()
    {
        singleton = this;
    }
}
