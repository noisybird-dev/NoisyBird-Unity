using System;
using UnityEditor;
using UnityEngine;

namespace NoisyBird.EditorExtension.Editor
{
    public static class EditorGUIHelper
    {
        public static bool DrawCustomHeader(string title, string key, ref bool isCheck, Action<Rect> rightEndGUI = null)
        {
            EditorGUILayout.BeginHorizontal();
            isCheck = GUILayout.Toggle(isCheck, "", GUILayout.Width(30f));
            bool isDraw = DrawCustomHeader(title, key, rightEndGUI);
            EditorGUILayout.EndHorizontal();
            return isDraw;
        }
        
        public static bool DrawCustomHeader(string title, string key, Action<Rect> rightEndGUI)
        {
            key = $"DrawHeader_{key}";
            bool state = EditorPrefs.GetBool(key, false);

            Rect rect = GUILayoutUtility.GetRect(1f, 22f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f));

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleLeft,
                richText = true
            };
            
            rightEndGUI?.Invoke(rect);

            if (GUI.Button(rect, title, style))
            {
                state = !state;
                EditorPrefs.SetBool(key, state);
            }

            return state;
        }
    }
}