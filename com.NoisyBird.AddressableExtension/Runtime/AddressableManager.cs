using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

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

        // Scene tracking
        private class TrackedScene
        {
            public AsyncOperationHandle<SceneInstance> Handle;
            public int RefCount;
        }

        // Instance tracking for pooling
        private class TrackedInstance
        {
            public AsyncOperationHandle<GameObject> Handle;
            public GameObject Instance;
            public string Key;
        }

        private readonly Dictionary<string, TrackedResource> _trackedResources = new Dictionary<string, TrackedResource>();
        private readonly Dictionary<string, TrackedScene> _trackedScenes = new Dictionary<string, TrackedScene>();
        private readonly Dictionary<GameObject, TrackedInstance> _trackedInstances = new Dictionary<GameObject, TrackedInstance>();

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

        #region Phase 1: GameObject 반환 함수

        /// <summary>
        /// GameObject를 동기적으로 로드하고 반환합니다.
        /// WARNING: 메인 스레드를 블록하므로 주의해서 사용하세요.
        /// </summary>
        /// <param name="key">Addressable 키 (Address)</param>
        /// <param name="parent">생성될 GameObject의 부모 Transform</param>
        /// <param name="tag">태그 (선택사항, ReleaseByTag로 일괄 해제 가능)</param>
        /// <returns>생성된 GameObject (실패 시 null)</returns>
        /// <remarks>
        /// AddressableLifecycleLinker가 자동으로 부착되어 OnDestroy 시 자동 해제됩니다.
        /// 수동으로 ReleaseAsset을 호출할 필요가 없습니다.
        /// 사용 예시:
        /// <code>
        /// GameObject enemy = AddressableManager.Instance.LoadGameObjectSync("Enemy", transform);
        /// enemy.transform.position = new Vector3(0, 0, 0);
        /// </code>
        /// </remarks>
        public GameObject LoadGameObjectSync(string key, Transform parent = null, string tag = null)
        {
            try
            {
                var prefab = LoadAssetSync<GameObject>(key, tag);
                if (prefab == null)
                {
                    Debug.LogError($"[AddressableManager] Failed to load GameObject prefab: {key}");
                    return null;
                }

                var instance = Instantiate(prefab, parent);
                var linker = instance.AddComponent<AddressableLifecycleLinker>();
                linker.Init(this, key);

                return instance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableManager] Error loading GameObject sync: {key}, Exception: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// GameObject를 비동기적으로 로드하고 반환합니다 (async/await 패턴).
        /// </summary>
        /// <param name="key">Addressable 키 (Address)</param>
        /// <param name="parent">생성될 GameObject의 부모 Transform</param>
        /// <param name="tag">태그 (선택사항, ReleaseByTag로 일괄 해제 가능)</param>
        /// <returns>생성된 GameObject</returns>
        /// <exception cref="Exception">로드 실패 시 예외 발생</exception>
        /// <remarks>
        /// AddressableLifecycleLinker가 자동으로 부착되어 OnDestroy 시 자동 해제됩니다.
        /// 수동으로 ReleaseAsset을 호출할 필요가 없습니다.
        /// 사용 예시:
        /// <code>
        /// GameObject enemy = await AddressableManager.Instance.LoadGameObjectAsync("Enemy", transform);
        /// enemy.transform.position = new Vector3(0, 0, 0);
        /// </code>
        /// </remarks>
        public async Task<GameObject> LoadGameObjectAsync(string key, Transform parent = null, string tag = null)
        {
            try
            {
                var prefab = await LoadAssetAsync<GameObject>(key, tag);
                if (prefab == null)
                {
                    throw new Exception($"Failed to load GameObject prefab: {key}");
                }

                var instance = Instantiate(prefab, parent);
                var linker = instance.AddComponent<AddressableLifecycleLinker>();
                linker.Init(this, key);

                return instance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableManager] Error loading GameObject async: {key}, Exception: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Phase 2: 배치 로드 기능

        /// <summary>
        /// 여러 에셋을 배치로 로드합니다 (콜백 방식).
        /// </summary>
        /// <typeparam name="T">에셋 타입</typeparam>
        /// <param name="keys">로드할 에셋 키들</param>
        /// <param name="onComplete">모든 에셋 로드 완료 시 호출 (에셋 리스트 전달)</param>
        /// <param name="onError">로드 실패 시 호출</param>
        /// <param name="tag">태그 (선택사항, ReleaseByTag로 일괄 해제 가능)</param>
        /// <remarks>
        /// 하나라도 실패하면 onError가 호출됩니다.
        /// 각 에셋은 개별적으로 RefCount 관리됩니다.
        /// </remarks>
        public void LoadAssets<T>(IEnumerable<string> keys, Action<IList<T>> onComplete, Action<Exception> onError = null, string tag = null)
        {
            var keysList = keys.ToList();
            if (keysList.Count == 0)
            {
                onComplete?.Invoke(new List<T>());
                return;
            }

            var results = new List<T>();
            var loadedCount = 0;
            var hasError = false;

            foreach (var key in keysList)
            {
                LoadAsset<T>(key, (asset) =>
                {
                    if (hasError) return;

                    results.Add(asset);
                    loadedCount++;

                    if (loadedCount == keysList.Count)
                    {
                        onComplete?.Invoke(results);
                    }
                }, (ex) =>
                {
                    if (!hasError)
                    {
                        hasError = true;
                        onError?.Invoke(ex);
                    }
                }, tag);
            }
        }

        /// <summary>
        /// 여러 에셋을 배치로 로드합니다 (async/await 패턴).
        /// </summary>
        /// <typeparam name="T">에셋 타입</typeparam>
        /// <param name="keys">로드할 에셋 키들</param>
        /// <param name="tag">태그 (선택사항, ReleaseByTag로 일괄 해제 가능)</param>
        /// <returns>로드된 에셋 리스트</returns>
        /// <exception cref="Exception">로드 실패 시 예외 발생</exception>
        /// <remarks>
        /// 사용 예시:
        /// <code>
        /// var sprites = await AddressableManager.Instance.LoadAssetsAsync&lt;Sprite&gt;(new[] { "Icon1", "Icon2", "Icon3" }, "UI");
        /// </code>
        /// </remarks>
        public async Task<IList<T>> LoadAssetsAsync<T>(IEnumerable<string> keys, string tag = null)
        {
            var keysList = keys.ToList();
            if (keysList.Count == 0)
            {
                return new List<T>();
            }

            var tasks = keysList.Select(key => LoadAssetAsync<T>(key, tag));
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        /// <summary>
        /// Label로 여러 에셋을 로드합니다 (콜백 방식).
        /// </summary>
        /// <typeparam name="T">에셋 타입</typeparam>
        /// <param name="label">Addressable Label</param>
        /// <param name="onComplete">모든 에셋 로드 완료 시 호출</param>
        /// <param name="onError">로드 실패 시 호출</param>
        /// <param name="tag">태그 (선택사항, ReleaseByTag로 일괄 해제 가능)</param>
        public void LoadAssetsByLabel<T>(string label, Action<IList<T>> onComplete, Action<Exception> onError = null, string tag = null)
        {
            var handle = Addressables.LoadAssetsAsync<T>(label, null);

            handle.Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    var results = new List<T>(op.Result);

                    // 각 에셋의 address를 key로 사용하여 추적
                    foreach (var asset in results)
                    {
                        // Note: Label 기반 로드는 개별 키 추적이 어려우므로 label을 key로 사용
                        if (!_trackedResources.ContainsKey(label))
                        {
                            var newTracker = new TrackedResource { Handle = op, RefCount = 1 };
                            _trackedResources.Add(label, newTracker);
                            RegisterTag(tag, label);
                        }
                    }

                    onComplete?.Invoke(results);
                }
                else
                {
                    onError?.Invoke(op.OperationException);
                }
            };
        }

        /// <summary>
        /// Label로 여러 에셋을 로드합니다 (async/await 패턴).
        /// </summary>
        /// <typeparam name="T">에셋 타입</typeparam>
        /// <param name="label">Addressable Label</param>
        /// <param name="tag">태그 (선택사항, ReleaseByTag로 일괄 해제 가능)</param>
        /// <returns>로드된 에셋 리스트</returns>
        public async Task<IList<T>> LoadAssetsByLabelAsync<T>(string label, string tag = null)
        {
            try
            {
                var handle = Addressables.LoadAssetsAsync<T>(label, null);
                var results = await handle.Task;

                // 추적 정보 등록
                if (!_trackedResources.ContainsKey(label))
                {
                    var newTracker = new TrackedResource { Handle = handle, RefCount = 1 };
                    _trackedResources.Add(label, newTracker);
                    RegisterTag(tag, label);
                }

                return new List<T>(results);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableManager] Error loading assets by label: {label}, Exception: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region Phase 3: Scene 관리 기능

        /// <summary>
        /// Scene을 비동기적으로 로드합니다 (콜백 방식).
        /// </summary>
        /// <param name="key">Scene의 Addressable 키</param>
        /// <param name="loadMode">Scene 로드 모드 (Additive 또는 Single)</param>
        /// <param name="onComplete">로드 완료 시 호출</param>
        /// <param name="onError">로드 실패 시 호출</param>
        /// <param name="tag">태그 (선택사항, ReleaseByTag로 일괄 해제 가능)</param>
        public void LoadSceneAsync(string key, LoadSceneMode loadMode, Action<SceneInstance> onComplete = null, Action<Exception> onError = null, string tag = null)
        {
            if (_trackedScenes.TryGetValue(key, out var tracked))
            {
                tracked.RefCount++;
                RegisterTag(tag, key);

                if (tracked.Handle.Status == AsyncOperationStatus.Succeeded)
                {
                    onComplete?.Invoke(tracked.Handle.Result);
                }
                else
                {
                    tracked.Handle.Completed += (op) =>
                    {
                        if (op.Status == AsyncOperationStatus.Succeeded)
                            onComplete?.Invoke(op.Result);
                        else
                            onError?.Invoke(op.OperationException);
                    };
                }
                return;
            }

            var handle = Addressables.LoadSceneAsync(key, loadMode);
            var newTracker = new TrackedScene { Handle = handle, RefCount = 1 };
            _trackedScenes.Add(key, newTracker);
            RegisterTag(tag, key);

            handle.Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    onComplete?.Invoke(op.Result);
                }
                else
                {
                    _trackedScenes.Remove(key);
                    onError?.Invoke(op.OperationException);
                }
            };
        }

        /// <summary>
        /// Scene을 비동기적으로 로드합니다 (async/await 패턴).
        /// </summary>
        /// <param name="key">Scene의 Addressable 키</param>
        /// <param name="loadMode">Scene 로드 모드 (Additive 또는 Single)</param>
        /// <param name="tag">태그 (선택사항, ReleaseByTag로 일괄 해제 가능)</param>
        /// <returns>로드된 SceneInstance</returns>
        /// <remarks>
        /// 사용 예시:
        /// <code>
        /// var sceneInstance = await AddressableManager.Instance.LoadSceneTaskAsync("BattleScene", LoadSceneMode.Additive, "Battle");
        /// Debug.Log("Battle scene loaded!");
        /// </code>
        /// </remarks>
        public async Task<SceneInstance> LoadSceneTaskAsync(string key, LoadSceneMode loadMode, string tag = null)
        {
            if (_trackedScenes.TryGetValue(key, out var tracked))
            {
                tracked.RefCount++;
                RegisterTag(tag, key);

                if (!tracked.Handle.IsDone)
                {
                    await tracked.Handle.Task;
                }

                if (tracked.Handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return tracked.Handle.Result;
                }
                else
                {
                    throw tracked.Handle.OperationException;
                }
            }

            try
            {
                var handle = Addressables.LoadSceneAsync(key, loadMode);
                var newTracker = new TrackedScene { Handle = handle, RefCount = 1 };
                _trackedScenes.Add(key, newTracker);
                RegisterTag(tag, key);

                var result = await handle.Task;
                return result;
            }
            catch (Exception ex)
            {
                _trackedScenes.Remove(key);
                Debug.LogError($"[AddressableManager] Error loading scene: {key}, Exception: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Scene을 언로드합니다 (콜백 방식).
        /// </summary>
        /// <param name="key">Scene의 Addressable 키</param>
        /// <param name="onComplete">언로드 완료 시 호출</param>
        /// <param name="onError">언로드 실패 시 호출</param>
        public void UnloadSceneAsync(string key, Action onComplete = null, Action<Exception> onError = null)
        {
            if (_trackedScenes.TryGetValue(key, out var tracked))
            {
                tracked.RefCount--;
                if (tracked.RefCount <= 0)
                {
                    var handle = Addressables.UnloadSceneAsync(tracked.Handle);
                    handle.Completed += (op) =>
                    {
                        if (op.Status == AsyncOperationStatus.Succeeded)
                        {
                            _trackedScenes.Remove(key);
                            onComplete?.Invoke();
                        }
                        else
                        {
                            onError?.Invoke(op.OperationException);
                        }
                    };
                }
                else
                {
                    onComplete?.Invoke();
                }
            }
            else
            {
                Debug.LogWarning($"[AddressableManager] Trying to unload unknown or already unloaded scene: {key}");
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Scene을 언로드합니다 (async/await 패턴).
        /// </summary>
        /// <param name="key">Scene의 Addressable 키</param>
        public async Task UnloadSceneTaskAsync(string key)
        {
            if (_trackedScenes.TryGetValue(key, out var tracked))
            {
                tracked.RefCount--;
                if (tracked.RefCount <= 0)
                {
                    try
                    {
                        var handle = Addressables.UnloadSceneAsync(tracked.Handle);
                        await handle.Task;
                        _trackedScenes.Remove(key);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[AddressableManager] Error unloading scene: {key}, Exception: {ex.Message}");
                        throw;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[AddressableManager] Trying to unload unknown or already unloaded scene: {key}");
            }
        }

        #endregion

        #region Phase 4: 프리로드 및 상태 확인 기능

        /// <summary>
        /// 단일 에셋을 프리로드합니다 (콜백 방식).
        /// 에셋을 메모리에 로드하지만 결과는 반환하지 않습니다.
        /// </summary>
        /// <typeparam name="T">에셋 타입</typeparam>
        /// <param name="key">에셋 키</param>
        /// <param name="onComplete">로드 완료 시 호출</param>
        /// <param name="onError">로드 실패 시 호출</param>
        /// <param name="tag">태그 (선택사항, ReleaseByTag로 일괄 해제 가능)</param>
        public void PreloadAsset<T>(string key, Action onComplete = null, Action<Exception> onError = null, string tag = null)
        {
            LoadAsset<T>(key, (asset) =>
            {
                onComplete?.Invoke();
            }, onError, tag);
        }

        /// <summary>
        /// 여러 에셋을 프리로드합니다 (콜백 방식).
        /// </summary>
        /// <typeparam name="T">에셋 타입</typeparam>
        /// <param name="keys">프리로드할 에셋 키들</param>
        /// <param name="onComplete">모든 에셋 로드 완료 시 호출</param>
        /// <param name="onError">로드 실패 시 호출</param>
        /// <param name="tag">태그 (선택사항, ReleaseByTag로 일괄 해제 가능)</param>
        public void PreloadAssets<T>(IEnumerable<string> keys, Action onComplete = null, Action<Exception> onError = null, string tag = null)
        {
            LoadAssets<T>(keys, (assets) =>
            {
                onComplete?.Invoke();
            }, onError, tag);
        }

        /// <summary>
        /// 여러 에셋을 프리로드합니다 (async/await 패턴).
        /// </summary>
        /// <typeparam name="T">에셋 타입</typeparam>
        /// <param name="keys">프리로드할 에셋 키들</param>
        /// <param name="tag">태그 (선택사항, ReleaseByTag로 일괄 해제 가능)</param>
        /// <remarks>
        /// 사용 예시:
        /// <code>
        /// await AddressableManager.Instance.PreloadAssetsAsync&lt;Texture&gt;(new[] { "Tex1", "Tex2" }, "Common");
        /// // 이제 Tex1, Tex2가 메모리에 로드되어 즉시 사용 가능
        /// </code>
        /// </remarks>
        public async Task PreloadAssetsAsync<T>(IEnumerable<string> keys, string tag = null)
        {
            await LoadAssetsAsync<T>(keys, tag);
        }

        /// <summary>
        /// 에셋이 로드되어 있는지 확인합니다.
        /// </summary>
        /// <param name="key">에셋 키</param>
        /// <returns>로드되어 있으면 true</returns>
        public bool IsAssetLoaded(string key)
        {
            return _trackedResources.TryGetValue(key, out var tracked) && tracked.Handle.IsValid();
        }

        /// <summary>
        /// Scene이 로드되어 있는지 확인합니다.
        /// </summary>
        /// <param name="key">Scene 키</param>
        /// <returns>로드되어 있으면 true</returns>
        public bool IsSceneLoaded(string key)
        {
            return _trackedScenes.ContainsKey(key);
        }

        /// <summary>
        /// 이미 로드된 에셋을 RefCount 증가 없이 반환합니다.
        /// </summary>
        /// <typeparam name="T">에셋 타입</typeparam>
        /// <param name="key">에셋 키</param>
        /// <returns>로드된 에셋 (없으면 null)</returns>
        /// <remarks>
        /// 주의: RefCount가 증가하지 않으므로 수동으로 ReleaseAsset을 호출할 필요가 없습니다.
        /// 읽기 전용으로 사용하세요.
        /// </remarks>
        public T GetLoadedAsset<T>(string key) where T : class
        {
            if (_trackedResources.TryGetValue(key, out var tracked) && tracked.Handle.IsValid())
            {
                if (tracked.Handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return tracked.Handle.Result as T;
                }
            }
            return null;
        }

        #endregion

        #region Phase 5: 진단 및 디버깅 도구

        /// <summary>
        /// 특정 키의 RefCount를 조회합니다.
        /// </summary>
        /// <param name="key">에셋 키</param>
        /// <returns>RefCount (로드되지 않았으면 0)</returns>
        public int GetRefCount(string key)
        {
            if (_trackedResources.TryGetValue(key, out var tracked))
            {
                return tracked.RefCount;
            }
            return 0;
        }

        /// <summary>
        /// 현재 로드된 모든 에셋 키를 반환합니다.
        /// </summary>
        /// <returns>로드된 에셋 키 컬렉션</returns>
        public IEnumerable<string> GetLoadedKeys()
        {
            return _trackedResources.Keys;
        }

        /// <summary>
        /// 현재 로드된 모든 Scene 키를 반환합니다.
        /// </summary>
        /// <returns>로드된 Scene 키 컬렉션</returns>
        public IEnumerable<string> GetLoadedSceneKeys()
        {
            return _trackedScenes.Keys;
        }

        /// <summary>
        /// 특정 태그에 연결된 모든 키를 반환합니다.
        /// </summary>
        /// <param name="tag">태그</param>
        /// <returns>태그에 연결된 키 컬렉션</returns>
        public IEnumerable<string> GetTagKeys(string tag)
        {
            if (_tagToKeys.TryGetValue(tag, out var keys))
            {
                return keys;
            }
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// 현재 AddressableManager의 전체 상태를 로그로 출력합니다.
        /// 디버깅 및 모니터링 용도로 사용합니다.
        /// </summary>
        public void LogStatus()
        {
            Debug.Log("========== AddressableManager Status ==========");

            Debug.Log($"[Loaded Assets] Total: {_trackedResources.Count}");
            foreach (var kvp in _trackedResources)
            {
                Debug.Log($"  - Key: {kvp.Key}, RefCount: {kvp.Value.RefCount}, Status: {kvp.Value.Handle.Status}");
            }

            Debug.Log($"[Loaded Scenes] Total: {_trackedScenes.Count}");
            foreach (var kvp in _trackedScenes)
            {
                Debug.Log($"  - Key: {kvp.Key}, RefCount: {kvp.Value.RefCount}, Status: {kvp.Value.Handle.Status}");
            }

            Debug.Log($"[Pooled Instances] Total: {_trackedInstances.Count}");
            foreach (var kvp in _trackedInstances)
            {
                Debug.Log($"  - Instance: {kvp.Key.name}, Key: {kvp.Value.Key}");
            }

            Debug.Log($"[Tags] Total: {_tagToKeys.Count}");
            foreach (var kvp in _tagToKeys)
            {
                Debug.Log($"  - Tag: {kvp.Key}, Keys: [{string.Join(", ", kvp.Value)}]");
            }

            Debug.Log("===============================================");
        }

        #endregion

        #region Phase 6: 오브젝트 풀링 지원

        /// <summary>
        /// Addressables 내장 풀링 시스템을 사용하여 GameObject를 생성합니다 (콜백 방식).
        /// WARNING: 반드시 ReleaseInstance로 수동 해제해야 합니다.
        /// </summary>
        /// <param name="key">Addressable 키</param>
        /// <param name="parent">생성될 GameObject의 부모 Transform</param>
        /// <param name="onComplete">생성 완료 시 호출</param>
        /// <param name="onError">생성 실패 시 호출</param>
        /// <param name="tag">태그 (선택사항, ReleaseByTag로 일괄 해제 가능)</param>
        /// <remarks>
        /// LoadGameObject와 달리 AddressableLifecycleLinker를 부착하지 않습니다.
        /// 반드시 ReleaseInstance를 직접 호출해야 합니다.
        /// 오브젝트 풀링이 필요한 경우 (총알, 이펙트 등) 사용하세요.
        /// </remarks>
        public void InstantiateAsync(string key, Transform parent = null, Action<GameObject> onComplete = null, Action<Exception> onError = null, string tag = null)
        {
            var handle = Addressables.InstantiateAsync(key, parent);
            RegisterTag(tag, key);

            handle.Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    var instance = op.Result;
                    var tracked = new TrackedInstance
                    {
                        Handle = op,
                        Instance = instance,
                        Key = key
                    };
                    _trackedInstances.Add(instance, tracked);

                    onComplete?.Invoke(instance);
                }
                else
                {
                    onError?.Invoke(op.OperationException);
                }
            };
        }

        /// <summary>
        /// Addressables 내장 풀링 시스템을 사용하여 GameObject를 생성합니다 (async/await 패턴).
        /// WARNING: 반드시 ReleaseInstance로 수동 해제해야 합니다.
        /// </summary>
        /// <param name="key">Addressable 키</param>
        /// <param name="parent">생성될 GameObject의 부모 Transform</param>
        /// <param name="tag">태그 (선택사항, ReleaseByTag로 일괄 해제 가능)</param>
        /// <returns>생성된 GameObject</returns>
        /// <remarks>
        /// 사용 예시:
        /// <code>
        /// var bullet = await AddressableManager.Instance.InstantiateTaskAsync("Bullet", transform);
        /// // 사용 후 반드시 해제
        /// AddressableManager.Instance.ReleaseInstance(bullet);
        /// </code>
        /// </remarks>
        public async Task<GameObject> InstantiateTaskAsync(string key, Transform parent = null, string tag = null)
        {
            try
            {
                var handle = Addressables.InstantiateAsync(key, parent);
                RegisterTag(tag, key);

                var instance = await handle.Task;
                var tracked = new TrackedInstance
                {
                    Handle = handle,
                    Instance = instance,
                    Key = key
                };
                _trackedInstances.Add(instance, tracked);

                return instance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddressableManager] Error instantiating GameObject: {key}, Exception: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// InstantiateAsync로 생성된 GameObject를 해제합니다.
        /// </summary>
        /// <param name="instance">해제할 GameObject</param>
        /// <remarks>
        /// InstantiateAsync / InstantiateTaskAsync로 생성된 GameObject만 이 메서드로 해제하세요.
        /// LoadGameObject로 생성된 GameObject는 자동으로 해제되므로 이 메서드를 사용하지 마세요.
        /// </remarks>
        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null)
            {
                Debug.LogWarning("[AddressableManager] Trying to release null instance");
                return;
            }

            if (_trackedInstances.TryGetValue(instance, out var tracked))
            {
                Addressables.ReleaseInstance(tracked.Handle);
                _trackedInstances.Remove(instance);
            }
            else
            {
                Debug.LogWarning($"[AddressableManager] Trying to release unknown or already released instance: {instance.name}");
            }
        }

        #endregion

        #region Phase 7: 추가 async/await 메서드

        /// <summary>
        /// 단일 에셋을 로드합니다 (async/await 패턴).
        /// </summary>
        /// <typeparam name="T">에셋 타입</typeparam>
        /// <param name="key">에셋 키</param>
        /// <param name="tag">태그 (선택사항, ReleaseByTag로 일괄 해제 가능)</param>
        /// <returns>로드된 에셋</returns>
        /// <remarks>
        /// 사용 예시:
        /// <code>
        /// var sprite = await AddressableManager.Instance.LoadAssetAsync&lt;Sprite&gt;("Icon_Sword", "UI");
        /// // 사용 후 해제
        /// AddressableManager.Instance.ReleaseAsset("Icon_Sword");
        /// </code>
        /// </remarks>
        public async Task<T> LoadAssetAsync<T>(string key, string tag = null)
        {
            if (_trackedResources.TryGetValue(key, out var tracked))
            {
                tracked.RefCount++;
                RegisterTag(tag, key);

                if (!tracked.Handle.IsDone)
                {
                    await tracked.Handle.Task;
                }

                if (tracked.Handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return (T)tracked.Handle.Result;
                }
                else
                {
                    throw tracked.Handle.OperationException;
                }
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<T>(key);
                var newTracker = new TrackedResource { Handle = handle, RefCount = 1 };
                _trackedResources.Add(key, newTracker);
                RegisterTag(tag, key);

                var result = await handle.Task;
                return result;
            }
            catch (Exception ex)
            {
                _trackedResources.Remove(key);
                Debug.LogError($"[AddressableManager] Error loading asset async: {key}, Exception: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 단일 에셋을 프리로드합니다 (async/await 패턴).
        /// </summary>
        /// <typeparam name="T">에셋 타입</typeparam>
        /// <param name="key">에셋 키</param>
        /// <param name="tag">태그 (선택사항, ReleaseByTag로 일괄 해제 가능)</param>
        public async Task PreloadAssetAsync<T>(string key, string tag = null)
        {
            await LoadAssetAsync<T>(key, tag);
        }

        #endregion
    }
}
