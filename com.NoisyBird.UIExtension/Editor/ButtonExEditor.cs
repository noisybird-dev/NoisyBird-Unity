using NoisyBird.UIExtension.UI;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine.UI;

namespace NoisyBird.UIExtension.Editor.UI
{
    [CustomEditor(typeof(ButtonEx), true)]
    [CanEditMultipleObjects]
    
    public class ButtonExEditor : SelectableEditor
    {
        SerializedProperty m_OnClickProperty;
        SerializedProperty m_ClickTransitionProperty;
        SerializedProperty m_PunchScaleProperty;
        SerializedProperty m_PunchDurationProperty;
        SerializedProperty m_TransitionProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_OnClickProperty = serializedObject.FindProperty("m_OnClick");
            m_ClickTransitionProperty = serializedObject.FindProperty("m_ClickTransition");
            m_PunchScaleProperty = serializedObject.FindProperty("m_PunchScale");
            m_PunchDurationProperty = serializedObject.FindProperty("m_PunchDuration");
            m_TransitionProperty = serializedObject.FindProperty("m_Transition");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space(20f);

            serializedObject.Update();
            
            EditorGUILayout.PropertyField(m_ClickTransitionProperty);

            if (m_ClickTransitionProperty.enumValueIndex == (int)ButtonEx.ClickTransition.ScalePunch)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_PunchScaleProperty);
                EditorGUILayout.PropertyField(m_PunchDurationProperty);
                EditorGUI.indentLevel--;

                if (m_TransitionProperty.enumValueIndex == (int)Selectable.Transition.Animation)
                {
                    EditorGUILayout.HelpBox("Scale Punch transition may conflict with Animation transition if the animation modifies the scale.", MessageType.Warning);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_OnClickProperty);
            serializedObject.ApplyModifiedProperties();
        }
    }
}