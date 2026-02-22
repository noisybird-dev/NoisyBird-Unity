using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace NoisyBird.WindowSystem
{
    /// <summary>
    /// Window를 로드하는 델리게이트입니다.
    /// </summary>
    /// <param name="windowId">로드할 Window ID</param>
    /// <returns>로드된 WindowBase 인스턴스, 실패 시 null</returns>
    public delegate WindowBase WindowLoaderDelegate(string windowId);

    /// <summary>
    /// Window 시스템을 관리하는 싱글톤 매니저입니다.
    /// Window의 열기/닫기, 상태 저장/복구를 담당합니다.
    /// </summary>
    public class WindowManager : MonoBehaviour
    {
        /// <summary>
        /// Screen과 그 위에 열린 Popup들을 하나의 그룹으로 관리합니다.
        /// </summary>
        private class ScreenGroup
        {
            public WindowBase Screen;
            public List<WindowBase> Popups = new List<WindowBase>();

            /// <summary>
            /// Screen과 모든 Popup을 숨깁니다.
            /// </summary>
            public void HideAll()
            {
                Screen.Hide();
                foreach (var popup in Popups)
                    popup.Hide();
            }

            /// <summary>
            /// Screen과 모든 Popup을 다시 표시합니다.
            /// </summary>
            public void ShowAll()
            {
                Screen.Show();
                foreach (var popup in Popups)
                    popup.Show();
            }

            /// <summary>
            /// Screen과 모든 Popup을 파괴합니다.
            /// </summary>
            /// <param name="unregisterAction">등록 해제 콜백</param>
            public void DestroyAll(Action<string> unregisterAction)
            {
                // Popup을 역순으로 파괴
                for (int i = Popups.Count - 1; i >= 0; i--)
                {
                    var popup = Popups[i];
                    if (popup != null)
                    {
                        unregisterAction?.Invoke(popup.WindowId);
                        popup.DestroyWindow();
                    }
                }
                Popups.Clear();

                // Screen 파괴
                if (Screen != null)
                {
                    unregisterAction?.Invoke(Screen.WindowId);
                    Screen.DestroyWindow();
                }
            }
        }

        private static WindowManager _instance;

        // UI 전용 카메라
        private Camera _uiCamera;

        /// <summary>
        /// UI 전용 카메라를 반환합니다.
        /// </summary>
        public Camera UICamera => _uiCamera;

        /// <summary>
        /// 애니메이션 시작/종료 시 호출되는 delegate입니다.
        /// true = 애니메이션 시작 (터치 차단 등), false = 애니메이션 종료 (터치 해제 등).
        /// 프로젝트별로 터치 차단 로직을 할당하여 사용합니다.
        /// </summary>
        public Action<bool> OnWindowAnim;

        /// <summary>
        /// WindowManager의 싱글톤 인스턴스
        /// </summary>
        public static WindowManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<WindowManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("WindowManager");
                        _instance = go.AddComponent<WindowManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // 등록된 모든 Window (WindowId -> WindowBase)
        private Dictionary<string, WindowBase> _registeredWindows = new Dictionary<string, WindowBase>();

        // Screen/Popup 그룹 스택 (Top = 활성 그룹)
        private Stack<ScreenGroup> _screenGroups = new Stack<ScreenGroup>();

        // Underlay/Overlay/Toast Window 리스트 (스택 관리 안됨)
        private List<WindowBase> _nonStackWindows = new List<WindowBase>();

        // Window 상태 저장소 (WindowId -> WindowState)
        private Dictionary<string, WindowState> _savedStates = new Dictionary<string, WindowState>();

        // Window 로더 델리게이트 (프로젝트별 커스터마이징 가능)
        private WindowLoaderDelegate _windowLoader = null;

        // Container 참조 저장 (WindowType -> Transform)
        private Dictionary<WindowType, Transform> _containersByType = new Dictionary<WindowType, Transform>();

        // Container GameObject 이름 매핑
        private static readonly Dictionary<WindowType, string> _containerNames = new Dictionary<WindowType, string>
        {
            { WindowType.Underlay, "UnderlayContainer" },
            { WindowType.Screen, "ScreenContainer" },
            { WindowType.Popup, "PopupContainer" },
            { WindowType.Overlay, "OverlayContainer" },
            { WindowType.Toast, "ToastContainer" }
        };

        /// <summary>
        /// Window 로더 델리게이트를 설정합니다.
        /// 등록되지 않은 Window를 열 때 자동으로 로드하는 함수를 지정할 수 있습니다.
        /// </summary>
        /// <param name="loader">Window 로더 함수</param>
        public void SetWindowLoader(WindowLoaderDelegate loader)
        {
            _windowLoader = loader;
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Container 생성
            CreateContainers();
        }

        /// <summary>
        /// UI 전용 카메라와 WindowType별 Container GameObject를 생성합니다.
        /// 각 Container는 개별 Canvas (ScreenSpace-Camera)를 보유하며,
        /// Sort Order로 렌더링 순서를 관리합니다.
        /// </summary>
        private void CreateContainers()
        {
            // UI 전용 카메라 생성
            CreateUICamera();

            // WindowType 순서 정의 (렌더링 순서와 동일)
            WindowType[] orderedTypes = new WindowType[]
            {
                WindowType.Underlay,
                WindowType.Screen,
                WindowType.Popup,
                WindowType.Toast,
                WindowType.Overlay
            };

            foreach (WindowType type in orderedTypes)
            {
                GameObject container = new GameObject(_containerNames[type]);

                // Container를 WindowManager의 자식으로 설정
                container.transform.SetParent(transform, false);
                container.layer = LayerMask.NameToLayer("UI");

                // Canvas 추가 (ScreenSpace-Camera 모드)
                Canvas canvas = container.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = _uiCamera;
                canvas.sortingLayerName = "UI";
                canvas.sortingOrder = ((int)type + 1) * 100;

                // CanvasScaler 추가 (Scale With Screen Size)
                var scaler = container.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                // GraphicRaycaster 추가
                container.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                // Container 참조 저장
                _containersByType[type] = container.transform;
            }

            Debug.Log("[WindowManager] UI Camera and containers created.");
        }

        /// <summary>
        /// UI만 촬영하는 전용 카메라를 생성합니다.
        /// </summary>
        private void CreateUICamera()
        {
            GameObject cameraGo = new GameObject("WindowSystemCamera");
            cameraGo.transform.SetParent(transform, false);

            _uiCamera = cameraGo.AddComponent<Camera>();
            _uiCamera.clearFlags = CameraClearFlags.Depth;
            _uiCamera.cullingMask = 1 << LayerMask.NameToLayer("UI");
            _uiCamera.depth = 100f;
            _uiCamera.orthographic = true;
        }

        /// <summary>
        /// WindowType에 해당하는 Container Transform을 반환합니다.
        /// </summary>
        private Transform GetContainerForType(WindowType windowType)
        {
            return _containersByType.TryGetValue(windowType, out Transform container) ? container : null;
        }

        /// <summary>
        /// Window를 적절한 Container의 자식으로 설정합니다.
        /// </summary>
        private void SetWindowParent(WindowBase window)
        {
            Transform targetContainer = GetContainerForType(window.WindowType);

            if (targetContainer == null)
            {
                Debug.LogError($"[WindowManager] Container not found for WindowType '{window.WindowType}'");
                return;
            }

            if (window.transform.parent == targetContainer)
            {
                return;
            }

            window.transform.SetParent(targetContainer, false);
        }

        /// <summary>
        /// Window를 시스템에 등록합니다.
        /// </summary>
        /// <param name="window">등록할 Window</param>
        public void RegisterWindow(WindowBase window)
        {
            if (window == null)
            {
                Debug.LogError("[WindowManager] Cannot register null window.");
                return;
            }

            if (string.IsNullOrEmpty(window.WindowId))
            {
                Debug.LogError($"[WindowManager] Window {window.name} has no WindowId.");
                return;
            }

            if (_registeredWindows.ContainsKey(window.WindowId))
            {
                Debug.LogWarning($"[WindowManager] Window with ID '{window.WindowId}' is already registered. Overwriting.");
            }

            _registeredWindows[window.WindowId] = window;
            SetWindowParent(window);
        }

        /// <summary>
        /// Window를 시스템에서 등록 해제합니다.
        /// </summary>
        /// <param name="windowId">등록 해제할 Window ID</param>
        public void UnregisterWindow(string windowId)
        {
            _registeredWindows.Remove(windowId);
        }

        #region Open Window

        /// <summary>
        /// Window를 열거나 로드합니다. 애니메이션이 있으면 재생합니다.
        /// </summary>
        /// <param name="windowId">열 Window ID</param>
        /// <param name="payload">Window에 전달할 데이터 (선택사항)</param>
        /// <param name="restoreState">저장된 상태를 복구할지 여부</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> OpenWindow(string windowId, object payload = null, bool restoreState = false)
        {
            if (!_registeredWindows.TryGetValue(windowId, out WindowBase window))
            {
                // 등록되지 않은 Window인 경우, 로더를 통해 로드 시도
                if (_windowLoader != null)
                {
                    window = _windowLoader(windowId);

                    if (window != null)
                    {
                        RegisterWindow(window);
                        Debug.Log($"[WindowManager] Window '{windowId}' loaded and registered automatically.");
                    }
                    else
                    {
                        Debug.LogError($"[WindowManager] Window '{windowId}' could not be loaded. WindowLoader returned null.");
                        return false;
                    }
                }
                else
                {
                    Debug.LogError($"[WindowManager] Window '{windowId}' is not registered and no WindowLoader is set. " +
                                   $"Please register the window or set a WindowLoader using SetWindowLoader().");
                    return false;
                }
            }

            if (window.IsOpen)
            {
                Debug.LogWarning($"[WindowManager] Window '{windowId}' is already open.");
                return false;
            }

            // 상태 복구
            if (restoreState && _savedStates.TryGetValue(windowId, out WindowState state))
            {
                window.RestoreState(state);
            }

            // WindowType에 따라 분기 처리
            switch (window.WindowType)
            {
                case WindowType.Screen:
                    await OpenScreen(window, payload);
                    break;

                case WindowType.Popup:
                    await OpenPopup(window, payload);
                    break;

                default: // Underlay, Toast, Overlay
                    await OpenNonStackWindow(window, payload);
                    break;
            }

            return true;
        }

        /// <summary>
        /// Screen Window를 엽니다.
        /// 새 Screen 열기 애니메이션과 이전 Screen 닫기 애니메이션이 동시에 재생됩니다.
        /// </summary>
        private async Task OpenScreen(WindowBase window, object payload)
        {
            OnWindowAnim?.Invoke(true);

            // 새 ScreenGroup 생성 및 Push
            var group = new ScreenGroup { Screen = window };
            _screenGroups.Push(group);

            // Window 열기 (SetActive true)
            window.OnOpen(payload);
            window.transform.SetAsLastSibling();

            // 이전 ScreenGroup이 있으면 동시 애니메이션
            if (_screenGroups.Count > 1)
            {
                // 스택에서 이전 그룹 가져오기 (Peek은 현재 새 그룹이므로 한 단계 아래)
                var previousGroup = GetScreenGroupAt(1);

                if (previousGroup != null)
                {
                    // 새 Screen 열기 애니메이션 + 이전 Screen 닫기 애니메이션 동시 재생
                    await Task.WhenAll(
                        window.InternalPlayOpenAnimation(),
                        previousGroup.Screen.InternalPlayCloseAnimation()
                    );

                    // 애니메이션 완료 후 이전 그룹 숨김
                    previousGroup.HideAll();
                }
            }
            else
            {
                // 이전 그룹 없으면 열기 애니메이션만
                await window.InternalPlayOpenAnimation();
            }

            OnWindowAnim?.Invoke(false);
        }

        /// <summary>
        /// Popup Window를 엽니다. 열기 애니메이션을 재생합니다.
        /// </summary>
        private async Task OpenPopup(WindowBase window, object payload)
        {
            if (_screenGroups.Count == 0)
            {
                Debug.LogError($"[WindowManager] Cannot open Popup '{window.WindowId}' without an active Screen.");
                return;
            }

            OnWindowAnim?.Invoke(true);

            // 활성 ScreenGroup에 Popup 추가
            _screenGroups.Peek().Popups.Add(window);

            // Window 열기
            window.OnOpen(payload);
            window.transform.SetAsLastSibling();

            // 열기 애니메이션
            await window.InternalPlayOpenAnimation();

            OnWindowAnim?.Invoke(false);
        }

        /// <summary>
        /// Underlay/Toast/Overlay Window를 엽니다. 열기 애니메이션을 재생합니다.
        /// </summary>
        private async Task OpenNonStackWindow(WindowBase window, object payload)
        {
            OnWindowAnim?.Invoke(true);

            if (!_nonStackWindows.Contains(window))
            {
                _nonStackWindows.Add(window);
            }

            window.OnOpen(payload);

            // 열기 애니메이션
            await window.InternalPlayOpenAnimation();

            OnWindowAnim?.Invoke(false);
        }

        #endregion

        #region Close Window

        /// <summary>
        /// Window를 닫습니다. 애니메이션이 있으면 재생 후 닫습니다.
        /// Underlay/Screen/Popup은 Destroy, Toast/Overlay는 SetActive(false).
        /// </summary>
        /// <param name="windowId">닫을 Window ID</param>
        /// <param name="saveState">상태를 저장할지 여부</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> CloseWindow(string windowId, bool saveState = false)
        {
            if (!_registeredWindows.TryGetValue(windowId, out WindowBase window))
            {
                Debug.LogError($"[WindowManager] Window '{windowId}' is not registered.");
                return false;
            }

            if (!window.IsOpen)
            {
                Debug.LogWarning($"[WindowManager] Window '{windowId}' is not open.");
                return false;
            }

            // 상태 저장
            if (saveState)
            {
                SaveWindowState(windowId);
            }

            // WindowType에 따라 분기 처리
            switch (window.WindowType)
            {
                case WindowType.Underlay:
                    await CloseUnderlay(window);
                    break;

                case WindowType.Screen:
                    await CloseScreen(window);
                    break;

                case WindowType.Popup:
                    await ClosePopup(window);
                    break;

                default: // Toast, Overlay
                    await CloseNonStackWindow(window);
                    break;
            }

            return true;
        }

        /// <summary>
        /// Underlay Window를 닫습니다. (닫기 애니메이션 → Destroy)
        /// </summary>
        private async Task CloseUnderlay(WindowBase window)
        {
            OnWindowAnim?.Invoke(true);

            await window.InternalPlayCloseAnimation();

            _nonStackWindows.Remove(window);
            UnregisterWindow(window.WindowId);
            window.DestroyWindow();

            OnWindowAnim?.Invoke(false);
        }

        /// <summary>
        /// Screen Window를 닫습니다.
        /// 이전 Screen 열기 애니메이션과 현재 Screen 닫기 애니메이션이 동시에 재생됩니다.
        /// </summary>
        private async Task CloseScreen(WindowBase window)
        {
            if (_screenGroups.Count == 0)
            {
                Debug.LogError($"[WindowManager] No ScreenGroup found for Screen '{window.WindowId}'.");
                return;
            }

            var topGroup = _screenGroups.Peek();

            if (topGroup.Screen != window)
            {
                Debug.LogError($"[WindowManager] Screen '{window.WindowId}' is not the active Screen. Only the top Screen can be closed.");
                return;
            }

            OnWindowAnim?.Invoke(true);

            // Pop
            _screenGroups.Pop();

            // 이전 ScreenGroup이 있으면 동시 애니메이션
            if (_screenGroups.Count > 0)
            {
                var previousGroup = _screenGroups.Peek();

                // 이전 그룹 표시 (애니메이션 전에 활성화)
                previousGroup.ShowAll();

                // 이전 Screen 열기 애니메이션 + 현재 Screen 닫기 애니메이션 동시 재생
                await Task.WhenAll(
                    previousGroup.Screen.InternalPlayOpenAnimation(),
                    window.InternalPlayCloseAnimation()
                );
            }
            else
            {
                // 이전 그룹 없으면 닫기 애니메이션만
                await window.InternalPlayCloseAnimation();
            }

            // 파괴
            UnregisterWindow(window.WindowId);
            window.DestroyWindow();

            OnWindowAnim?.Invoke(false);
        }

        /// <summary>
        /// Popup Window를 닫습니다. (닫기 애니메이션 → Destroy)
        /// </summary>
        private async Task ClosePopup(WindowBase window)
        {
            if (_screenGroups.Count == 0)
            {
                Debug.LogError($"[WindowManager] No ScreenGroup found for Popup '{window.WindowId}'.");
                return;
            }

            OnWindowAnim?.Invoke(true);

            // 닫기 애니메이션
            await window.InternalPlayCloseAnimation();

            var topGroup = _screenGroups.Peek();
            topGroup.Popups.Remove(window);
            UnregisterWindow(window.WindowId);
            window.DestroyWindow();

            OnWindowAnim?.Invoke(false);
        }

        /// <summary>
        /// Toast/Overlay Window를 닫습니다. (닫기 애니메이션 → SetActive false)
        /// </summary>
        private async Task CloseNonStackWindow(WindowBase window)
        {
            OnWindowAnim?.Invoke(true);

            // 닫기 애니메이션
            await window.InternalPlayCloseAnimation();

            _nonStackWindows.Remove(window);
            window.OnClose();

            OnWindowAnim?.Invoke(false);
        }

        /// <summary>
        /// 스택의 최상위 Window를 닫습니다.
        /// Popup이 있으면 마지막 Popup, 없으면 Screen을 닫습니다.
        /// </summary>
        /// <param name="saveState">상태를 저장할지 여부</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> CloseTopWindow(bool saveState = false)
        {
            if (_screenGroups.Count == 0)
            {
                Debug.LogWarning("[WindowManager] No ScreenGroup to close.");
                return false;
            }

            var topGroup = _screenGroups.Peek();

            // Popup이 있으면 마지막 Popup 닫기
            if (topGroup.Popups.Count > 0)
            {
                var lastPopup = topGroup.Popups[topGroup.Popups.Count - 1];
                return await CloseWindow(lastPopup.WindowId, saveState);
            }

            // Popup이 없으면 Screen 닫기
            return await CloseWindow(topGroup.Screen.WindowId, saveState);
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// 모든 ScreenGroup을 파괴합니다. (Toast/Overlay는 유지, 애니메이션 없음)
        /// </summary>
        public void CloseAllScreenGroups()
        {
            while (_screenGroups.Count > 0)
            {
                var group = _screenGroups.Pop();
                group.DestroyAll(UnregisterWindow);
            }
        }

        /// <summary>
        /// 모든 열려있는 Window를 닫습니다. (애니메이션 없음)
        /// ScreenGroup은 Destroy, Toast/Overlay는 임시 닫기(SetActive false).
        /// </summary>
        /// <param name="saveStates">상태를 저장할지 여부</param>
        public void CloseAllWindows(bool saveStates = false)
        {
            if (saveStates)
            {
                SaveAllWindowStates();
            }

            // ScreenGroup 전체 파괴
            CloseAllScreenGroups();

            // 비스택 Window 임시 닫기 (역순)
            for (int i = _nonStackWindows.Count - 1; i >= 0; i--)
            {
                var window = _nonStackWindows[i];
                if (window != null)
                {
                    window.OnClose();
                }
            }
            _nonStackWindows.Clear();
        }

        /// <summary>
        /// 모든 Window를 파괴합니다. (로그아웃 등 전체 리셋 시 사용, 애니메이션 없음)
        /// ScreenGroup + Toast/Overlay 모두 Destroy합니다.
        /// </summary>
        public void DestroyAllWindows()
        {
            // ScreenGroup 전체 파괴
            while (_screenGroups.Count > 0)
            {
                var group = _screenGroups.Pop();
                group.DestroyAll(UnregisterWindow);
            }

            // 비스택 Window 전체 파괴 (역순)
            for (int i = _nonStackWindows.Count - 1; i >= 0; i--)
            {
                var window = _nonStackWindows[i];
                if (window != null)
                {
                    UnregisterWindow(window.WindowId);
                    window.DestroyWindow();
                }
            }
            _nonStackWindows.Clear();

            // 상태 저장소 클리어
            _savedStates.Clear();
        }

        #endregion

        #region State Management

        /// <summary>
        /// Window의 현재 상태를 저장합니다.
        /// </summary>
        /// <param name="windowId">저장할 Window ID</param>
        public void SaveWindowState(string windowId)
        {
            if (!_registeredWindows.TryGetValue(windowId, out WindowBase window))
            {
                Debug.LogError($"[WindowManager] Window '{windowId}' is not registered.");
                return;
            }

            WindowState state = window.CaptureState();
            if (state != null)
            {
                _savedStates[windowId] = state;
            }
        }

        /// <summary>
        /// 모든 열려있는 Window의 상태를 저장합니다.
        /// </summary>
        public void SaveAllWindowStates()
        {
            foreach (var group in _screenGroups)
            {
                if (group.Screen != null)
                {
                    SaveWindowState(group.Screen.WindowId);
                }

                foreach (var popup in group.Popups)
                {
                    if (popup != null)
                    {
                        SaveWindowState(popup.WindowId);
                    }
                }
            }

            foreach (WindowBase window in _nonStackWindows)
            {
                SaveWindowState(window.WindowId);
            }
        }

        /// <summary>
        /// 저장된 Window 상태를 가져옵니다.
        /// </summary>
        public WindowState GetSavedState(string windowId)
        {
            return _savedStates.TryGetValue(windowId, out WindowState state) ? state : null;
        }

        /// <summary>
        /// 저장된 Window 상태를 제거합니다.
        /// </summary>
        public void ClearSavedState(string windowId)
        {
            _savedStates.Remove(windowId);
        }

        /// <summary>
        /// 모든 저장된 상태를 제거합니다.
        /// </summary>
        public void ClearAllSavedStates()
        {
            _savedStates.Clear();
        }

        #endregion

        #region Query

        /// <summary>
        /// Window가 등록되어 있는지 확인합니다.
        /// </summary>
        public bool IsWindowRegistered(string windowId)
        {
            return _registeredWindows.ContainsKey(windowId);
        }

        /// <summary>
        /// Window가 열려있는지 확인합니다.
        /// </summary>
        public bool IsWindowOpen(string windowId)
        {
            return _registeredWindows.TryGetValue(windowId, out WindowBase window) && window.IsOpen;
        }

        /// <summary>
        /// 등록된 Window를 가져옵니다.
        /// </summary>
        public WindowBase GetWindow(string windowId)
        {
            return _registeredWindows.TryGetValue(windowId, out WindowBase window) ? window : null;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Stack에서 특정 인덱스의 ScreenGroup을 가져옵니다. (0 = Top)
        /// </summary>
        private ScreenGroup GetScreenGroupAt(int index)
        {
            int current = 0;
            foreach (var group in _screenGroups)
            {
                if (current == index)
                    return group;
                current++;
            }
            return null;
        }

        #endregion
    }
}
