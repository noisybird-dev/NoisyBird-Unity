using UnityEngine;

namespace NoisyBird.WindowSystem.Examples
{
    /// <summary>
    /// Window System 사용 예제입니다.
    /// 이 스크립트를 씬에 추가하고 Inspector에서 Window들을 할당한 후 테스트할 수 있습니다.
    /// </summary>
    public class WindowSystemExample : MonoBehaviour
    {
        [Header("Window References")]
        [SerializeField] private InventoryWindow _inventoryWindow;
        [SerializeField] private SettingsWindow _settingsWindow;
        [SerializeField] private ConfirmPopup _confirmPopup;

        private void Update()
        {
            // 키보드 단축키로 Window 테스트
            if (Input.GetKeyDown(KeyCode.I))
            {
                ToggleInventory();
            }

            if (Input.GetKeyDown(KeyCode.O))
            {
                ToggleSettings();
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                ShowConfirmPopup();
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                SaveAllStates();
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                LoadAllStates();
            }
        }

        /// <summary>
        /// 인벤토리 Window 토글
        /// </summary>
        public void ToggleInventory()
        {
            if (_inventoryWindow == null) return;

            if (WindowManager.Instance.IsWindowOpen(_inventoryWindow.WindowId))
            {
                WindowManager.Instance.CloseWindow(_inventoryWindow.WindowId, saveState: true);
                Debug.Log("[Example] Inventory closed and state saved");
            }
            else
            {
                WindowManager.Instance.OpenWindow(_inventoryWindow.WindowId, restoreState: true);
                Debug.Log("[Example] Inventory opened and state restored");
            }
        }

        /// <summary>
        /// 설정 Window 토글
        /// </summary>
        public void ToggleSettings()
        {
            if (_settingsWindow == null) return;

            if (WindowManager.Instance.IsWindowOpen(_settingsWindow.WindowId))
            {
                WindowManager.Instance.CloseWindow(_settingsWindow.WindowId, saveState: true);
                Debug.Log("[Example] Settings closed and state saved");
            }
            else
            {
                WindowManager.Instance.OpenWindow(_settingsWindow.WindowId, restoreState: true);
                Debug.Log("[Example] Settings opened and state restored");
            }
        }

        /// <summary>
        /// 확인 팝업 표시
        /// </summary>
        public void ShowConfirmPopup()
        {
            if (_confirmPopup == null) return;

            ConfirmPopupData data = new ConfirmPopupData(
                title: "확인",
                message: "정말로 실행하시겠습니까?",
                onConfirm: () => Debug.Log("[Example] Confirmed!"),
                onCancel: () => Debug.Log("[Example] Cancelled!")
            );

            WindowManager.Instance.OpenWindow(_confirmPopup.WindowId, payload: data);
        }

        /// <summary>
        /// 모든 열려있는 Window의 상태 저장
        /// </summary>
        public void SaveAllStates()
        {
            WindowManager.Instance.SaveAllWindowStates();
            Debug.Log("[Example] All window states saved");
        }

        /// <summary>
        /// 저장된 상태로 모든 Window 복구
        /// </summary>
        public void LoadAllStates()
        {
            // 인벤토리 복구
            if (_inventoryWindow != null && WindowManager.Instance.GetSavedState(_inventoryWindow.WindowId) != null)
            {
                WindowManager.Instance.OpenWindow(_inventoryWindow.WindowId, restoreState: true);
            }

            // 설정 복구
            if (_settingsWindow != null && WindowManager.Instance.GetSavedState(_settingsWindow.WindowId) != null)
            {
                WindowManager.Instance.OpenWindow(_settingsWindow.WindowId, restoreState: true);
            }

            Debug.Log("[Example] All window states restored");
        }

        /// <summary>
        /// 모든 Window 닫기
        /// </summary>
        public void CloseAllWindows()
        {
            WindowManager.Instance.CloseAllWindows(saveStates: true);
            Debug.Log("[Example] All windows closed");
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 400));
            GUILayout.Label("=== Window System Example ===");
            GUILayout.Space(10);

            GUILayout.Label("Keyboard Shortcuts:");
            GUILayout.Label("I - Toggle Inventory");
            GUILayout.Label("O - Toggle Settings");
            GUILayout.Label("P - Show Confirm Popup");
            GUILayout.Label("S - Save All States");
            GUILayout.Label("L - Load All States");
            GUILayout.Space(10);

            if (GUILayout.Button("Toggle Inventory (I)"))
                ToggleInventory();

            if (GUILayout.Button("Toggle Settings (O)"))
                ToggleSettings();

            if (GUILayout.Button("Show Confirm Popup (P)"))
                ShowConfirmPopup();

            GUILayout.Space(10);

            if (GUILayout.Button("Save All States (S)"))
                SaveAllStates();

            if (GUILayout.Button("Load All States (L)"))
                LoadAllStates();

            if (GUILayout.Button("Close All Windows"))
                CloseAllWindows();

            GUILayout.EndArea();
        }
    }
}
