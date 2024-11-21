/*using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : Singleton_template<PoolManager>
{
    private Dictionary<string, object> poolDictionary = new Dictionary<string, object>();

    // Method to initialize a pool for a specific type
    public void CreatePool<T>(string tag, GameObject prefab, Transform holder, int size) where T : Component
    {
        if (poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} already exists.");
            return;
        }

        Pool<T> pool = new Pool<T>(() => CreateGameObject<T>(prefab, holder), size);
        pool.Initialize();
        poolDictionary.Add(tag, pool);
    }

    private T CreateGameObject<T>(GameObject prefab, Transform holder) where T : Component
    {
        GameObject newObj = holder != null ? Instantiate(prefab, holder) : Instantiate(prefab);
        T component = newObj.GetComponent<T>();
        if (component == null)
        {
            component = newObj.AddComponent<T>();
        }
        return component;
    }

    public T SpawnFromPool<T>(string tag, Vector3 pos, Quaternion rot) where T : Component
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogError($"Pool with tag {tag} not found.");
            return null;
        }

        Pool<T> pool = poolDictionary[tag] as Pool<T>;
        if (pool == null)
        {
            Debug.LogError($"Pool with tag {tag} is not of the correct type.");
            return null;
        }
        return pool.GetObject(pos, rot);
    }

    public void ReturnToPool<T>(string tag, T obj) where T : Component
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogError($"Pool with tag {tag} not found.");
            return;
        }

        Pool<T> pool = poolDictionary[tag] as Pool<T>;
        if (pool == null)
        {
            Debug.LogError($"Pool with tag {tag} is not of the correct type.");
            return;
        }
        pool.ReturnObject(obj.gameObject);
    }

    // Pool class definition remains largely the same, but now works with a specific component type
    [System.Serializable]
    public class Pool<T> where T : Component
    {
        private Func<T> createObject;
        private Queue<T> objectPool = new Queue<T>();
        private List<T> activeObjects = new List<T>();

        public Pool(Func<T> createFunc, int initialSize)
        {
            createObject = createFunc;
            for (int i = 0; i < initialSize; i++)
            {
                var newObj = createObject();
                newObj.gameObject.SetActive(false);
                objectPool.Enqueue(newObj);
            }
        }

        public void Initialize()
        {
            // Initialize is handled in the constructor
        }

        public T GetObject(Vector3 pos, Quaternion rot)
        {
            if (objectPool.Count == 0)
            {
                objectPool.Enqueue(createObject());
            }

            var obj = objectPool.Dequeue();
            obj.transform.SetPositionAndRotation(pos, rot);
            obj.gameObject.SetActive(true);
            activeObjects.Add(obj);
            return obj;
        }

        public void ReturnObject(GameObject obj)
        {
            obj.SetActive(false);
            T component = obj.GetComponent<T>();
            if (component != null)
            {
                objectPool.Enqueue(component);
                activeObjects.Remove(component);
            }
        }
    }
}*/


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : Singleton_template<PoolManager>
{
    public List<Pool> poolDefinitions; // Definitions for each pool
    private Dictionary<string, Pool> poolDictionary;

    private void Start()
    {
        poolDictionary = new Dictionary<string, Pool>();

        foreach (var poolDef in poolDefinitions)
        {
            Pool pool = new Pool(() => CreateGameObject(poolDef.prefab, poolDef.holder), poolDef.size);
            pool.tag = poolDef.tag;
            pool.prefab = poolDef.prefab;
            pool.holder = poolDef.holder;
            pool.size = poolDef.size;

            pool.Initialize();
            poolDictionary.Add(poolDef.tag, pool);
        }
    }


    private GameObject CreateGameObject(GameObject prefab, Transform holder)
    {
        if (holder != null)
        {
            return Instantiate(prefab, holder);
        }
        else
        {
            return Instantiate(prefab);
        }
    }

    public GameObject SpawnFromPool(string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogError($"Pool with tag {tag} not found.");
            return null;
        }
        return poolDictionary[tag].GetObject();
    }

    public GameObject SpawnFromPool(string tag, Vector3 pos, Quaternion rot)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogError($"Pool with tag {tag} not found.");
            return null;
        }
        return poolDictionary[tag].GetObject(pos, rot);
    }

    public GameObject SpawnFromPool(string tag, Transform parent)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogError($"Pool with tag {tag} not found.");
            return null;
        }
        return poolDictionary[tag].GetObject(parent);
    }

    public void ReturnToPool(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogError($"Pool with tag {tag} not found.");
            return;
        }
        poolDictionary[tag].ReturnObject(obj);
    }
    public void ReturnToPoolDelayed(string tag, GameObject obj, float t)
    {
        StartCoroutine(RTPD(tag, obj, t));
    }
    private IEnumerator RTPD(string tag, GameObject obj, float t)
    {
        if (!poolDictionary.ContainsKey(tag)) { Debug.LogWarning("No Pool with tag " + tag); yield return null; }

        yield return new WaitForSeconds(t);

        if (obj.activeInHierarchy)
        {
            obj.SetActive(false);
            poolDictionary[tag].ReturnObject(obj);
            // remove objectToSpawn from the active list in the coresponding pool
        }
    }

    public IEnumerable<GameObject> GetActiveObjects(string poolTag)
    {
        if (poolDictionary.TryGetValue(poolTag, out Pool pool))
        {
            return pool.ActiveObjects;
        }

        Debug.LogWarning($"Pool with tag {poolTag} not found.");
        return null;
    }
}
[System.Serializable]
public class Pool
{
    public string tag;
    public GameObject prefab;
    public int size;
    public Transform holder;
    private Queue<GameObject> objectPool = new Queue<GameObject>();
    private Func<GameObject> createObject;
    private List<GameObject> activeObjects = new List<GameObject>();

    public IEnumerable<GameObject> ActiveObjects => activeObjects.AsReadOnly();

    public void ReturnObject(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(holder);
        objectPool.Enqueue(obj);
        activeObjects.Remove(obj);
    }
    public Pool(Func<GameObject> createFunc, int initialSize)
    {
        createObject = createFunc;
        size = initialSize;
    }

    public void Initialize()
    {
        AddObjects(size);
    }

    private void AddObjects(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var newObj = createObject();
            newObj.SetActive(false);
            objectPool.Enqueue(newObj);
        }
    }

    public GameObject GetObject()
    {
        if (objectPool.Count == 0)
        {
            AddObjects(1); // Add more objects if the pool is empty
        }

        var obj = objectPool.Dequeue();
        obj.SetActive(true);
        activeObjects.Add(obj);
        return obj;
    }

    public GameObject GetObject(Vector3 pos, Quaternion rot)
    {
        if (objectPool.Count == 0)
        {
            AddObjects(1);
        }
        var obj = objectPool.Dequeue();
        obj.transform.SetPositionAndRotation(pos, rot);
        obj.SetActive(true);
        activeObjects.Add(obj);
        return obj;
    }

    public GameObject GetObject(Transform parent)
    {
        if (objectPool.Count == 0)
        {
            AddObjects(1);
        }
        var obj = objectPool.Dequeue();
        obj.transform.SetParent(parent);
        obj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        obj.SetActive(true);
        activeObjects.Add(obj);
        return obj;
    }
}

/*public class PoolManager : Singleton_template<PoolManager>
{
    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = pool.holder != null ? Instantiate(pool.prefab, pool.holder) : Instantiate(pool.prefab);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 pos, Quaternion rot)
    {
        if (!poolDictionary.ContainsKey(tag)) { Debug.LogWarning("No Pool with tag " + tag); return null; }

        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.SetPositionAndRotation(pos, rot);

        poolDictionary[tag].Enqueue(objectToSpawn);

        // add objectToSpawn to the active list in the coresponding pool

        return objectToSpawn;
    }

    public GameObject SpawnFromPool(string tag)
    {
        if (!poolDictionary.ContainsKey(tag)) { Debug.LogWarning("No Pool with tag " + tag); return null; }

        GameObject objectToSpawn = poolDictionary[tag].Dequeue();

        objectToSpawn.SetActive(true);

        poolDictionary[tag].Enqueue(objectToSpawn);

        // add objectToSpawn to the active list in the coresponding pool

        return objectToSpawn;
    }

    public void ReturnToPool(string tag, GameObject objectToReturn)
    {
        if (!poolDictionary.ContainsKey(tag)) { Debug.LogWarning("No Pool with tag " + tag); return; }

        objectToReturn.SetActive(false);
        poolDictionary[tag].Enqueue(objectToReturn);
        // remove objectToSpawn from the active list in the coresponding pool
    }

    public IEnumerator ReturnToPoolDelayed(string tag, GameObject objectToReturn, float t)
    {
        if (!poolDictionary.ContainsKey(tag)) { Debug.LogWarning("No Pool with tag " + tag); yield return null; }

        yield return new WaitForSeconds(t);

        if (objectToReturn.activeInHierarchy)
        {
            objectToReturn.SetActive(false);
            poolDictionary[tag].Enqueue(objectToReturn);
            // remove objectToSpawn from the active list in the coresponding pool
        }
    }
}
[System.Serializable]
public class Pool
{
    public string tag;
    public GameObject prefab;
    public int size;
    public Transform holder;
    public List<GameObject> active = new List<GameObject>();
}*/
