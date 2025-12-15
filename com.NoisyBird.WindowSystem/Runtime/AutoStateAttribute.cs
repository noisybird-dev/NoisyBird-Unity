using System;

namespace NoisyBird.WindowSystem
{
    /// <summary>
    /// 이 Attribute가 붙은 필드는 자동으로 상태 저장/복구 대상이 됩니다.
    /// AutoStateWindow와 함께 사용하여 Reflection 기반 자동 상태 관리를 수행합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class AutoStateAttribute : Attribute
    {
        /// <summary>
        /// 상태 저장 시 사용할 키 (선택사항, 비어있으면 필드 이름 사용)
        /// </summary>
        public string Key { get; set; }

        public AutoStateAttribute()
        {
        }

        public AutoStateAttribute(string key)
        {
            Key = key;
        }
    }
}
