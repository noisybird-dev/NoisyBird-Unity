using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NoisyBird.UIExtension.SafeArea
{
    public class SafeAreaManager
    {
        private static SafeAreaManager _instance;
        public static SafeAreaManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new SafeAreaManager();
                }
                return _instance;
            }
        }

        private Rect _safeArea;
        public Rect SafeArea => _safeArea;

#if UNITY_EDITOR
        // Editor-only: Allow setting a simulated safe area for testing
        private bool _useSimulatedSafeArea = false;
        private Rect _simulatedSafeArea;
        
        public void SetSimulatedSafeArea(Rect simulatedArea)
        {
            _useSimulatedSafeArea = true;
            _simulatedSafeArea = simulatedArea;
            Refresh();
        }
        
        public void ClearSimulatedSafeArea()
        {
            _useSimulatedSafeArea = false;
            Refresh();
        }
#endif

        public delegate void SafeAreaChanged(Rect safeArea);
        public event SafeAreaChanged OnSafeAreaChanged;

        private SafeAreaManager()
        {
            Refresh();
        }

        public void CheckUpdate()
        {
            Rect currentSafeArea = GetCurrentSafeArea();
            if (_safeArea != currentSafeArea)
            {
                Refresh();
            }
        }

        private Rect GetCurrentSafeArea()
        {
#if UNITY_EDITOR
            if (_useSimulatedSafeArea)
            {
                return _simulatedSafeArea;
            }
            else
            {
                return Screen.safeArea;
            }
#else
            return Screen.safeArea;
#endif
        }

        public void Refresh()
        {
            _safeArea = GetCurrentSafeArea();
            OnSafeAreaChanged?.Invoke(_safeArea);
        }
    }
}
