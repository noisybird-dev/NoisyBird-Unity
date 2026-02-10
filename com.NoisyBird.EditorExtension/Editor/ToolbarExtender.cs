using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace NoisyBird.EditorExtension.Editor
{
    [InitializeOnLoad]
    public static class ToolbarExtender
    {
        static int m_toolCount;
        static GUIStyle m_commandStyle = null;

        public static readonly List<Action> LeftToolbarGUI = new List<Action>();
        public static readonly List<Action> RightToolbarGUI = new List<Action>();

        static ToolbarExtender()
        {
            Type toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");

#if UNITY_2021_1_OR_NEWER
            string fieldName = "k_ToolCount";
#else
            string fieldName = "get_toolCount";
#endif

            FieldInfo toolIcons = toolbarType.GetField(fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            m_toolCount = toolIcons != null ? (int)toolIcons.GetValue(null) : 8;

#if UNITY_2021_1_OR_NEWER
            ToolbarCallback.OnToolbarGUI -= OnGUI;
            ToolbarCallback.OnToolbarGUI += OnGUI;
#else
            ToolbarCallback.OnToolbarGUILeft = GUILeft;
            ToolbarCallback.OnToolbarGUIRight = GUIRight;
#endif
        }

#if UNITY_2021_1_OR_NEWER
        static void OnGUI()
        {
            GUILayout.BeginHorizontal();
            foreach (var handler in LeftToolbarGUI)
            {
                handler();
            }
            GUILayout.FlexibleSpace();
            foreach (var handler in RightToolbarGUI)
            {
                handler();
            }
            GUILayout.EndHorizontal();
        }
#else
        static void GUILeft()
        {
            GUILayout.BeginHorizontal();
            foreach (var handler in LeftToolbarGUI)
            {
                handler();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        static void GUIRight()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            foreach (var handler in RightToolbarGUI)
            {
                handler();
            }
            GUILayout.EndHorizontal();
        }
#endif

        public static GUIStyle CommandStyle
        {
            get
            {
                if (m_commandStyle == null)
                {
                    m_commandStyle = new GUIStyle("Command")
                    {
                        fontSize = 16,
                        alignment = TextAnchor.MiddleCenter,
                        imagePosition = ImagePosition.ImageAbove,
                        fontStyle = FontStyle.Bold
                    };
                }
                return m_commandStyle;
            }
        }
    }

#if UNITY_2021_1_OR_NEWER
    public static class ToolbarCallback
    {
        static Type m_toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        static Type m_guiViewType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GUIView");
        static PropertyInfo m_viewVisualTree = m_guiViewType.GetProperty("visualTree",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo m_imguiContainerOnGui = typeof(UnityEngine.UIElements.IMGUIContainer).GetField("m_OnGUIHandler",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        static ScriptableObject m_currentToolbar;

        public static Action OnToolbarGUI;

        static ToolbarCallback()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        static void OnUpdate()
        {
            if (m_currentToolbar == null)
            {
                var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);
                m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
                if (m_currentToolbar != null)
                {
                    var visualTree = m_viewVisualTree.GetValue(m_currentToolbar, null) as UnityEngine.UIElements.VisualElement;
                    var container = (UnityEngine.UIElements.IMGUIContainer)visualTree[0];

                    var handler = (Action)m_imguiContainerOnGui.GetValue(container);
                    handler -= OnGUI;
                    handler += OnGUI;
                    m_imguiContainerOnGui.SetValue(container, handler);
                }
            }
        }

        static void OnGUI()
        {
            var handler = OnToolbarGUI;
            if (handler != null) handler();
        }
    }
#else
    public static class ToolbarCallback
    {
        static Type m_toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        static ScriptableObject m_currentToolbar;
        public static Action OnToolbarGUILeft;
        public static Action OnToolbarGUIRight;

        static ToolbarCallback()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        static void OnUpdate()
        {
            if (m_currentToolbar == null)
            {
                var toolbars = Resources.FindObjectsOfTypeAll(m_toolbarType);
                m_currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
            }
        }
    }
#endif
}
