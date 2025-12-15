using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    /// Window의 열기/닫기, 상태 저장/복구, 씬 전환 처리를 담당합니다.
    /// </summary>
    public class WindowManager : MonoBehaviour
    {
        private static WindowManager _instance;

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

        // Screen/Popup Window 스택 (스택 관리됨)
        private Stack<WindowBase> _windowStack = new Stack<WindowBase>();

        // Overlay/Toast Window 리스트 (스택 관리 안됨)
        private List<WindowBase> _nonStackWindows = new List<WindowBase>();

        // Window 상태 저장소 (WindowId -> WindowState)
        private Dictionary<string, WindowState> _savedStates = new Dictionary<string, WindowState>();

        // Window 로더 델리게이트 (프로젝트별 커스터마이징 가능)
        private WindowLoaderDelegate _windowLoader = null;

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

            // 씬 전환 이벤트 등록
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
            }
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
        }

        /// <summary>
        /// Window를 시스템에서 등록 해제합니다.
        /// </summary>
        /// <param name="windowId">등록 해제할 Window ID</param>
        public void UnregisterWindow(string windowId)
        {
            if (_registeredWindows.ContainsKey(windowId))
            {
                _registeredWindows.Remove(windowId);
            }
        }

        /// <summary>
        /// Window를 엽니다.
        /// </summary>
        /// <param name="windowId">열 Window ID</param>
        /// <param name="payload">Window에 전달할 데이터 (선택사항)</param>
        /// <param name="restoreState">저장된 상태를 복구할지 여부</param>
        /// <returns>성공 여부</returns>
        public bool OpenWindow(string windowId, object payload = null, bool restoreState = false)
        {
            if (!_registeredWindows.TryGetValue(windowId, out WindowBase window))
            {
                // 등록되지 않은 Window인 경우, 로더를 통해 로드 시도
                if (_windowLoader != null)
                {
                    window = _windowLoader(windowId);
                    
                    if (window != null)
                    {
                        // 로드 성공 시 자동 등록
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

            // Window 열기
            window.OnOpen(payload);

            // WindowType에 따라 스택 또는 비스택 리스트에 추가
            if (window.WindowType == WindowType.Screen || window.WindowType == WindowType.Popup)
            {
                // Screen/Popup은 스택에 Push
                _windowStack.Push(window);
            }
            else // Underlay, Overlay, Toast
            {
                // Underlay/Overlay/Toast는 비스택 리스트에 추가
                if (!_nonStackWindows.Contains(window))
                {
                    _nonStackWindows.Add(window);
                }
            }

            // Hierarchy 순서 업데이트
            UpdateWindowHierarchyOrder(window);

            return true;
        }

        /// <summary>
        /// Window를 닫습니다.
        /// </summary>
        /// <param name="windowId">닫을 Window ID</param>
        /// <param name="saveState">상태를 저장할지 여부</param>
        /// <returns>성공 여부</returns>
        public bool CloseWindow(string windowId, bool saveState = false)
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

            // Window 닫기
            window.OnClose();

            // WindowType에 따라 스택 또는 비스택 리스트에서 제거
            if (window.WindowType == WindowType.Screen || window.WindowType == WindowType.Popup)
            {
                // Stack에서 제거 (Stack은 직접 Remove가 없으므로 재구성)
                var tempStack = new Stack<WindowBase>();
                bool found = false;
                
                while (_windowStack.Count > 0)
                {
                    var w = _windowStack.Pop();
                    if (w == window)
                    {
                        found = true;
                        break;
                    }
                    tempStack.Push(w);
                }
                
                // 나머지 다시 Push
                while (tempStack.Count > 0)
                {
                    _windowStack.Push(tempStack.Pop());
                }
            }
            else // Overlay, Toast
            {
                _nonStackWindows.Remove(window);
            }

            return true;
        }

        /// <summary>
        /// Window의 Hierarchy 순서를 WindowType에 따라 업데이트합니다.
        /// 순서: Underlay < (Screen/Popup 스택) < Overlay < Toast
        /// Screen/Popup은 구분 없이 나중에 열린 것이 위에 표시됩니다.
        /// </summary>
        /// <param name="window">순서를 업데이트할 Window</param>
        private void UpdateWindowHierarchyOrder(WindowBase window)
        {
            if (window == null || window.transform.parent == null)
                return;

            Transform parent = window.transform.parent;
            int targetIndex = GetHierarchyIndexForWindow(window, parent);
            window.transform.SetSiblingIndex(targetIndex);
        }

        /// <summary>
        /// Window의 Hierarchy 인덱스를 계산합니다.
        /// </summary>
        private int GetHierarchyIndexForWindow(WindowBase window, Transform parent)
        {
            int targetIndex = 0;
            bool isStackWindow = window.WindowType == WindowType.Screen || window.WindowType == WindowType.Popup;

            // 부모의 모든 자식을 순회하며 적절한 위치 찾기
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                WindowBase childWindow = child.GetComponent<WindowBase>();

                if (childWindow == null || childWindow == window)
                    continue;

                bool childIsStackWindow = childWindow.WindowType == WindowType.Screen || childWindow.WindowType == WindowType.Popup;
                int childPriority = GetWindowTypePriority(childWindow.WindowType);
                int windowPriority = GetWindowTypePriority(window.WindowType);

                // 우선순위가 낮은 타입이면 다음 인덱스로
                if (childPriority < windowPriority)
                {
                    targetIndex = i + 1;
                }
                // Screen/Popup 영역 내에서는 나중에 열린 것이 위로
                else if (isStackWindow && childIsStackWindow)
                {
                    targetIndex = i + 1;
                }
            }

            return targetIndex;
        }

        /// <summary>
        /// WindowType의 우선순위를 반환합니다. (낮을수록 아래에 표시)
        /// Screen과 Popup은 같은 우선순위를 가집니다.
        /// </summary>
        private int GetWindowTypePriority(WindowType windowType)
        {
            switch (windowType)
            {
                case WindowType.Underlay: return 0;
                case WindowType.Screen: return 1;
                case WindowType.Popup: return 1;  // Screen과 같은 우선순위
                case WindowType.Overlay: return 2;
                case WindowType.Toast: return 3;
                default: return 0;
            }
        }

        /// <summary>
        /// 스택의 최상위 Window를 닫습니다. (Screen/Popup만 해당)
        /// </summary>
        /// <param name="saveState">상태를 저장할지 여부</param>
        /// <returns>성공 여부</returns>
        public bool CloseTopWindow(bool saveState = false)
        {
            if (_windowStack.Count == 0)
            {
                Debug.LogWarning("[WindowManager] No window in stack to close.");
                return false;
            }

            WindowBase topWindow = _windowStack.Peek();
            return CloseWindow(topWindow.WindowId, saveState);
        }

        /// <summary>
        /// 모든 열려있는 Window를 닫습니다.
        /// </summary>
        /// <param name="saveStates">상태를 저장할지 여부</param>
        public void CloseAllWindows(bool saveStates = false)
        {
            // 스택 Window 닫기 (Pop으로 역순)
            while (_windowStack.Count > 0)
            {
                WindowBase window = _windowStack.Peek();
                CloseWindow(window.WindowId, saveStates);
            }

            // 비스택 Window 닫기
            for (int i = _nonStackWindows.Count - 1; i >= 0; i--)
            {
                WindowBase window = _nonStackWindows[i];
                CloseWindow(window.WindowId, saveStates);
            }
        }

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
            // 스택 Window 상태 저장
            foreach (WindowBase window in _windowStack)
            {
                SaveWindowState(window.WindowId);
            }

            // 비스택 Window 상태 저장
            foreach (WindowBase window in _nonStackWindows)
            {
                SaveWindowState(window.WindowId);
            }
        }

        /// <summary>
        /// 저장된 Window 상태를 가져옵니다.
        /// </summary>
        /// <param name="windowId">Window ID</param>
        /// <returns>저장된 WindowState, 없으면 null</returns>
        public WindowState GetSavedState(string windowId)
        {
            return _savedStates.TryGetValue(windowId, out WindowState state) ? state : null;
        }

        /// <summary>
        /// 저장된 Window 상태를 제거합니다.
        /// </summary>
        /// <param name="windowId">Window ID</param>
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

        /// <summary>
        /// Window가 등록되어 있는지 확인합니다.
        /// </summary>
        /// <param name="windowId">Window ID</param>
        /// <returns>등록 여부</returns>
        public bool IsWindowRegistered(string windowId)
        {
            return _registeredWindows.ContainsKey(windowId);
        }

        /// <summary>
        /// Window가 열려있는지 확인합니다.
        /// </summary>
        /// <param name="windowId">Window ID</param>
        /// <returns>열림 여부</returns>
        public bool IsWindowOpen(string windowId)
        {
            return _registeredWindows.TryGetValue(windowId, out WindowBase window) && window.IsOpen;
        }

        /// <summary>
        /// 등록된 Window를 가져옵니다.
        /// </summary>
        /// <param name="windowId">Window ID</param>
        /// <returns>WindowBase, 없으면 null</returns>
        public WindowBase GetWindow(string windowId)
        {
            return _registeredWindows.TryGetValue(windowId, out WindowBase window) ? window : null;
        }

        /// <summary>
        /// 씬이 로드될 때 호출됩니다.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 씬 전환 후 처리는 여기서 수행
            // 필요시 특정 Window를 자동으로 열거나 상태를 복구할 수 있음
        }

        /// <summary>
        /// 씬이 언로드될 때 호출됩니다.
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            // 씬 전환 전 처리
            // Stack과 NonStack 모두 처리
            List<WindowBase> allWindows = new List<WindowBase>();
            allWindows.AddRange(_windowStack);
            allWindows.AddRange(_nonStackWindows);

            foreach (WindowBase window in allWindows)
            {
                if (window == null)
                    continue;

                switch (window.SceneRule)
                {
                    case WindowSceneRule.DestroyOnSceneChange:
                        // 상태 저장 후 파괴
                        SaveWindowState(window.WindowId);
                        CloseWindow(window.WindowId);
                        UnregisterWindow(window.WindowId);
                        break;

                    case WindowSceneRule.HideOnSceneChange:
                        // 상태 저장 후 숨김
                        SaveWindowState(window.WindowId);
                        window.Hide();
                        
                        // 스택 또는 비스택 리스트에서 제거
                        if (window.WindowType == WindowType.Screen || window.WindowType == WindowType.Popup)
                        {
                            // Stack에서 제거는 CloseWindow에서 처리되므로 여기서는 직접 제거
                            var tempStack = new Stack<WindowBase>();
                            while (_windowStack.Count > 0)
                            {
                                var w = _windowStack.Pop();
                                if (w != window)
                                    tempStack.Push(w);
                            }
                            while (tempStack.Count > 0)
                                _windowStack.Push(tempStack.Pop());
                        }
                        else
                        {
                            _nonStackWindows.Remove(window);
                        }
                        break;

                    case WindowSceneRule.KeepOnSceneChange:
                        // 아무것도 하지 않음 (유지)
                        break;
                }
            }
        }
    }
}
