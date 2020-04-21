using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public GameObject prefab;
    public int minPoolSize;

    Queue<GameObject> pool = new Queue<GameObject>();

    private void Update()
    {
        if (pool.Count >= minPoolSize) return;

        var obj = Instantiate(prefab);
        obj.SetActive(false);

        pool.Enqueue(obj);
    }

    internal T Spawn<T>(Vector2 position, Quaternion rotation)
    {
        GameObject newObject;
        if (pool.Count > 0)
        {
            newObject = pool.Dequeue();
        }
        else
        {
            newObject = Instantiate(prefab);
        }

        newObject.SetActive(true);

        var newPosition = new Vector3(position.x, position.y, newObject.transform.position.z);
        newObject.transform.position = newPosition;
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
