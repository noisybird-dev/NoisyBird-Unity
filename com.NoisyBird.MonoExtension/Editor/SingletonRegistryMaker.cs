#if UNITY_EDITOR
using NoisyBird.MonoExtension;
using UnityEditor;
using UnityEngine;


namespace NoisyBird.MonoExtension.Editor
{
    [InitializeOnLoad]
    public static class SingletonRegistryMaker
    {
        static SingletonRegistryMaker()
        {
            bool exist = SingletonRegistry.Instance == null;
            if (exist == false)
            {
                Debug.Log("[SingletonRegistry] Not Exist SingletonRegistry");
            }
        }

        [MenuItem("Noisy Bird/MonoExtension/Singleton Registry Settings")]
        public static void MakeSingletonRegistry()
        {
            Selection.activeObject = SingletonRegistry.Instance;
            EditorGUIUtility.PingObject(SingletonRegistry.Instance);
        }
    }
    #endif
}
