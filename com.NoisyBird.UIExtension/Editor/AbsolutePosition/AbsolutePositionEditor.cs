using UnityEditor;
using UnityEngine;
using NoisyBird.UIExtension.UI;

namespace NoisyBird.UIExtension.Editor.AbsolutePosition
{
    [CustomEditor(typeof(UIExtension.UI.AbsolutePosition))]
    [CanEditMultipleObjects]
    public class AbsolutePositionEditor : UnityEditor.Editor
    {
        private SerializedProperty _anchorTargetProp;
        private SerializedProperty _anchorPresetProp;
        private SerializedProperty _offsetProp;
        private SerializedProperty _sizeProp;

        private static readonly string[] AnchorPresetNames = new string[]
        {
            "Top Left", "Top Center", "Top Right",
            "Middle Left", "Middle Center", "Middle Right",
            "Bottom Left", "Bottom Center", "Bottom Right"
        };

        private UnityEditor.Editor _rectTransformEditor;
        private bool _showRectTransform = false;

        private SerializedProperty _stretchModeProp;

        private void OnEnable()
        {
            _anchorTargetProp = serializedObject.FindProperty("_anchorTarget");
            _anchorPresetProp = serializedObject.FindProperty("_anchorPreset");
            _stretchModeProp = serializedObject.FindProperty("_stretchMode");
            _offsetProp = serializedObject.FindProperty("_offset");
            _sizeProp = serializedObject.FindProperty("_size");
        }

        private void OnDisable()
        {
            if (_rectTransformEditor != null)
            {
                DestroyImmediate(_rectTransformEditor);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(5);
            
            // 경고 메시지
            EditorGUILayout.HelpBox(
                "이 컴포넌트는 RectTransform을 자동으로 관리합니다.\n" +
                "RectTransform을 직접 수정하지 마세요.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // Anchor Target
            EditorGUILayout.PropertyField(_anchorTargetProp, new GUIContent("Anchor Target", "SafeArea 또는 Canvas를 기준으로 위치를 설정합니다."));

            EditorGUILayout.Space(5);

            // Anchor Preset Grid (3x3)
            EditorGUILayout.LabelField("Anchor Preset", EditorStyles.boldLabel);
            DrawAnchorPresetGrid();
            
            EditorGUILayout.Space(5);
            
            // Stretch Buttons
            DrawStretchButtons();

            EditorGUILayout.Space(10);

            // Offset
            EditorGUILayout.PropertyField(_offsetProp, new GUIContent("Offset", "앵커 포인트로부터의 오프셋"));

            // Size
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(new GUIContent("Size", "UI 요소의 크기. Stretch 모드일 경우 해당 축의 값은 무시됩니다."));
            
            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 13; // "X", "Y" 라벨 너비

            UIExtension.UI.AbsolutePosition.StretchMode currentStretchMode = (UIExtension.UI.AbsolutePosition.StretchMode)_stretchModeProp.enumValueIndex;

            // X
            bool disableX = (currentStretchMode == UIExtension.UI.AbsolutePosition.StretchMode.Width || 
                             currentStretchMode == UIExtension.UI.AbsolutePosition.StretchMode.All);
            EditorGUI.BeginDisabledGroup(disableX);
            EditorGUILayout.PropertyField(_sizeProp.FindPropertyRelative("x"), new GUIContent("X"));
            EditorGUI.EndDisabledGroup();

            // Y
            bool disableY = (currentStretchMode == UIExtension.UI.AbsolutePosition.StretchMode.Height || 
                             currentStretchMode == UIExtension.UI.AbsolutePosition.StretchMode.All);
            EditorGUI.BeginDisabledGroup(disableY);
            EditorGUILayout.PropertyField(_sizeProp.FindPropertyRelative("y"), new GUIContent("Y"));
            EditorGUI.EndDisabledGroup();

            EditorGUIUtility.labelWidth = originalLabelWidth;
            EditorGUI.indentLevel = oldIndent;
            
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 현재 설정 정보 표시
            if (Application.isPlaying || !Application.isPlaying)
            {
                UIExtension.UI.AbsolutePosition absPos = target as UIExtension.UI.AbsolutePosition;
                if (absPos != null)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Current Settings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("Target", absPos.Target.ToString());
                    EditorGUILayout.LabelField("Preset", GetPresetDisplayName(absPos.Preset));
                    EditorGUILayout.LabelField("Stretch", absPos.Stretch.ToString());
                    EditorGUILayout.LabelField("Offset", absPos.Offset.ToString("F2"));
                    EditorGUILayout.LabelField("Size", absPos.Size.ToString("F2"));
                    EditorGUI.indentLevel--;
                }
            }

            serializedObject.ApplyModifiedProperties();

            // RectTransform 정보 (읽기 전용, 접힌 상태)
            EditorGUILayout.Space(10);
            _showRectTransform = EditorGUILayout.Foldout(_showRectTransform, "RectTransform (Read Only)", true);
            
            if (_showRectTransform)
            {
                EditorGUI.BeginDisabledGroup(true);
                
                RectTransform rectTransform = (target as UIExtension.UI.AbsolutePosition)?.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    if (_rectTransformEditor == null)
                    {
                        _rectTransformEditor = CreateEditor(rectTransform);
                    }
                    
                    _rectTransformEditor.OnInspectorGUI();
                }
                
                EditorGUI.EndDisabledGroup();
            }
        }

        private void DrawStretchButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            DrawStretchButton("Stretch Width", UIExtension.UI.AbsolutePosition.StretchMode.Width);
            DrawStretchButton("Stretch Height", UIExtension.UI.AbsolutePosition.StretchMode.Height);
            DrawStretchButton("Stretch All", UIExtension.UI.AbsolutePosition.StretchMode.All);
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStretchButton(string label, UIExtension.UI.AbsolutePosition.StretchMode mode)
        {
            UIExtension.UI.AbsolutePosition.StretchMode currentMode = (UIExtension.UI.AbsolutePosition.StretchMode)_stretchModeProp.enumValueIndex;
            bool isSelected = currentMode == mode;
            
            // 원래 배경색 저장
            Color originalColor = GUI.backgroundColor;
            
            if (isSelected)
            {
                GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            }
            
            if (GUILayout.Button(label, GUILayout.Height(25)))
            {
                if (isSelected)
                {
                    _stretchModeProp.enumValueIndex = (int)UIExtension.UI.AbsolutePosition.StretchMode.None;
                }
                else
                {
                    _stretchModeProp.enumValueIndex = (int)mode;
                }
                serializedObject.ApplyModifiedProperties();
            }
            
            // 배경색 복구
            GUI.backgroundColor = originalColor;
        }

        private void DrawAnchorPresetGrid()
        {
            int currentPreset = _anchorPresetProp.enumValueIndex;
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 10;
            buttonStyle.padding = new RectOffset(2, 2, 2, 2);

            EditorGUILayout.BeginVertical();
            
            for (int row = 0; row < 3; row++)
            {
                EditorGUILayout.BeginHorizontal();
                
                for (int col = 0; col < 3; col++)
                {
                    int index = row * 3 + col;
                    
                    // 원래 배경색 저장
                    Color originalColor = GUI.backgroundColor;
                    
                    if (currentPreset == index)
                    {
                        // 선택된 버튼 강조 (밝은 초록색)
                        GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
                        buttonStyle.fontStyle = FontStyle.Bold;
                    }
                    else
                    {
                        buttonStyle.fontStyle = FontStyle.Normal;
                    }

                    string buttonLabel = GetButtonLabel(index);
                    
                    if (GUILayout.Button(buttonLabel, buttonStyle, GUILayout.Height(30), GUILayout.ExpandWidth(true)))
                    {
                        _anchorPresetProp.enumValueIndex = index;
                        serializedObject.ApplyModifiedProperties();
                    }
                    
                    // 배경색 복구
                    GUI.backgroundColor = originalColor;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }

        private string GetButtonLabel(int index)
        {
            // 시각적으로 앵커 위치를 표시
            switch (index)
            {
                case 0: return "┌─"; // Top Left
                case 1: return "─┬─"; // Top Center
                case 2: return "─┐"; // Top Right
                case 3: return "├─"; // Middle Left
                case 4: return "─┼─"; // Middle Center
                case 5: return "─┤"; // Middle Right
                case 6: return "└─"; // Bottom Left
                case 7: return "─┴─"; // Bottom Center
                case 8: return "─┘"; // Bottom Right
                default: return "?";
            }
        }

        private string GetPresetDisplayName(UIExtension.UI.AbsolutePosition.AnchorPreset preset)
        {
            return AnchorPresetNames[(int)preset];
        }
    }
}
