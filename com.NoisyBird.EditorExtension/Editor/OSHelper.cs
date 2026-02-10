#if UNITY_EDITOR
using System.Diagnostics;
using UnityEngine;

namespace NoisyBird.EditorExtension.Editor
{
    public static class OSHelper
    {
        public static void StartProcess(string path)
        {
            var process = new Process();
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            process.StartInfo = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            };
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "open",
                Arguments = $"\"{path}\"",
                UseShellExecute = false
            };
#else
            Debug.LogWarning("이 플랫폼에서는 폴더 열기가 지원되지 않습니다.");
#endif
            process.Start();
        }

        public static void OpenDirectory(string path)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            System.Diagnostics.Process.Start("explorer.exe", $"\"{path}\"");
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            System.Diagnostics.Process.Start("open", $"\"{path}\"");
#else
            Debug.LogWarning("이 플랫폼에서는 폴더 열기가 지원되지 않습니다.");
#endif
        }
    }
}
#endif