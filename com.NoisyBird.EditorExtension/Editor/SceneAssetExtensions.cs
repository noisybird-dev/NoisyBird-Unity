using UnityEditor;

namespace NoisyBird.EditorExtension.Editor
{
    public static class SceneAssetExtensions
    {
        public static string GetPath(this SceneAsset sceneAsset)
        {
            return AssetDatabase.GetAssetPath(sceneAsset);
        }
    }
}
