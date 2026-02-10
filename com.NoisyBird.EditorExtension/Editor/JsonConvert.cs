using UnityEngine;

namespace NoisyBird.EditorExtension.Editor
{
    public static class JsonConvert
    {
        public static string SerializeObject(object obj)
        {
            return JsonUtility.ToJson(obj, true);
        }

        public static T DeserializeObject<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }
    }
}
