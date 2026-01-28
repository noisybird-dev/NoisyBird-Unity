using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NoisyBird.MonoExtension
{
    [CreateAssetMenu(menuName = "NoisyBird/Singleton Registry", fileName = "SingletonRegistry")]
    public class SingletonRegistry : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string typeName;
            public GameObject prefab;
        }

        [SerializeField] private List<Entry> _entries = new();
        private Dictionary<string, Entry> _map;

        public static SingletonRegistry Instance
        {
            get
            {
                var reg = Resources.Load<SingletonRegistry>("NoisyBird/MonoExtension/SingletonRegistry");
                if (reg != null)
                    return reg;

#if UNITY_EDITOR
                var path = "Assets/Resources/NoisyBird/MonoExtension";
                var assetPath = $"{path}/SingletonRegistry.asset";

                // 폴더 생성
                if (!System.IO.Directory.Exists("Assets/Resources"))
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");

                if (!System.IO.Directory.Exists("Assets/Resources/NoisyBird"))
                    UnityEditor.AssetDatabase.CreateFolder("Assets/Resources", "NoisyBird");

                if (!System.IO.Directory.Exists("Assets/Resources/NoisyBird/MonoExtension"))
                    UnityEditor.AssetDatabase.CreateFolder("Assets/Resources/NoisyBird", "MonoExtension");

                reg = UnityEditor.AssetDatabase.LoadAssetAtPath<SingletonRegistry>(assetPath);
                if (reg == null)
                {
                    reg = ScriptableObject.CreateInstance<SingletonRegistry>();
                    UnityEditor.AssetDatabase.CreateAsset(reg, assetPath);
                    UnityEditor.AssetDatabase.SaveAssets();
                    UnityEditor.AssetDatabase.Refresh();
                    Debug.Log($"[NoisyBird] SingletonRegistry created at: {assetPath}");
                }
#endif

                return reg;
            }
        }

        void OnEnable()
        {
            _map = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var e in _entries)
            {
                if (string.IsNullOrEmpty(e.typeName)) continue;
                _map[e.typeName] = e;
            }
        }

        public bool TryGetEntry(Type type, out Entry entry)
        {
            if (_map == null) OnEnable();
            if (_map == null)
            {
                entry = null;
                return false;
            }
            return _map.TryGetValue(type?.FullName ?? string.Empty, out entry);
        }
        
        public void UpsertEntry(Type type, Entry e)
        {
            e.typeName = type.FullName;
            if (_map == null) OnEnable();
            if (_map == null) return;
            if (e.typeName != null)
            {
                _map[e.typeName] = e;
                var idx = _entries.FindIndex(x => x.typeName == e.typeName);
                if (idx >= 0) _entries[idx] = e;
                else _entries.Add(e);
            }
        }

        public Type GetTypeByString(string typeFullName)
        {
            Type type = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
                })
                .FirstOrDefault(t => t.FullName == typeFullName);

            return type;
        }
    }
}
