using UnityEngine;

namespace EditorTools
{
    public static class DebugExtension
    {
        public static void Log(this object origin, object message) => Log(origin, message, null);
        public static void Log(this object origin, object message, Object context)
        {
            Debug.Log($"[{origin.GetType().Name}] {message}", context);
        }

        public static void LogWarning(this object origin, object message) => LogWarning(origin, message, null);
        public static void LogWarning(this object origin, object message, Object context)
        {
            Debug.LogWarning($"[{origin.GetType().Name}] {message}", context);
        }

        public static void LogError(this object origin, object message) => LogError(origin, message, null);
        public static void LogError(this object origin, object message, Object context)
        {
            Debug.LogError($"[{origin.GetType().Name}] {message}", context);
        }
    }
}