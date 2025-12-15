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

        [Tooltip("씬 전환 시 Window의 처리 규칙")]
        [SerializeField] private WindowSceneRule _sceneRule = WindowSceneRule.DestroyOnSceneChange;

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
        /// 씬 전환 시 Window의 처리 규칙
        /// </summary>
        public WindowSceneRule SceneRule
        {
            get => _sceneRule;
            set => _sceneRule = value;
        }

        /// <summary>
        /// Window가 현재 열려있는지 여부
        /// </summary>
        public bool IsOpen { get; private set; }

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
        /// Window가 닫힐 때 호출됩니다.
        /// </summary>
        public virtual void OnClose()
        {
            IsOpen = false;
            gameObject.SetActive(false);
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
