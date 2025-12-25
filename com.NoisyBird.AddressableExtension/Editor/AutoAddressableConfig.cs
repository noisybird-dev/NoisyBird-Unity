using System;
using System.Collections.Generic;
using UnityEngine;

namespace NoisyBird.AddressableExtension.Editor
{
    [CreateAssetMenu(fileName = "AutoAddressableConfig", menuName = "NoisyBird/Addressable Extension/Auto Addressable Config")]
    public class AutoAddressableConfig : ScriptableObject
    {
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
