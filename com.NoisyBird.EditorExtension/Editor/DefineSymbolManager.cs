using System.Linq;
using UnityEditor;

namespace NoisyBird.EditorExtension.Editor
{
    public static class DefineSymbolManager
    {
        public static bool IsSymbolAlreadyDefined(string symbol)
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            return defines.Split(';').Contains(symbol);
        }

        public static void AddDefineSymbol(string symbol)
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            var symbolList = defines.Split(';').ToList();
            
            if (!symbolList.Contains(symbol))
            {
                symbolList.Add(symbol);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", symbolList));
            }
        }

        public static void RemoveDefineSymbol(string symbol)
        {
            var buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            var symbolList = defines.Split(';').ToList();
            
            if (symbolList.Contains(symbol))
            {
                symbolList.Remove(symbol);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", symbolList));
            }
        }
    }
}
