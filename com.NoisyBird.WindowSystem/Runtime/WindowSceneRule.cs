namespace NoisyBird.WindowSystem
{
    /// <summary>
    /// 씬 전환 시 Window의 처리 규칙을 정의합니다.
    /// </summary>
    public enum WindowSceneRule
    {
        /// <summary>
        /// 씬이 바뀌면 파괴됩니다.
        /// </summary>
        DestroyOnSceneChange,

        /// <summary>
        /// 씬이 바뀌면 숨김 처리됩니다. 필요 시 다시 Show 가능합니다.
        /// </summary>
        HideOnSceneChange,

        /// <summary>
        /// 씬이 바뀌어도 그대로 유지됩니다. (DontDestroyOnLoad 기준)
        /// </summary>
        KeepOnSceneChange
    }
}
