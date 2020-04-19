using System;
using UnityEngine;

interface IPoolable
{
    void Spawn();
    event Action<GameObject> Despawn;
}
