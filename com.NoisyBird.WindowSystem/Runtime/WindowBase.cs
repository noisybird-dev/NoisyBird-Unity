using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NoisyBird.WindowSystem
{
    /// <summary>
    /// 모든 Window의 기본 클래스입니다.
    /// 이 클래스를 상속받아 각 Window를 구현해야 합니다.
    /// </summary>
    public abstract class WindowBase : UIBehaviour
    {
        [Header("Window Meta")]
        [Tooltip("Window를 식별하기 위한 고유 ID")]
        [SerializeField] private string _windowId;

        [Tooltip("Window의 타입")]
        [SerializeField] private WindowType _windowType = WindowType.Screen;

        /// <summary>
        /// Window를 식별하기 위한 고유 ID
        /// </summary>
        public string WindowId
        {
            get => _windowId;
            set => _windowId = value;
        }

        /// <summary>
        /// Window의 타입
        /// </summary>
        public WindowType WindowType
        {
            get => _windowType;
            set => _windowType = value;
        }

        /// <summary>
        /// Window가 현재 열려있는지 여부
        /// </summary>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Window가 파괴될 때 호출되는 이벤트 (Addressable 해제 등에 활용)
        /// </summary>
        public event Action<WindowBase> OnWindowDestroy;

        /// <summary>
        /// 현재 Window의 상태를 캡처합니다.
        /// </summary>
        /// <returns>캡처된 WindowState 객체</returns>
        public abstract WindowState CaptureState();

        /// <summary>
        /// 저장된 상태를 이 Window에 복구합니다.
        /// </summary>
        /// <param name="state">복구할 WindowState 객체</param>
        public abstract void RestoreState(WindowState state);

        /// <summary>
        /// Window가 열릴 때 호출됩니다.
        /// </summary>
        /// <param name="payload">Window에 전달할 데이터 (선택사항)</param>
        public virtual void OnOpen(object payload = null)
        {
            IsOpen = true;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Window를 임시로 닫습니다. (SetActive false, 재사용 가능)
        /// </summary>
        public virtual void OnClose()
        {
            IsOpen = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Window를 파괴합니다. OnWindowDestroy 이벤트 호출 후 GameObject를 파괴합니다.
        /// </summary>
        public virtual void DestroyWindow()
        {
            IsOpen = false;
            OnWindowDestroy?.Invoke(this);
            Destroy(gameObject);
        }

        /// <summary>
        /// Window를 숨깁니다. (상태는 유지)
        /// </summary>
        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 숨겨진 Window를 다시 표시합니다.
        /// </summary>
        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 열기 애니메이션을 재생합니다. override하여 구현합니다.
        /// 기본값: 즉시 완료 (애니메이션 없음).
        /// </summary>
        /// <returns>애니메이션 완료를 나타내는 Task</returns>
        protected virtual Task PlayOpenAnimation() => Task.CompletedTask;

        /// <summary>
        /// 닫기 애니메이션을 재생합니다. override하여 구현합니다.
        /// 기본값: 즉시 완료 (애니메이션 없음).
        /// </summary>
        /// <returns>애니메이션 완료를 나타내는 Task</returns>
        protected virtual Task PlayCloseAnimation() => Task.CompletedTask;

        /// <summary>
        /// 열기 애니메이션을 외부에서 호출할 수 있도록 하는 내부 메서드입니다.
        /// </summary>
        internal Task InternalPlayOpenAnimation() => PlayOpenAnimation();

        /// <summary>
        /// 닫기 애니메이션을 외부에서 호출할 수 있도록 하는 내부 메서드입니다.
        /// </summary>
        internal Task InternalPlayCloseAnimation() => PlayCloseAnimation();

        protected override void OnValidate()
        {
            // WindowId가 비어있으면 GameObject 이름으로 자동 설정
            if (string.IsNullOrEmpty(_windowId))
            {
                _windowId = gameObject.name;
            }
        }
    }
}
