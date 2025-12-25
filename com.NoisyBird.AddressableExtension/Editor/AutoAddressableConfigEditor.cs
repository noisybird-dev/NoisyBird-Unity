using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.Collections.Generic;
using System.Linq;

namespace NoisyBird.AddressableExtension.Editor
{
    [CustomEditor(typeof(AutoAddressableConfig))]
    public class AutoAddressableConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty _rulesProp;
        private AddressableAssetSettings _settings;

        private void OnEnable()
        {
            _rulesProp = serializedObject.FindProperty("Rules");
            _settings = AddressableAssetSettingsDefaultObject.Settings;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (_settings == null)
            {
                _settings = AddressableAssetSettingsDefaultObject.Settings;
                if (_settings == null)
                {
                    EditorGUILayout.HelpBox("Addressable Asset Settings not found. Please create one.", MessageType.Warning);
                    if (GUILayout.Button("Open Addressables Groups"))
                    {
                        EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
                    }
                    return;
                }
            }

            EditorGUILayout.LabelField("Auto Addressable Rules", EditorStyles.boldLabel);
            
            for (int i = 0; i < _rulesProp.arraySize; i++)
            {
                var ruleProp = _rulesProp.GetArrayElementAtIndex(i);
                var folderPathProp = ruleProp.FindPropertyRelative("FolderPath");
                var groupNameProp = ruleProp.FindPropertyRelative("GroupName");
                var labelsProp = ruleProp.FindPropertyRelative("Labels");
                var simplifyAddressProp = ruleProp.FindPropertyRelative("SimplifyAddress");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Rule {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    _rulesProp.DeleteArrayElementAtIndex(i);
                    // Deleting modifies array, so exit loop to avoid index issues (OnGUI will redraw)
                    break;
                }
                EditorGUILayout.EndHorizontal();

                // Folder Path (Standard Property)
                EditorGUILayout.PropertyField(folderPathProp);

                // Group Name (Dropdown)
                DrawGroupDropdown(groupNameProp);

                // Labels (List with Dropdown for adding)
                DrawLabels(labelsProp);

                // Simplify Address
                EditorGUILayout.PropertyField(simplifyAddressProp);

                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }

            if (GUILayout.Button("Add New Rule"))
            {
                _rulesProp.InsertArrayElementAtIndex(_rulesProp.arraySize);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawGroupDropdown(SerializedProperty groupNameProp)
        {
            var groups = _settings.groups.Select(g => g.Name).ToList();
            int currentIndex = groups.IndexOf(groupNameProp.stringValue);
            if (currentIndex == -1) currentIndex = 0; // Default to first if not found (or "Default Local Group" usually)

            int newIndex = EditorGUILayout.Popup("Group Name", currentIndex, groups.ToArray());
            if (newIndex >= 0 && newIndex < groups.Count)
            {
                groupNameProp.stringValue = groups[newIndex];
            }
            else if (groups.Count > 0)
            {
                 // Ensure valid value if empty
                 groupNameProp.stringValue = groups[0];
            }
        }

        private void DrawLabels(SerializedProperty labelsProp)
        {
            EditorGUILayout.LabelField("Labels");
            EditorGUI.indentLevel++;
            
            for (int j = 0; j < labelsProp.arraySize; j++)
            {
                 EditorGUILayout.BeginHorizontal();
                 EditorGUILayout.PropertyField(labelsProp.GetArrayElementAtIndex(j), GUIContent.none);
                 if (GUILayout.Button("-", GUILayout.Width(20)))
                 {
                     labelsProp.DeleteArrayElementAtIndex(j);
                     break;
                 }
                 EditorGUILayout.EndHorizontal();
            }

            // Dropdown to add label
            var allLabels = _settings.GetLabels();
            // Filter labels already used? Maybe not necessary, but helpful.
            
            int selected = EditorGUILayout.Popup("Add Label", -1, allLabels.ToArray());
            if (selected >= 0)
            {
                string labelToAdd = allLabels[selected];
                // Check duplicate ?
                bool hasLabel = false;
                for(int k=0; k<labelsProp.arraySize; k++) {
                    if (labelsProp.GetArrayElementAtIndex(k).stringValue == labelToAdd) {
                        hasLabel = true; 
                        break;
                    }
                }
                
                if (!hasLabel)
                {
                    labelsProp.InsertArrayElementAtIndex(labelsProp.arraySize);
                    labelsProp.GetArrayElementAtIndex(labelsProp.arraySize - 1).stringValue = labelToAdd;
                }
            }
            EditorGUI.indentLevel--;
        }
    }
}
