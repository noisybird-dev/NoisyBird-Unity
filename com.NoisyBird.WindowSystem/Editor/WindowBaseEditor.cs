using UnityEngine;
using UnityEditor;

namespace NoisyBird.WindowSystem.Editor
{
    /// <summary>
    /// WindowBase의 커스텀 인스펙터입니다.
    /// </summary>
    [CustomEditor(typeof(WindowBase), true)]
    public class WindowBaseEditor : UnityEditor.Editor
    {
        private SerializedProperty _windowIdProp;
        private SerializedProperty _windowTypeProp;

        private void OnEnable()
        {
            _windowIdProp = serializedObject.FindProperty("_windowId");
            _windowTypeProp = serializedObject.FindProperty("_windowType");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            WindowBase window = (WindowBase)target;

            // Header
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Window Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            // Window ID
            EditorGUILayout.PropertyField(_windowIdProp, new GUIContent("Window ID"));

            // Auto-fill button
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            if (GUILayout.Button("Auto-fill from GameObject", GUILayout.Height(18)))
            {
                _windowIdProp.stringValue = window.gameObject.name;
                serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            // Window Type
            EditorGUILayout.PropertyField(_windowTypeProp, new GUIContent("Window Type"));

            EditorGUILayout.Space(10);

            // Runtime Info
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);

                GUI.enabled = false;
                EditorGUILayout.Toggle("Is Open", window.IsOpen);
                EditorGUILayout.Toggle("Is Registered", WindowManager.Instance?.IsWindowRegistered(window.WindowId) ?? false);
                GUI.enabled = true;

                EditorGUILayout.Space(5);

                // Control Buttons
                EditorGUILayout.BeginHorizontal();

                GUI.enabled = !window.IsOpen && WindowManager.Instance != null;
                if (GUILayout.Button("Open Window"))
                {
                    WindowManager.Instance.OpenWindow(window.WindowId);
                }
                GUI.enabled = true;

                GUI.enabled = window.IsOpen && WindowManager.Instance != null;
                if (GUILayout.Button("Close Window"))
                {
                    WindowManager.Instance.CloseWindow(window.WindowId, saveState: true);
                }
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();

                GUI.enabled = WindowManager.Instance != null;
                if (GUILayout.Button("Save State"))
                {
                    WindowManager.Instance.SaveWindowState(window.WindowId);
                    Debug.Log($"[WindowBaseEditor] State saved for '{window.WindowId}'");
                }

                if (GUILayout.Button("Restore State"))
                {
                    var state = WindowManager.Instance.GetSavedState(window.WindowId);
                    if (state != null)
                    {
                        window.RestoreState(state);
                        Debug.Log($"[WindowBaseEditor] State restored for '{window.WindowId}'");
                    }
                    else
                    {
                        Debug.LogWarning($"[WindowBaseEditor] No saved state for '{window.WindowId}'");
                    }
                }
                GUI.enabled = true;

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Open Window Manager
                if (GUILayout.Button("Open Window Manager"))
                {
                    WindowManagerEditorWindow.ShowWindow();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("런타임 정보는 플레이 모드에서만 표시됩니다.", MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // Draw default inspector for derived class fields
            DrawPropertiesExcluding(serializedObject, "_windowId", "_windowType");

            serializedObject.ApplyModifiedProperties();
        }
    }
}
