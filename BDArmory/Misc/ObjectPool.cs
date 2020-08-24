using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BDArmory.Misc
{
    public class ObjectPool : MonoBehaviour
    {
        public GameObject poolObject;
        public int size { get { return pool.Count; } }
        public bool canGrow;
        public int lastIndex = 0;

        List<GameObject> pool;

        public string poolObjectName;

        void Awake()
        {
            pool = new List<GameObject>();
        }

        void Start()
        {
        }

        public GameObject GetPooledObject(int index)
        {
            return pool[index];
        }

        private void AddObjectsToPool(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                GameObject obj = Instantiate(poolObject);
                obj.transform.SetParent(transform);
                obj.SetActive(false);
                pool.Add(obj);
            }
        }

        public GameObject GetPooledObject()
        {
            if (!poolObject)
            {
                Debug.LogWarning("Tried to instantiate a pool object but prefab is missing! (" + poolObjectName + ")");
            }

            // Start at the last index returned and cycle round for efficiency. This makes this a typically O(1) seek operation.
            for (int i = lastIndex; i < pool.Count; ++i)
            {
                if (!pool[i].activeInHierarchy)
                {
                    lastIndex = i;
                    return pool[i];
                }
            }
            for (int i = 0; i < lastIndex; ++i)
            {
                if (!pool[i].activeInHierarchy)
                {
                    lastIndex = i;
                    return pool[i];
                }
            }

            if (canGrow)
            {
                var size = (int)(pool.Count * 1.2); // Grow by 20%
                Debug.Log("[ObjectPool]: Increasing pool size to " + size + " for " + poolObjectName);
                AddObjectsToPool(size - pool.Count);

                return pool[pool.Count - 1]; // Return the last entry in the pool
            }

            return null;
        }

        public void DisableAfterDelay(GameObject obj, float t)
        {
            StartCoroutine(DisableObject(obj, t));
        }

        IEnumerator DisableObject(GameObject obj, float t)
        {
            yield return new WaitForSeconds(t);
            if (obj)
            {
                obj.SetActive(false);
                obj.transform.parent = transform;
            }
        }

        public static ObjectPool CreateObjectPool(GameObject obj, int size, bool canGrow, bool destroyOnLoad, bool disableAfterDelay = false)
        {
            GameObject poolObject = new GameObject(obj.name + "Pool");
            ObjectPool op = poolObject.AddComponent<ObjectPool>();
            op.poolObject = obj;
            op.canGrow = canGrow;
            op.poolObjectName = obj.name;
            if (!destroyOnLoad)
            {
                DontDestroyOnLoad(poolObject);
            }
            op.AddObjectsToPool(size);
            
            return op;
        }
    }
}
