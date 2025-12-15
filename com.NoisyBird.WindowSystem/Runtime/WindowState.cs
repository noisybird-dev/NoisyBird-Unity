using System;

namespace NoisyBird.WindowSystem
{
    /// <summary>
    /// Window의 상태를 저장하기 위한 추상 클래스입니다.
    /// 각 Window는 이 클래스를 상속받아 자신만의 상태 클래스를 정의해야 합니다.
    /// </summary>
    [Serializable]
    public abstract class WindowState
    {
        /// <summary>
        /// 상태를 복제합니다. 깊은 복사가 필요한 경우 직접 구현해야 합니다.
        /// </summary>
        /// <returns>복제된 WindowState 객체</returns>
        public virtual WindowState Clone()
        {
            return (WindowState)MemberwiseClone();
        }
    }
}
