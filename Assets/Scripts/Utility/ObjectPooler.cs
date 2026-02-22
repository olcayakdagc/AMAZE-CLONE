using System.Collections.Generic;
using UnityEngine;

namespace Utility
{
    public enum PoolNames
    {
        Audio,
        GridVisual,
    }

    public class ObjectPooler<T> where T : Component
    {
        public class PoolItem
        {
            public readonly PoolNames Id;
            public readonly T Prefab;
            public readonly int DefaultCapacity;
            public readonly int MaxSize;

            public PoolItem(PoolNames id, T prefab, int defaultCapacity = 10, int maxSize = 50)
            {
                this.Id = id;
                this.Prefab = prefab;
                this.DefaultCapacity = defaultCapacity;
                this.MaxSize = maxSize;
            }
        }

        private class Pool
        {
            public readonly Stack<T> InactiveObjects = new();
            public readonly PoolItem Settings;
            public int ActiveCount;

            public Pool(PoolItem settings)
            {
                Settings = settings;
                for (int i = 0; i < settings.DefaultCapacity; i++)
                {
                    var obj = Object.Instantiate(settings.Prefab);
                    obj.gameObject.SetActive(false);
                    InactiveObjects.Push(obj);
                }
            }
        }

        private readonly Dictionary<PoolNames, Pool> _pools = new();
        
        public ObjectPooler(List<PoolItem> poolItems)
        {
            foreach (var item in poolItems)
            {
                _pools[item.Id] = new Pool(item);
            }
        }

        public T Spawn(PoolNames id)
        {
            return Spawn(id, Vector3.zero, Quaternion.identity);
        }

        public T Spawn(PoolNames id, Transform parent)
        {
            var obj = Spawn(id, Vector3.zero, Quaternion.identity);
            obj.transform.SetParent(parent, false);
            return obj;
        }

        public T Spawn(PoolNames id, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(id, out var pool))
            {
                Debug.LogError($"Pool with ID {id} not found!");
                return null;
            }

            T obj;
            if (pool.InactiveObjects.Count > 0)
            {
                obj = pool.InactiveObjects.Pop();
            }
            else
            {
                if (pool.ActiveCount >= pool.Settings.MaxSize)
                {
                    Debug.LogWarning($"Pool '{id}' reached max size ({pool.Settings.MaxSize}), instantiating anyway.");
                }

                obj = Object.Instantiate(pool.Settings.Prefab);
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.gameObject.SetActive(true);
            pool.ActiveCount++;

            return obj;
        }

        public T Spawn(PoolNames id, Vector3 position, Vector3 eulerAngles)
        {
            return Spawn(id, position, Quaternion.Euler(eulerAngles));
        }

        public void Release(PoolNames id, T obj)
        {
            if (!_pools.TryGetValue(id, out var pool))
            {
                Debug.LogWarning($"Pool with ID {id} not found, destroying object.");
                Object.Destroy(obj.gameObject);
                return;
            }

            obj.gameObject.SetActive(false);
            obj.transform.SetParent(null); // optionally detach from parent
            pool.InactiveObjects.Push(obj);
            pool.ActiveCount--;
        }
    }
}