using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NoisyBird.Debug.Runtime
{
    public class RuntimeCheatUI : MonoBehaviour
    {
        private class CheatItem
        {
            public MemberInfo Member;
            public object Target; // null for static
            public NBCheatAttribute Attribute;
            public string CategoryName;
        }

        private static RuntimeCheatUI _instance;
        private bool _isShow = false;
        private Dictionary<string, List<CheatItem>> _groupedCheats = new Dictionary<string, List<CheatItem>>();
        private Vector2 _scrollPosition;
        private Rect _windowRect = new Rect(20, 20, 600, 900);

        private bool _isSearchDomain = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
#if USE_CHEAT && !UNITY_EDITOR
            if (_instance == null)
            {
                var go = new GameObject("RuntimeCheatUI");
                _instance = go.AddComponent<RuntimeCheatUI>();
                DontDestroyOnLoad(go);
            }
#endif
        }

        private void Start()
        {
            if (_isSearchDomain) return;
            RefreshCheatTypes();
        }

        private void RefreshCheatTypes()
        {
            _groupedCheats.Clear();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
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
                    var members = type.GetMembers(flags)
                        .Where(m => Attribute.IsDefined(m, typeof(NBCheatAttribute)));

                    foreach (var member in members)
                    {
                        var attr = member.GetCustomAttribute<NBCheatAttribute>();
                        if (attr == null) continue;

                        string category = attr.Category?.ToString() ?? "Uncategorized";

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

            foreach (var key in _groupedCheats.Keys.ToList())
            {
                _groupedCheats[key] = _groupedCheats[key].OrderBy(item => item.Attribute.Group).ToList();
            }

            _isSearchDomain = true;
        }

        private const float kReferenceHeight = 1080f;
        private float _scale = 1.0f;
        private Vector2 _currentTouchOffset = Vector2.zero;

        private void OnGUI()
        {
            _scale = Screen.height / kReferenceHeight;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * _scale);

            _currentTouchOffset = Vector2.zero; // Set offset for global GUI elements

            if (!_isShow)
            {
                var safeArea = Screen.safeArea;
                // GUI coordinates start from top-left, Screen coordinates from bottom-left
                float safeY = Screen.height - safeArea.yMax;
                
                // Convert screen coordinates to scaled reference coordinates
                float scaledSafeX = (safeArea.x + 10) / _scale;
                float scaledSafeY = (safeY + 10) / _scale;
                
                Rect btnRect = new Rect(scaledSafeX, scaledSafeY, 150, 50);

                if (CustomButton(btnRect, "Cheat"))
                {
                    _isShow = true;
                    RefreshCheatTypes(); // Refresh when opening to get latest scene objects
                }
                return;
            }

            _windowRect = GUI.Window(0, _windowRect, DrawWindow, "Cheat Window");
        }

        private bool CustomButton(Rect rect, string text)
        {
            if (GUI.Button(rect, text)) return true;

            // Manual Touch Check
            if (Input.touchCount > 0)
            {
                foreach (var touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Ended)
                    {
                        // Convert touch screen position to GUI Scaled coordinate system
                        Vector2 screenPos = touch.position;
                        Vector2 guiScreenPos = new Vector2(screenPos.x, Screen.height - screenPos.y); // Flip Y
                        Vector2 scaledPos = guiScreenPos / _scale;

                        // Check if touch is inside the button rect (accounting for current offset)
                        Rect globalRect = new Rect(rect.position + _currentTouchOffset, rect.size);
                        if (globalRect.Contains(scaledPos)) return true;
                    }
                }
            }
            
            return false;
        }

        private void DrawWindow(int windowID)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close"))
            {
                _isShow = false;
            }
            if (GUILayout.Button("Refresh"))
            {
                RefreshCheatTypes();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            if (_groupedCheats.Count == 0)
            {
                GUILayout.Label("No cheats found.");
            }
            else
            {
                var sortedCategories = _groupedCheats.Keys.OrderBy(k => k).ToList();
                foreach (var category in sortedCategories)
                {
                    GUILayout.Label(category, GUI.skin.FindStyle("BoldLabel") ?? GUI.skin.label);
                    GUILayout.BeginVertical("box");
                    foreach (var item in _groupedCheats[category])
                    {
                        DrawCheatItem(item);
                    }
                    GUILayout.EndVertical();
                    GUILayout.Space(5);
                }
            }

            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private void DrawCheatItem(CheatItem item)
        {
            string label = item.Target != null ? $"[{((UnityEngine.Object)item.Target).name}] {item.Member.Name}" : item.Member.Name;

            GUILayout.BeginHorizontal();
            
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
                     GUILayout.Label($"{label} (Params not supported)");
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

            GUILayout.EndHorizontal();
        }

        private void DrawValueControl(string label, Type type, Func<object> getter, Action<object> setter)
        {
            object value = getter();
            GUILayout.Label(label, GUILayout.Width(150));

            if (type == typeof(int))
            {
                string valStr = GUILayout.TextField(value.ToString());
                if (int.TryParse(valStr, out int res) && res != (int)value) setter(res);
            }
            else if (type == typeof(float))
            {
                string valStr = GUILayout.TextField(value.ToString());
                if (float.TryParse(valStr, out float res) && Math.Abs(res - (float)value) > float.Epsilon) setter(res);
            }
            else if (type == typeof(string))
            {
                string valStr = GUILayout.TextField(value as string ?? "");
                if (valStr != (string)value) setter(valStr);
            }
            else if (type == typeof(bool))
            {
                bool valBool = GUILayout.Toggle((bool)value, "");
                if (valBool != (bool)value) setter(valBool);
            }
            else if (type.IsEnum)
            {
                // Simple enum handling for runtime GUI (cycling button)
                if (GUILayout.Button(value.ToString()))
                {
                    Array values = Enum.GetValues(type);
                    int index = Array.IndexOf(values, value);
                    index = (index + 1) % values.Length;
                    setter(values.GetValue(index));
                }
            }
            else
            {
                GUILayout.Label($"Type {type.Name} not supported");
            }
        }
    }
}
