using System;
using UnityEngine;
using UnityEngine.UI;

namespace NoisyBird.WindowSystem.Examples
{
    /// <summary>
    /// 간단한 확인/취소 팝업 예제입니다.
    /// </summary>
    public class ConfirmPopup : WindowBase
    {
        [Header("UI References")]
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        private Action _onConfirm;
        private Action _onCancel;

        private void Awake()
        {
            if (_confirmButton != null)
                _confirmButton.onClick.AddListener(OnConfirmClicked);

            if (_cancelButton != null)
                _cancelButton.onClick.AddListener(OnCancelClicked);
        }

        public override void OnOpen(object payload = null)
        {
            base.OnOpen(payload);

            if (payload is ConfirmPopupData data)
            {
                if (_titleText != null)
                    _titleText.text = data.Title;

                if (_messageText != null)
                    _messageText.text = data.Message;

                _onConfirm = data.OnConfirm;
                _onCancel = data.OnCancel;
            }
        }

        public override void OnClose()
        {
            base.OnClose();
            _onConfirm = null;
            _onCancel = null;
        }

        private void OnConfirmClicked()
        {
            _onConfirm?.Invoke();
            WindowManager.Instance.CloseWindow(WindowId);
        }

        private void OnCancelClicked()
        {
            _onCancel?.Invoke();
            WindowManager.Instance.CloseWindow(WindowId);
        }

        public override WindowState CaptureState()
        {
            // 팝업은 상태를 저장하지 않음
            return null;
        }

        public override void RestoreState(WindowState state)
        {
            // 팝업은 상태를 복구하지 않음
        }

        private void Start()
        {
            WindowManager.Instance.RegisterWindow(this);
        }

        private void OnDestroy()
        {
            WindowManager.Instance.UnregisterWindow(WindowId);
        }
    }

    /// <summary>
    /// ConfirmPopup에 전달할 데이터 구조체입니다.
    /// </summary>
    public class ConfirmPopupData
    {
        public string Title;
        public string Message;
        public Action OnConfirm;
        public Action OnCancel;

        public ConfirmPopupData(string title, string message, Action onConfirm = null, Action onCancel = null)
        {
            Title = title;
            Message = message;
            OnConfirm = onConfirm;
            OnCancel = onCancel;
        }
    }
}
