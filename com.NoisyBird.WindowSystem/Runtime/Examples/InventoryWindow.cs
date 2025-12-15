using System;
using UnityEngine;

namespace NoisyBird.WindowSystem.Examples
{
    /// <summary>
    /// 인벤토리 Window의 상태를 저장하는 클래스입니다.
    /// </summary>
    [Serializable]
    public class InventoryWindowState : WindowState
    {
        public int SelectedTab;
        public float ScrollNormalizedPos;
        public string SelectedItemId;

        public override WindowState Clone()
        {
            return new InventoryWindowState
            {
                SelectedTab = this.SelectedTab,
                ScrollNormalizedPos = this.ScrollNormalizedPos,
                SelectedItemId = this.SelectedItemId
            };
        }
    }

    /// <summary>
    /// 인벤토리 Window 예제입니다.
    /// 수동으로 상태를 저장/복구하는 방식을 보여줍니다.
    /// </summary>
    public class InventoryWindow : WindowBase
    {
        [Header("Inventory Settings")]
        [SerializeField] private int _selectedTab = 0;
        [SerializeField] private float _scrollNormalizedPos = 1f;
        [SerializeField] private string _selectedItemId = "";

        public int SelectedTab
        {
            get => _selectedTab;
            set => _selectedTab = value;
        }

        public float ScrollNormalizedPos
        {
            get => _scrollNormalizedPos;
            set => _scrollNormalizedPos = value;
        }

        public string SelectedItemId
        {
            get => _selectedItemId;
            set => _selectedItemId = value;
        }

        public override WindowState CaptureState()
        {
            return new InventoryWindowState
            {
                SelectedTab = _selectedTab,
                ScrollNormalizedPos = _scrollNormalizedPos,
                SelectedItemId = _selectedItemId
            };
        }

        public override void RestoreState(WindowState state)
        {
            if (state is InventoryWindowState inventoryState)
            {
                _selectedTab = inventoryState.SelectedTab;
                _scrollNormalizedPos = inventoryState.ScrollNormalizedPos;
                _selectedItemId = inventoryState.SelectedItemId;

                // UI 업데이트 로직
                UpdateUI();
            }
        }

        public override void OnOpen(object payload = null)
        {
            base.OnOpen(payload);
            UpdateUI();
        }

        private void UpdateUI()
        {
            // 실제 UI 업데이트 로직
            Debug.Log($"[InventoryWindow] Tab: {_selectedTab}, Scroll: {_scrollNormalizedPos}, Item: {_selectedItemId}");
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
    }
}
