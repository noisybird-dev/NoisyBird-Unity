using UnityEngine;

namespace NoisyBird.MonoExtension
{
    public class MonoSingleton<T> : CallBackMonoBehaviour<MonoSingleton<T>> where T : MonoSingleton<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance != null) return _instance;
#if UNITY_2023_1_OR_NEWER
                _instance = FindFirstObjectByType<T>();
#else
                _instance = FindObjectOfType<T>();
#endif

                if (_instance != null) return _instance;

                var created = CreateFromRegistrySync();
                if (created) return _instance;

                var go = new GameObject(typeof(T).Name);
                _instance = go.AddComponent<T>();
                return _instance;
            }
        }

        [SerializeField] protected bool dontDestroyOnLoad;

        public override void Awake()
        {
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
            base.Awake();
        }
        
        private static bool CreateFromRegistrySync()
        {
            var reg = SingletonRegistry.Instance;
            if (reg == null) return false;

            if (!reg.TryGetEntry(typeof(T), out var entry))
                return false;
            
            if (entry.prefab == null || entry.prefab.GetComponent<T>() == null)
                return false;

            var go = Instantiate(entry.prefab);
            _instance = go.GetComponent<T>();
            return true;
        }
    }
}