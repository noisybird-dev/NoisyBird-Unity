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
            string fullDirectory = Path.Combine(CONFIG_PATH_ROOT, CONFIG_PATH_SUB);
            string assetPath = Path.Combine(fullDirectory, CONFIG_NAME);
            
            // Normalize path to forward slashes for Unity
            assetPath = assetPath.Replace("\\", "/");
            fullDirectory = fullDirectory.Replace("\\", "/");

            var config = AssetDatabase.LoadAssetAtPath<AutoAddressableConfig>(assetPath);

            if (config == null)
            {
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
    }
}
