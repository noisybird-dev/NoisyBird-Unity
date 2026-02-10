using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NoisyBird.EditorExtension.Editor
{
    public class JsonDiffEditor : EditorWindow
    {
        protected Vector2 scroll;
        protected List<(string line, bool isDiff)> diffResult = new();

        [MenuItem("Tools/JSON Diff Viewer")]
        public static void ShowWindow()
        {
            GetWindow<JsonDiffEditor>("JSON Diff Viewer").LoadData();
        }

        public virtual void LoadData()
        {
        }

        protected virtual void OnGUI()
        {
            if (diffResult.Count > 0)
            {
                GUILayout.Label("Diff Result", EditorStyles.boldLabel);
                scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(200));
                foreach (var line in diffResult)
                {
                    GUIStyle style = new(EditorStyles.label);
                    if (line.isDiff)
                        style.normal.textColor = Color.red;
                    EditorGUILayout.LabelField(line.line, style);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private List<(string line, bool isDiff)> CompareJson(string a, string b)
        {
            var result = new List<(string, bool)>();
            var linesA = a.Split('\n');
            var linesB = b.Split('\n');
            int max = Mathf.Max(linesA.Length, linesB.Length);

            for (int i = 0; i < max; i++)
            {
                string lineA = i < linesA.Length ? linesA[i].Trim() : "";
                string lineB = i < linesB.Length ? linesB[i].Trim() : "";

                if (lineA == lineB)
                    result.Add((lineA, false));
                else
                {
                    if (!string.IsNullOrEmpty(lineA)) result.Add(($"A: {lineA}", true));
                    if (!string.IsNullOrEmpty(lineB)) result.Add(($"B: {lineB}", true));
                }
            }

            return result;
        }
    }
}