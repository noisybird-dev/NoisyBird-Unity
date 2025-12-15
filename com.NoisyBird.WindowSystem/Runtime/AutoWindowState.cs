using System;
using System.Collections.Generic;

namespace NoisyBird.WindowSystem
{
    /// <summary>
    /// 자동 상태 저장을 위한 범용 WindowState 구현입니다.
    /// Dictionary를 사용하여 다양한 타입의 값을 저장할 수 있습니다.
    /// </summary>
    [Serializable]
    public class AutoWindowState : WindowState
    {
        /// <summary>
        /// 상태 데이터를 저장하는 Dictionary (Key -> Value)
        /// </summary>
        public Dictionary<string, object> StateData = new Dictionary<string, object>();

        /// <summary>
        /// 값을 설정합니다.
        /// </summary>
        public void SetValue<T>(string key, T value)
        {
            StateData[key] = value;
        }

        /// <summary>
        /// 값을 가져옵니다.
        /// </summary>
        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (StateData.TryGetValue(key, out object value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// 키가 존재하는지 확인합니다.
        /// </summary>
        public bool HasKey(string key)
        {
            return StateData.ContainsKey(key);
        }

        public override WindowState Clone()
        {
            AutoWindowState cloned = new AutoWindowState();
            foreach (var kvp in StateData)
            {
                cloned.StateData[kvp.Key] = kvp.Value;
            }
            return cloned;
        }
    }
}
