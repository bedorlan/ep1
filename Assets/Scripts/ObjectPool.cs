using System.Collections.Generic;
using UnityEngine;

internal class ObjectPool<T>
{
    GameObject prefab;
    Queue<GameObject> pool = new Queue<GameObject>();

    internal ObjectPool(GameObject prefab)
    {
        this.prefab = prefab;
    }

    internal T Spawn(Vector3 position, Quaternion rotation)
    {
        GameObject newObject;
        if (pool.Count > 0)
        {
            newObject = pool.Dequeue();
        }
        else
        {
            newObject = MonoBehaviour.Instantiate(prefab);
        }

        newObject.SetActive(true);
        newObject.transform.position = position;
        newObject.transform.rotation = rotation;

        var poolObject = newObject.GetComponentInChildren<IPoolable>();
        poolObject.Spawn();
        poolObject.Despawn += PoolObject_Despawn;

        return newObject.GetComponentInChildren<T>();
    }

    private void PoolObject_Despawn(GameObject obj)
    {
        obj = obj.transform.root.gameObject;

        var poolObject = obj.GetComponentInChildren<IPoolable>();
        poolObject.Despawn -= PoolObject_Despawn;

        obj.SetActive(false);
        pool.Enqueue(obj);
    }
}
