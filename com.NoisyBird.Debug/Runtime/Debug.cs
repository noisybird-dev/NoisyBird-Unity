using System.Diagnostics;
using UnityEngine;

namespace NoisyBird.Debug
{
    public static class Debug
    {
        [Conditional("USE_DEBUG")]
        public static void Log(DebugType type, object message, Object context = null)
        {
            if (context == null)
            {
                UnityEngine.Debug.Log($"[{type}] {message}");
            }
            else
            {
                UnityEngine.Debug.Log($"[{type}] {message}", context);
            }
        }

        [Conditional("USE_DEBUG")]
        public static void Log(object message, Object context = null)
        {
            if (context == null)
            {
                UnityEngine.Debug.Log($"[Temp] {message}");
            }
            else
            {
                UnityEngine.Debug.Log($"[Temp] {message}", context);
            }
        }

        [Conditional("USE_DEBUG")]
        public static void LogWarning(DebugType type, object message, Object context = null)
        {
            if (context == null)
            {
                UnityEngine.Debug.LogWarning($"[{type}] {message}");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[{type}] {message}", context);
            }
        }

        [Conditional("USE_DEBUG")]
        public static void LogWarning(object message, Object context = null)
        {
            if (context == null)
            {
                UnityEngine.Debug.LogWarning($"[Temp] {message}");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[Temp] {message}", context);
            }
        }

        [Conditional("USE_DEBUG")]
        public static void LogError(DebugType type, object message, Object context = null)
        {
            if (context == null)
            {
                UnityEngine.Debug.LogError($"[{type}] {message}");
            }
            else
            {
                UnityEngine.Debug.LogError($"[{type}] {message}", context);
            }
        }

        [Conditional("USE_DEBUG")]
        public static void LogError(object message, Object context = null)
        {
            if (context == null)
            {
                UnityEngine.Debug.LogError($"[Temp] {message}");
            }
            else
            {
                UnityEngine.Debug.LogError($"[Temp] {message}", context);
            }
        }
    }
}
