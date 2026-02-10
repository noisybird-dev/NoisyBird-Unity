using UnityEditor;
using UnityEngine;
using System;

namespace NoisyBird.EditorExtension.Editor
{
    public class TextInputPopup : EditorWindow
    {
        private string input = "";
        private Action<string> onSubmit;

        public static void Show(string title, string defaultText = "", Action<string> callback = null)
        {
            var window = ScriptableObject.CreateInstance<TextInputPopup>();
            window.titleContent = new GUIContent(title);
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 80);
            window.onSubmit = callback;
            window.input = defaultText;
            window.ShowUtility(); // 팝업처럼 작게 뜨는 창
        }

        private void OnGUI()
        {
            GUILayout.Label("입력:", EditorStyles.label);
            GUI.SetNextControlName("InputField");
            input = EditorGUILayout.TextField(input);

            GUILayout.Space(10);

            if (GUILayout.Button("확인"))
            {
                onSubmit?.Invoke(input);
                Close();
            }

            if (GUILayout.Button("취소"))
            {
                Close();
            }

            // Enter로 입력 가능
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                onSubmit?.Invoke(input);
                Close();
            }

            EditorGUI.FocusTextInControl("InputField");
        }
    }
}