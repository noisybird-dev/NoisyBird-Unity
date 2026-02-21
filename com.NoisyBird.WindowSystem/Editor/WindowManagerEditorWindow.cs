using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace NoisyBird.WindowSystem.Editor
{
    /// <summary>
    /// Window System을 관리하고 디버깅하기 위한 에디터 윈도우입니다.
    /// </summary>
    public class WindowManagerEditorWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _showRegisteredWindows = true;
        private bool _showScreenGroups = true;
        private bool _showNonStackWindows = true;
        private bool _showSavedStates = true;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double REFRESH_INTERVAL = 0.5;

        // 스타일
        private GUIStyle _headerStyle;
        private GUIStyle _windowItemStyle;
        private GUIStyle _stateItemStyle;
        private GUIStyle _groupStyle;
        private bool _stylesInitialized;

        [MenuItem("Noisy Bird/Window System/Window Manager")]
        public static void ShowWindow()
        {
            WindowManagerEditorWindow window = GetWindow<WindowManagerEditorWindow>("Window Manager");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            _lastRefreshTime = EditorApplication.timeSinceStartup;
        }

        private void OnGUI()
        {
            InitializeStyles();

            if (_autoRefresh && EditorApplication.timeSinceStartup - _lastRefreshTime > REFRESH_INTERVAL)
            {
                _lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }

            DrawToolbar();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawRegisteredWindows();
            DrawScreenGroups();
            DrawNonStackWindows();
            DrawSavedStates();

            EditorGUILayout.EndScrollView();
        }

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 5)
            };

            _windowItemStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 5, 5),
                margin = new RectOffset(0, 0, 2, 2)
            };

            _stateItemStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 5, 5),
                margin = new RectOffset(0, 0, 2, 2)
            };

            _groupStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(0, 0, 4, 4)
            };

            _stylesInitialized = true;
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                Repaint();
            }

            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", EditorStyles.toolbarButton, GUILayout.Width(100));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Close All Windows", EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                if (EditorUtility.DisplayDialog("Confirm", "Close all open windows?", "Yes", "No"))
                {
                    WindowManager.Instance?.CloseAllWindows(saveStates: true);
                }
            }

            if (GUILayout.Button("Destroy All", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("Confirm", "Destroy all windows? This cannot be undone.", "Yes", "No"))
                {
                    WindowManager.Instance?.DestroyAllWindows();
                }
            }

            if (GUILayout.Button("Clear All States", EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                if (EditorUtility.DisplayDialog("Confirm", "Clear all saved states?", "Yes", "No"))
                {
                    WindowManager.Instance?.ClearAllSavedStates();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawRegisteredWindows()
        {
            _showRegisteredWindows = EditorGUILayout.BeginFoldoutHeaderGroup(_showRegisteredWindows, "Registered Windows");

            if (_showRegisteredWindows)
            {
                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Window Manager is only available in Play Mode.", MessageType.Info);
                }
                else if (WindowManager.Instance == null)
                {
                    EditorGUILayout.HelpBox("WindowManager instance not found.", MessageType.Warning);
                }
                else
                {
                    var registeredWindows = GetRegisteredWindows();

                    if (registeredWindows.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No windows registered.", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"Total: {registeredWindows.Count}", EditorStyles.miniLabel);

                        foreach (var kvp in registeredWindows)
                        {
                            DrawWindowItem(kvp.Key, kvp.Value);
                        }
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);
        }

        private void DrawWindowItem(string windowId, WindowBase window)
        {
            if (window == null) return;

            EditorGUILayout.BeginVertical(_windowItemStyle);

            EditorGUILayout.BeginHorizontal();

            // Window 정보
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Window ID", windowId, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Type", window.WindowType.ToString());
            EditorGUILayout.LabelField("Is Open", window.IsOpen ? "O Yes" : "X No");
            EditorGUILayout.LabelField("GameObject", window.gameObject.name);
            EditorGUILayout.EndVertical();

            // 버튼들
            EditorGUILayout.BeginVertical(GUILayout.Width(100));

            GUI.enabled = !window.IsOpen;
            if (GUILayout.Button("Open"))
            {
                WindowManager.Instance.OpenWindow(windowId);
            }
            GUI.enabled = true;

            GUI.enabled = window.IsOpen;
            if (GUILayout.Button("Close"))
            {
                WindowManager.Instance.CloseWindow(windowId, saveState: true);
            }
            GUI.enabled = true;

            if (GUILayout.Button("Save State"))
            {
                WindowManager.Instance.SaveWindowState(windowId);
                Debug.Log($"[WindowManager] State saved for '{windowId}'");
            }

            if (GUILayout.Button("Select"))
            {
                Selection.activeGameObject = window.gameObject;
                EditorGUIUtility.PingObject(window.gameObject);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawScreenGroups()
        {
            _showScreenGroups = EditorGUILayout.BeginFoldoutHeaderGroup(_showScreenGroups, "Screen Groups (Screen/Popup Stack)");

            if (_showScreenGroups)
            {
                if (!Application.isPlaying || WindowManager.Instance == null)
                {
                    EditorGUILayout.HelpBox("Window Manager is only available in Play Mode.", MessageType.Info);
                }
                else
                {
                    var screenGroups = GetScreenGroups();

                    if (screenGroups == null || screenGroups.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No screen groups are currently active.", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"Total Groups: {screenGroups.Count}", EditorStyles.miniLabel);

                        int index = 0;
                        foreach (var group in screenGroups)
                        {
                            bool isTop = (index == 0); // Stack 순회 시 첫 번째가 Top
                            DrawScreenGroup(group, index, isTop);
                            index++;
                        }
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);
        }

        private void DrawScreenGroup(object group, int stackIndex, bool isTop)
        {
            if (group == null) return;

            // Reflection으로 ScreenGroup 필드 접근
            var groupType = group.GetType();
            var screenField = groupType.GetField("Screen");
            var popupsField = groupType.GetField("Popups");

            if (screenField == null || popupsField == null) return;

            var screen = screenField.GetValue(group) as WindowBase;
            var popups = popupsField.GetValue(group) as List<WindowBase>;

            if (screen == null) return;

            EditorGUILayout.BeginVertical(_groupStyle);

            string label = isTop ? $"[Active] Screen: {screen.WindowId}" : $"[Hidden] Screen: {screen.WindowId}";
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            if (popups != null && popups.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var popup in popups)
                {
                    if (popup != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"Popup: {popup.WindowId}");

                        if (isTop && GUILayout.Button("Close", GUILayout.Width(60)))
                        {
                            WindowManager.Instance.CloseWindow(popup.WindowId);
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("(No popups)", EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }

            if (isTop)
            {
                if (GUILayout.Button("Close Screen", GUILayout.Height(20)))
                {
                    WindowManager.Instance.CloseWindow(screen.WindowId);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawNonStackWindows()
        {
            _showNonStackWindows = EditorGUILayout.BeginFoldoutHeaderGroup(_showNonStackWindows, "Non-Stack Windows (Underlay/Toast/Overlay)");

            if (_showNonStackWindows)
            {
                if (!Application.isPlaying || WindowManager.Instance == null)
                {
                    EditorGUILayout.HelpBox("Window Manager is only available in Play Mode.", MessageType.Info);
                }
                else
                {
                    var nonStackWindows = GetNonStackWindows();

                    if (nonStackWindows.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No non-stack windows are currently open.", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"Total: {nonStackWindows.Count}", EditorStyles.miniLabel);

                        foreach (var window in nonStackWindows)
                        {
                            if (window != null)
                            {
                                EditorGUILayout.BeginHorizontal(_windowItemStyle);
                                EditorGUILayout.LabelField(window.WindowId, EditorStyles.boldLabel);
                                EditorGUILayout.LabelField(window.WindowType.ToString(), GUILayout.Width(80));

                                if (GUILayout.Button("Close", GUILayout.Width(60)))
                                {
                                    WindowManager.Instance.CloseWindow(window.WindowId, saveState: true);
                                }

                                EditorGUILayout.EndHorizontal();
                            }
                        }
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);
        }

        private void DrawSavedStates()
        {
            _showSavedStates = EditorGUILayout.BeginFoldoutHeaderGroup(_showSavedStates, "Saved States");

            if (_showSavedStates)
            {
                if (!Application.isPlaying || WindowManager.Instance == null)
                {
                    EditorGUILayout.HelpBox("Window Manager is only available in Play Mode.", MessageType.Info);
                }
                else
                {
                    var savedStates = GetSavedStates();

                    if (savedStates.Count == 0)
                    {
                        EditorGUILayout.HelpBox("No saved states.", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"Total: {savedStates.Count}", EditorStyles.miniLabel);

                        foreach (var kvp in savedStates)
                        {
                            DrawStateItem(kvp.Key, kvp.Value);
                        }
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(5);
        }

        private void DrawStateItem(string windowId, WindowState state)
        {
            EditorGUILayout.BeginHorizontal(_stateItemStyle);

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Window ID", windowId, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("State Type", state.GetType().Name);

            // AutoWindowState인 경우 상세 정보 표시
            if (state is AutoWindowState autoState)
            {
                EditorGUILayout.LabelField($"Saved Properties: {autoState.StateData.Count}");

                EditorGUI.indentLevel++;
                foreach (var data in autoState.StateData)
                {
                    EditorGUILayout.LabelField($"- {data.Key}", data.Value?.ToString() ?? "null", EditorStyles.miniLabel);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(100));

            if (GUILayout.Button("Restore"))
            {
                if (WindowManager.Instance.IsWindowRegistered(windowId))
                {
                    WindowManager.Instance.OpenWindow(windowId, restoreState: true);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", $"Window '{windowId}' is not registered.", "OK");
                }
            }

            if (GUILayout.Button("Clear"))
            {
                WindowManager.Instance.ClearSavedState(windowId);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        // Reflection을 사용하여 private 필드 접근
        private Dictionary<string, WindowBase> GetRegisteredWindows()
        {
            if (WindowManager.Instance == null) return new Dictionary<string, WindowBase>();

            var field = typeof(WindowManager).GetField("_registeredWindows",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                return field.GetValue(WindowManager.Instance) as Dictionary<string, WindowBase>
                    ?? new Dictionary<string, WindowBase>();
            }

            return new Dictionary<string, WindowBase>();
        }

        private Stack<object> GetScreenGroups()
        {
            if (WindowManager.Instance == null) return null;

            var field = typeof(WindowManager).GetField("_screenGroups",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field == null) return null;

            // Stack<ScreenGroup>을 Stack<object>로 변환
            var rawStack = field.GetValue(WindowManager.Instance);
            if (rawStack == null) return null;

            var result = new Stack<object>();
            var enumerator = rawStack as System.Collections.IEnumerable;
            if (enumerator != null)
            {
                var tempList = new List<object>();
                foreach (var item in enumerator)
                {
                    tempList.Add(item);
                }
                // Stack 순회는 Top→Bottom이므로 그대로 유지
                foreach (var item in tempList)
                {
                    result.Push(item);
                }
            }

            return result;
        }

        private List<WindowBase> GetNonStackWindows()
        {
            if (WindowManager.Instance == null) return new List<WindowBase>();

            var field = typeof(WindowManager).GetField("_nonStackWindows",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                return field.GetValue(WindowManager.Instance) as List<WindowBase>
                    ?? new List<WindowBase>();
            }

            return new List<WindowBase>();
        }

        private Dictionary<string, WindowState> GetSavedStates()
        {
            if (WindowManager.Instance == null) return new Dictionary<string, WindowState>();

            var field = typeof(WindowManager).GetField("_savedStates",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                return field.GetValue(WindowManager.Instance) as Dictionary<string, WindowState>
                    ?? new Dictionary<string, WindowState>();
            }

            return new Dictionary<string, WindowState>();
        }
    }
}
