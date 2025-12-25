using System.IO;
using UnityEditor;
using UnityEngine;

namespace NoisyBird.AddressableExtension.Editor
{
    public static class AddressableConfigMenu
    {
        private const string CONFIG_PATH_ROOT = "Assets/Resources";
        private const string CONFIG_PATH_SUB = "NoisyBird/AddressableExtension";
        private const string CONFIG_NAME = "AutoAddressableConfig.asset";

        [MenuItem("Noisy Bird/Addressable/Config", priority = 100)]
        public static void OpenConfig()
        {
            var config = AutoAddressableConfig.Instance;
            
            if (config == null)
            {
                // Create if not exists
                string fullDirectory = Path.Combine(CONFIG_PATH_ROOT, CONFIG_PATH_SUB);
                string assetPath = Path.Combine(fullDirectory, CONFIG_NAME);
                
                // Normalize path to forward slashes for Unity
                assetPath = assetPath.Replace("\\", "/");
                fullDirectory = fullDirectory.Replace("\\", "/");

                if (!Directory.Exists(fullDirectory))
                {
                    Directory.CreateDirectory(fullDirectory);
                    AssetDatabase.Refresh();
                }

                config = ScriptableObject.CreateInstance<AutoAddressableConfig>();
                AssetDatabase.CreateAsset(config, assetPath);
                AssetDatabase.SaveAssets();
                
                Debug.Log($"[AddressableConfigMenu] Created new config at {assetPath}");
            }

            EditorGUIUtility.PingObject(config);
            Selection.activeObject = config;
        }

        [MenuItem("Assets/Add Folder to Addressable Config", false, 20)]
        public static void AddFolderToConfig()
        {
            var obj = Selection.activeObject;
            string path = AssetDatabase.GetAssetPath(obj);

            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
            {
                Debug.LogWarning("Selected object is not a valid folder.");
                return;
            }

            // Ensure config exists
            OpenConfig(); 
            var config = AutoAddressableConfig.Instance;
            
            if (config != null)
            {
                // Check dupes
                bool exists = config.Rules.Exists(r => r.FolderPath == path);
                if (!exists)
                {
                    config.Rules.Add(new AutoAddressableConfig.AddressableRule
                    {
                        FolderPath = path,
                        GroupName = "Default Local Group"
                    });
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[AutoAddressableConfig] Added rule for: {path}");
                    
                    // Highlight the config to show it worked
                    EditorGUIUtility.PingObject(config); 
                }
                else
                {
                    Debug.LogWarning($"[AutoAddressableConfig] Rule already exists for: {path}");
                }
            }
        }

        [MenuItem("Assets/Add Folder to Addressable Config", true)]
        public static bool ValidateAddFolderToConfig()
        {
            // Check activeObject first (works for main area)
            var obj = Selection.activeObject;
            if (obj != null)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (AssetDatabase.IsValidFolder(path)) return true;
            }

            // Fallback: Check if any selected GUID corresponds to a folder
            // (When Right-clicking in left sidebar, Selection.activeObject might sometimes lag or behave differently depending on focus)
            // But usually Selection.assetGUIDs works.
            if (Selection.assetGUIDs.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                return AssetDatabase.IsValidFolder(path);
            }
            
            return false;
        }
    }
}
