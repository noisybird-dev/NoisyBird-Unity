namespace NoisyBird.WindowSystem
{
    /// <summary>
    /// Window의 타입을 정의합니다.
    /// </summary>
    public enum WindowType
    {
        /// <summary>
        /// 배경 UI (모든 것 아래에 표시됨)
        /// </summary>
        Underlay,

        /// <summary>
        /// 전체 화면을 덮는 화면 (예: 인벤토리, 영웅 관리)
        /// </summary>
        Screen,

        /// <summary>
        /// Screen 위에 뜨는 팝업
        /// </summary>
        Popup,

        /// <summary>
        /// 항상 떠 있는 오버레이 (글로벌 HUD, 상단바 등)
        /// </summary>
        Overlay,

        /// <summary>
        /// 잠깐 나왔다 사라지는 알림
        /// </summary>
        Toast
    }
}
