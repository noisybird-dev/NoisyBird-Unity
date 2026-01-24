using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace NoisyBird.AddressableExtension
{
    public class AddressableManager : MonoBehaviour
    {
        private static AddressableManager _instance;
        public static AddressableManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("AddressableManager");
                    _instance = go.AddComponent<AddressableManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // Key: Primary Key (Address), Value: Handle and RefCount
        private class TrackedResource
        {
            public AsyncOperationHandle Handle;
            public int RefCount;
        }

        private readonly Dictionary<string, TrackedResource> _trackedResources = new Dictionary<string, TrackedResource>();
        
        // Tag support: Tag -> List of keys associated with this tag
        private readonly Dictionary<string, HashSet<string>> _tagToKeys = new Dictionary<string, HashSet<string>>();

        /// <summary>
        /// Loads an asset of type T. Uses reference counting.
        /// </summary>
        public void LoadAsset<T>(string key, Action<T> onComplete, Action<Exception> onError = null, string tag = null)
        {
            if (_trackedResources.TryGetValue(key, out var tracked))
            {
                tracked.RefCount++;
                RegisterTag(tag, key);
                
                if (tracked.Handle.Status == AsyncOperationStatus.Succeeded)
                {
                    onComplete?.Invoke((T)tracked.Handle.Result);
                }
                else
                {
                    tracked.Handle.Completed += (op) =>
                    {
                        if (op.Status == AsyncOperationStatus.Succeeded)
                            onComplete?.Invoke((T)op.Result);
                        else
                            onError?.Invoke(op.OperationException);
                    };
                }
                return;
            }

            // Start new load
            var handle = Addressables.LoadAssetAsync<T>(key);
            var newTracker = new TrackedResource { Handle = handle, RefCount = 1 };
            _trackedResources.Add(key, newTracker);
            RegisterTag(tag, key);
            
            handle.Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    onComplete?.Invoke(op.Result);
                }
                else
                {
                    // Load failed, remove tracking
                    _trackedResources.Remove(key);
                    onError?.Invoke(op.OperationException);
                }
            };
        }

        /// <summary>
        /// Loads an asset synchronously.
        /// WARNING: This blocks the main thread until completion. Use with caution.
        /// </summary>
        public T LoadAssetSync<T>(string key, string tag = null)
        {
            if (_trackedResources.TryGetValue(key, out var tracked))
            {
                tracked.RefCount++;
                RegisterTag(tag, key);

                if (!tracked.Handle.IsDone)
                {
                    tracked.Handle.WaitForCompletion();
                }

                if (tracked.Handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return (T)tracked.Handle.Result;
                }
                return default;
            }

            // Start new load
            var handle = Addressables.LoadAssetAsync<T>(key);
            var newTracker = new TrackedResource { Handle = handle, RefCount = 1 };
            _trackedResources.Add(key, newTracker);
            RegisterTag(tag, key);
            
            try 
            {
                var result = handle.WaitForCompletion();
                
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return result;
                }
                else
                {
                   // Remove tracking if failed immediately?
                   _trackedResources.Remove(key);
                   return default;
                }
            }
            catch (Exception)
            {
                _trackedResources.Remove(key);
                throw;
            }
        }

        /// <summary>
        /// Releases an asset. Decrements ref count. If 0, releases handle.
        /// </summary>
        public void ReleaseAsset(string key)
        {
            if (_trackedResources.TryGetValue(key, out var tracked))
            {
                tracked.RefCount--;
                if (tracked.RefCount <= 0)
                {
                    Addressables.Release(tracked.Handle);
                    _trackedResources.Remove(key);
                }
            }
            else
            {
                Debug.LogWarning($"[AddressableManager] Trying to release unknown or already released key: {key}");
            }
        }

        /// <summary>
        /// Instantiates a prefab and attaches a lifecycle linker for auto-release.
        /// RefCount is managed by the instantiated GameObject's lifecycle.
        /// </summary>
        public void LoadGameObject(string key, Transform parent = null, Action<GameObject> onComplete = null, Action<Exception> onError = null, string tag = null)
        {
            LoadAsset<GameObject>(key, (prefab) =>
            {
                if (prefab == null) return;

                var instance = Instantiate(prefab, parent);
                var linker = instance.AddComponent<AddressableLifecycleLinker>();
                linker.Init(this, key);
                
                onComplete?.Invoke(instance);
            }, onError, tag);
        }
        
        public T LoadGameObjectAsync<T>(string key, Transform parent = null, string tag = null) where T : Component
        {
            var prefab = LoadAssetSync<GameObject>(key, tag);
            if (prefab == null) return null;

            var instance = Instantiate(prefab, parent);
            var linker = instance.AddComponent<AddressableLifecycleLinker>();
            linker.Init(this, key);
            
            return instance.GetComponent<T>();
        }

        private void RegisterTag(string tag, string key)
        {
            if (string.IsNullOrEmpty(tag)) return;
            
            if (!_tagToKeys.TryGetValue(tag, out var keys))
            {
                keys = new HashSet<string>();
                _tagToKeys[tag] = keys;
            }
            keys.Add(key);
        }

        /// <summary>
        /// Releases all references to the asset, effectively unloading it immediately.
        /// </summary>
        public void ReleaseAllRef(string key)
        {
            if (_trackedResources.TryGetValue(key, out var tracked))
            {
                int count = tracked.RefCount;
                for (int i = 0; i < count; i++)
                {
                    ReleaseAsset(key);
                }
            }
        }

        public void ReleaseByTag(string tag)
        {
            if (_tagToKeys.TryGetValue(tag, out var keys))
            {
                var keysToRelease = new List<string>(keys); 
                foreach (var key in keysToRelease)
                {
                    ReleaseAllRef(key);
                }
                
                _tagToKeys.Remove(tag);
            }
        }
    }
}
