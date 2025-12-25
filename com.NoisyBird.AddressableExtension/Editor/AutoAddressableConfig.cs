using System;
using System.Collections.Generic;
using UnityEngine;

namespace NoisyBird.AddressableExtension.Editor
{
    [CreateAssetMenu(fileName = "AutoAddressableConfig", menuName = "NoisyBird/Addressable Extension/Auto Addressable Config")]
    public class AutoAddressableConfig : ScriptableObject
    {
        private static AutoAddressableConfig _instance;
        public static AutoAddressableConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<AutoAddressableConfig>(
                        "Assets/Resources/NoisyBird/AddressableExtension/AutoAddressableConfig.asset");
                }
                return _instance;
            }
        }

        [Serializable]
        public class AddressableRule
        {
            [Tooltip("Relative path to the folder (e.g., Assets/GameData/Items)")]
            public string FolderPath;
            
            [Tooltip("Name of the Addressable Group")]
            public string GroupName;
            
            [Tooltip("Optional labels to apply")]
            public List<string> Labels;
            
            [Tooltip("If true, the address will be simplified to the file name without extension")]
            public bool SimplifyAddress = true;
        }

        public List<AddressableRule> Rules = new List<AddressableRule>();
    }
}
