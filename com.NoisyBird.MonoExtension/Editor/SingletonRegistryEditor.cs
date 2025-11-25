#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NoisyBird.MonoExtension;
using UnityEditor;
using UnityEngine;

namespace NoisyBird.MonoExtension.Editor
{
    [CustomEditor(typeof(SingletonRegistry))]
    public class SingletonRegistryEditor : UnityEditor.Editor
    {
        private static bool _isChecked = false;

        private static List<Type> _monoSingletonTypes = new List<Type>();
        
        public override void OnInspectorGUI()
        {
            var registry = target as SingletonRegistry;
            if (registry == null) return;

            if (_isChecked == false)
            {
                _monoSingletonTypes.Clear();
                var genericDef = typeof(MonoSingleton<>);

                _monoSingletonTypes.AddRange(
                    AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a =>
                        {
                            try { return a.GetTypes(); }
                            catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
                        })
                        .Where(t =>
                                t.IsClass &&
                                !t.IsAbstract &&
                                !t.ContainsGenericParameters &&        // 아직 제네릭 파라미터 남은 타입 제외
                                t.FullName != null &&                  // 동적/부분 타입 필터
                                IsSubclassOfRawGeneric(t, genericDef)  // 핵심: 오픈 제네릭 기반 상속 검사
                        )
                );
                _isChecked = true;
            }

            bool isChanged = false;
            EditorGUILayout.BeginVertical();
            {
                foreach (var type in _monoSingletonTypes)
                {
                    if (registry.TryGetEntry(type, out SingletonRegistry.Entry entry) == false)
                    {
                        registry.UpsertEntry(type, new SingletonRegistry.Entry
                        {
                            typeName = type.FullName,
                            prefab = null,
                        });
                    }

                    if (DrawEntryField(entry, () => { Undo.RecordObject(registry, "Modify SingletonRegistry"); }))
                    {
                        isChanged = true;
                    }
                    EditorGUILayout.Space(10);
                }

                if (_monoSingletonTypes.Count <= 0)
                {
                    EditorGUILayout.LabelField("There is no child class of MonoSingleton<>");
                }
            } 
            EditorGUILayout.EndVertical();

            if (isChanged)
            {
                EditorUtility.SetDirty(target);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>is changed</returns>
        private bool DrawEntryField(SingletonRegistry.Entry entry, Action onChanged)
        {
            var typeLastName = entry.typeName.Split('.')[^1];
            var newPrefab = EditorGUILayout.ObjectField(typeLastName, entry.prefab, typeof(GameObject), allowSceneObjects: false) as GameObject;
            if (newPrefab != null && newPrefab.Equals(entry.prefab) == false)
            {
                onChanged?.Invoke();
                entry.prefab = newPrefab;
                return true;
            }
            return false;
        }
        
        static bool IsSubclassOfRawGeneric(Type toCheck, Type genericDef)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (cur == genericDef) return true;
                toCheck = toCheck.BaseType;
            }
            return false;
        }
    }
}
#endif