using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace NoisyBird.AddressableExtension.Editor
{
    public class AutoAddressablePostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            // Combine imported and moved assets (using new paths for moved)
            var assetsToCheck = new HashSet<string>(importedAssets);
            foreach (var moved in movedAssets)
            {
                assetsToCheck.Add(moved);
            }

            if (assetsToCheck.Count == 0) return;

            if (!AddressableAssetSettingsDefaultObject.SettingsExists) return;
            
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            var config = FindConfig();
            if (config == null) return;

            bool modified = false;

            foreach (var assetPath in assetsToCheck)
            {
                if (!IsValidAsset(assetPath)) continue;

                var rule = config.Rules.FirstOrDefault(r => assetPath.StartsWith(r.FolderPath + "/"));
                if (rule != null)
                {
                    var group = settings.FindGroup(rule.GroupName);
                    if (group == null)
                    {
                        group = settings.CreateGroup(rule.GroupName, false, false, true, settings.DefaultGroup.Schemas);
                        modified = true;
                    }

                    var guid = AssetDatabase.AssetPathToGUID(assetPath);
                    var entry = settings.CreateOrMoveEntry(guid, group);
                    if (entry != null)
                    {
                        // Simplify Address
                        if (rule.SimplifyAddress)
                        {
                            entry.SetAddress(Path.GetFileNameWithoutExtension(assetPath));
                        }
                        
                        // Set Labels
                        if (rule.Labels != null && rule.Labels.Count > 0)
                        {
                            foreach (var label in rule.Labels)
                            {
                                settings.AddLabel(label); // Ensure label exists in settings
                                entry.SetLabel(label, true, true);
                            }
                        }
                        
                        modified = true;
                    }
                }
            }
            
            if (modified)
            {
                // Is SetDirty needed? AddressableAssetSettings calls SetDirty internally often.
                // But good to be safe if we rely on it.
            }
        }

        private static AutoAddressableConfig FindConfig()
        {
            var guids = AssetDatabase.FindAssets("t:AutoAddressableConfig");
            if (guids.Length == 0) return null;
            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AutoAddressableConfig>(path);
        }

        private static bool IsValidAsset(string path)
        {
            // Ignore folders, meta files
            if (AssetDatabase.IsValidFolder(path)) return false;
            if (path.EndsWith(".cs") || path.EndsWith(".js") || path.EndsWith(".shader")) return false; // Usually we don't address script files directly unless TextAsset
             // Actually, shaders usually ARE addressable if needed. Scripts not.
             // But "IsValidFolder" handles directory.
             
             return true;
        }
    }
}
