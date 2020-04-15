using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Index : MonoBehaviour
{
    public GameObject magicFlamePrefab;
    public GameObject menuObject;

    internal static Index singleton { get; private set; }
    private void Start()
    {
        singleton = this;
    }
}
