using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public class PooledObject : MonoBehaviour
    {
        public GameObject originalPrefab;
        public bool inPool;
    }

    public static class SimplePool
    {
        private static readonly Dictionary<GameObject, Stack<GameObject>> pools = new Dictionary<GameObject, Stack<GameObject>>();
        private static SimplePoolScheduler schedulerInstance;

        public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;

            if (!pools.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<GameObject>(16);
                pools[prefab] = stack;
            }

            GameObject obj;
            if (stack.Count > 0)
            {
                obj = stack.Pop();
                if (obj == null)
                {
                    obj = Create(prefab, position, rotation);
                }
                else
                {
                    obj.transform.SetPositionAndRotation(position, rotation);
                    obj.SetActive(true);
                    var po = obj.GetComponent<PooledObject>();
                    if (po != null) po.inPool = false;
                }
            }
            else
            {
                obj = Create(prefab, position, rotation);
            }

            return obj;
        }

        public static void Return(GameObject instance)
        {
            if (instance == null) return;
            var po = instance.GetComponent<PooledObject>();
            if (po == null || po.originalPrefab == null)
            {
                Object.Destroy(instance);
                return;
            }
            if (po.inPool) return;
            if (!pools.TryGetValue(po.originalPrefab, out var stack))
            {
                stack = new Stack<GameObject>(16);
                pools[po.originalPrefab] = stack;
            }
            instance.SetActive(false);
            po.inPool = true;
            stack.Push(instance);
        }

        public static void ReturnDelayed(GameObject instance, float delay)
        {
            EnsureScheduler();
            if (schedulerInstance != null) schedulerInstance.ReturnDelayed(instance, delay);
        }

        public static void Clear()
        {
            foreach (var stack in pools.Values)
            {
                while (stack.Count > 0)
                {
                    var obj = stack.Pop();
                    if (obj != null) Object.Destroy(obj);
                }
            }
            pools.Clear();
        }

        private static GameObject Create(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var obj = Object.Instantiate(prefab, position, rotation);
            var po = obj.GetComponent<PooledObject>();
            if (po == null) po = obj.AddComponent<PooledObject>();
            po.originalPrefab = prefab;
            po.inPool = false;
            return obj;
        }

        private static void EnsureScheduler()
        {
            if (schedulerInstance != null) return;
            var go = new GameObject("SimplePoolScheduler");
            Object.DontDestroyOnLoad(go);
            schedulerInstance = go.AddComponent<SimplePoolScheduler>();
        }
    }

    public class SimplePoolScheduler : MonoBehaviour
    {
        public void ReturnDelayed(GameObject instance, float delay)
        {
            StartCoroutine(Routine(instance, delay));
        }

        private System.Collections.IEnumerator Routine(GameObject instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            SimplePool.Return(instance);
        }
    }
}
