using UnityEngine;
using UnityEngine.UI;

namespace NoisyBird.WindowSystem.Examples
{
    /// <summary>
    /// AutoState를 사용하는 설정 Window 예제입니다.
    /// [AutoState] Attribute를 사용하여 자동으로 상태를 저장/복구합니다.
    /// </summary>
    public class SettingsWindow : AutoStateWindow
    {
        [Header("UI References")]
        [AutoState] [SerializeField] private ScrollRect _scrollRect;
        [AutoState] [SerializeField] private Toggle _soundToggle;
        [AutoState] [SerializeField] private Toggle _musicToggle;
        [AutoState] [SerializeField] private Slider _volumeSlider;
        [AutoState] [SerializeField] private Dropdown _qualityDropdown;
        [AutoState] [SerializeField] private InputField _playerNameInput;

        public override void OnOpen(object payload = null)
        {
            base.OnOpen(payload);
            Debug.Log("[SettingsWindow] Opened");
        }

        public override void OnClose()
        {
            base.OnClose();
            Debug.Log("[SettingsWindow] Closed");
        }

        private void Start()
        {
            // Window를 WindowManager에 등록
            WindowManager.Instance.RegisterWindow(this);
        }

        private void OnDestroy()
        {
            // Window를 WindowManager에서 등록 해제
            WindowManager.Instance.UnregisterWindow(WindowId);
        }

        // UI 이벤트 핸들러 예제
        public void OnSoundToggleChanged(bool value)
        {
            Debug.Log($"[SettingsWindow] Sound: {value}");
        }

        public void OnMusicToggleChanged(bool value)
        {
            Debug.Log($"[SettingsWindow] Music: {value}");
        }

        public void OnVolumeChanged(float value)
        {
            Debug.Log($"[SettingsWindow] Volume: {value}");
        }

        public void OnQualityChanged(int value)
        {
            Debug.Log($"[SettingsWindow] Quality: {value}");
        }

        public void OnPlayerNameChanged(string value)
        {
            Debug.Log($"[SettingsWindow] Player Name: {value}");
        }
    }
}
