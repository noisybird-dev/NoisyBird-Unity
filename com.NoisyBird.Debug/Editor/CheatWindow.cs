using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace NoisyBird.Debug.Editor
{
    public class CheatWindow : EditorWindow
    {
        [MenuItem("Noisy Bird/Cheat Window")]
        public static void ShowWindow()
        {
            GetWindow<CheatWindow>("Cheat Window");
        }

        private class CheatItem
        {
            public MemberInfo Member;
            public object Target; // null for static
            public NBCheatAttribute Attribute;
            public string CategoryName;
        }

        private Dictionary<string, List<CheatItem>> _groupedCheats = new Dictionary<string, List<CheatItem>>();
        private Vector2 _scrollPosition;

        private void OnEnable()
        {
            RefreshCheatTypes();
        }

        private void RefreshCheatTypes()
        {
            _groupedCheats.Clear();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            // Collect methods, properties, fields with NBCheatAttribute
            // Binding flags: Public, NonPublic, Static, Instance
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

            foreach (var assembly in assemblies)
            {
                Type[] types;
                try 
                {
                    types = assembly.GetTypes();
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (var type in types)
                {
                    // Scan members
                    var members = type.GetMembers(flags)
                        .Where(m => Attribute.IsDefined(m, typeof(NBCheatAttribute)));

                    foreach (var member in members)
                    {
                        var attr = member.GetCustomAttribute<NBCheatAttribute>();
                        if (attr == null) continue;

                        string category = attr.Category?.ToString() ?? "Uncategorized";

                        // Determine targets
                        List<object> targets = new List<object>();

                        bool isStatic = false;
                        if (member is MethodInfo mi) isStatic = mi.IsStatic;
                        else if (member is PropertyInfo pi) isStatic = pi.GetGetMethod(true)?.IsStatic ?? pi.GetSetMethod(true)?.IsStatic ?? false;
                        else if (member is FieldInfo fi) isStatic = fi.IsStatic;

                        if (isStatic)
                        {
                            targets.Add(null);
                        }
                        else
                        {
                            // If instance, must be MonoBehaviour to find in scene
                            if (typeof(MonoBehaviour).IsAssignableFrom(type))
                            {
                                var objects = FindObjectsOfType(type);
                                foreach (var obj in objects)
                                {
                                    targets.Add(obj);
                                }
                            }
                        }

                        foreach (var target in targets)
                        {
                            if (!_groupedCheats.ContainsKey(category))
                            {
                                _groupedCheats[category] = new List<CheatItem>();
                            }

                            _groupedCheats[category].Add(new CheatItem
                            {
                                Member = member,
                                Target = target,
                                Attribute = attr,
                                CategoryName = category
                            });
                        }
                    }
                }
            }

            // Sort by Group
            foreach (var key in _groupedCheats.Keys.ToList())
            {
                _groupedCheats[key] = _groupedCheats[key].OrderBy(item => item.Attribute.Group).ToList();
            }
        }

        private void OnGUI()
        {
            DrawUseDebugToggle();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Refresh", GUILayout.Height(30)))
            {
                RefreshCheatTypes();
            }

            EditorGUILayout.Space(10);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_groupedCheats.Count == 0)
            {
                EditorGUILayout.HelpBox("No cheats found. Add [NBCheat] attribute to static methods/properties or instance MonoBehaviours in the scene.", MessageType.Info);
            }
            else
            {
                var sortedCategories = _groupedCheats.Keys.OrderBy(k => k).ToList();
                foreach (var category in sortedCategories)
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField(category, EditorStyles.boldLabel);
                    
                    foreach (var item in _groupedCheats[category])
                    {
                        DrawCheatItem(item);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(5);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawUseDebugToggle()
        {
            EditorGUILayout.BeginVertical("box");
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            string symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            var symbolList = symbols.Split(';').ToList();
            
            bool useDebug = symbolList.Contains("USE_DEBUG");
            bool newUseDebug = EditorGUILayout.Toggle("Enable USE_DEBUG", useDebug);
            
            bool useCheat = symbolList.Contains("USE_CHEAT");
            bool newUseCheat = EditorGUILayout.Toggle("Enable USE_CHEAT", useCheat);

            if (newUseDebug != useDebug || newUseCheat != useCheat)
            {
                if (newUseDebug && !useDebug) symbolList.Add("USE_DEBUG");
                else if (!newUseDebug && useDebug) symbolList.Remove("USE_DEBUG");

                if (newUseCheat && !useCheat) symbolList.Add("USE_CHEAT");
                else if (!newUseCheat && useCheat) symbolList.Remove("USE_CHEAT");

                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", symbolList));
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawCheatItem(CheatItem item)
        {
            // Prefix label (Target Name for instances, or just Member Name)
            string label = item.Target != null ? $"[{((UnityEngine.Object)item.Target).name}] {item.Member.Name}" : item.Member.Name;

            if (item.Member is MethodInfo method)
            {
                if (method.GetParameters().Length == 0)
                {
                     if (GUILayout.Button(label))
                     {
                         method.Invoke(item.Target, null);
                     }
                }
                else
                {
                    EditorGUILayout.LabelField(label, "Method with params not supported yet");
                }
            }
            else if (item.Member is PropertyInfo prop)
            {
                if (prop.CanRead && prop.CanWrite)
                {
                    DrawValueControl(label, prop.PropertyType, 
                        () => prop.GetValue(item.Target), 
                        (val) => prop.SetValue(item.Target, val));
                }
            }
            else if (item.Member is FieldInfo field)
            {
                DrawValueControl(label, field.FieldType, 
                    () => field.GetValue(item.Target), 
                    (val) => field.SetValue(item.Target, val));
            }
        }

        private void DrawValueControl(string label, Type type, Func<object> getter, Action<object> setter)
        {
            object value = getter();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(200));

            if (type == typeof(int))
            {
                var newValue = EditorGUILayout.IntField((int)value);
                if (newValue != (int)value) setter(newValue);
            }
            else if (type == typeof(float))
            {
                var newValue = EditorGUILayout.FloatField((float)value);
                if (Math.Abs(newValue - (float)value) > float.Epsilon) setter(newValue);
            }
            else if (type == typeof(string))
            {
                var valStr = value as string;
                var newValue = EditorGUILayout.TextField(valStr);
                if (newValue != valStr) setter(newValue);
            }
            else if (type == typeof(bool))
            {
                var newValue = EditorGUILayout.Toggle((bool)value);
                if (newValue != (bool)value) setter(newValue);
            }
            else if (type.IsEnum)
            {
                var newValue = EditorGUILayout.EnumPopup((Enum)value);
                if (!object.Equals(newValue, value)) setter(newValue);
            }
            else
            {
                 EditorGUILayout.LabelField($"TYPE NOT SUPPORTED: {type.Name}");
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
