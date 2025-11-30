using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

namespace NoisyBird.UIExtension.SafeArea
{
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class SafeArea : UIBehaviour
    {
        private RectTransform _rectTransform;
        private RectTransform _rootCanvasRect;
        private UnityEngine.Canvas _rootCanvas;
        
        private bool _needsApply = false;
        private Coroutine _delayedApplyCoroutine;
#if UNITY_EDITOR
        private EditorCoroutine _delayedApplyEditorCoroutine;
#endif
        private Vector2 _lastCanvasSize;

        protected override void Awake()
        {
            base.Awake();
            Init();
        }

        private void Init()
        {
            _rectTransform = GetComponent<RectTransform>();
            _rootCanvas = GetComponentInParent<UnityEngine.Canvas>();
            if (_rootCanvas != null)
            {
                _rootCanvas = _rootCanvas.rootCanvas;
                _rootCanvasRect = _rootCanvas.GetComponent<RectTransform>();
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UnityEngine.Canvas.willRenderCanvases += OnWillRenderCanvases;
            
            Init();
            
            SafeAreaManager.Instance.OnSafeAreaChanged += OnSafeAreaChanged;
            
            SafeAreaManager.Instance.Refresh();
            ApplySafeArea(SafeAreaManager.Instance.SafeArea);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnityEngine.Canvas.willRenderCanvases -= OnWillRenderCanvases;
            
            StopCoroutine();
            
            if (SafeAreaManager.Instance != null)
            {
                SafeAreaManager.Instance.OnSafeAreaChanged -= OnSafeAreaChanged;
            }
        }

        private void StartCoroutine()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                _delayedApplyEditorCoroutine = EditorCoroutineUtility.StartCoroutine(DelayedApplySafeArea(), gameObject);
            }
            else
            {
                _delayedApplyCoroutine = StartCoroutine(DelayedApplySafeArea());
            }
#else
            _delayedApplyCoroutine = StartCoroutine(DelayedApplySafeArea());
#endif
        }

        private void StopCoroutine()
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                if (_delayedApplyEditorCoroutine != null)
                {
                    EditorCoroutineUtility.StopCoroutine(_delayedApplyEditorCoroutine);
                    _delayedApplyEditorCoroutine = null;
                }
            }
            else
            {
                if (_delayedApplyCoroutine != null)
                {
                    StopCoroutine(_delayedApplyCoroutine);
                    _delayedApplyCoroutine = null;
                }
            }
#else
            if (_delayedApplyCoroutine != null)
            {
                StopCoroutine(_delayedApplyCoroutine);
                _delayedApplyCoroutine = null;
            }
#endif
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                // In edit mode, update directly without using willRenderCanvases
                // to avoid mouse hover issues
                OnWillRenderCanvases();
            }
            else
            {
                OnWillRenderCanvases();
            }
#else
            OnWillRenderCanvases();
#endif
        }

        private void OnWillRenderCanvases()
        {
            // Check if Canvas size changed (resolution/rotation)
            if (_rootCanvasRect != null)
            {
                Vector2 currentCanvasSize = _rootCanvasRect.rect.size;
                if (_lastCanvasSize != currentCanvasSize)
                {
                    //Debug.Log($"[SafeArea] Canvas size changed: {_lastCanvasSize} -> {currentCanvasSize}");
                    _lastCanvasSize = currentCanvasSize;
                    SafeAreaManager.Instance.CheckUpdate();
                    _needsApply = true;
                }
            }
            else
            {
                // Try to init if null (e.g. parent changed)
                Init();
            }
            
            if (_needsApply)
            {
                //Debug.Log($"[SafeArea] Triggering delayed apply, Canvas size: {_lastCanvasSize}");
                _needsApply = false;
                StopCoroutine();
                StartCoroutine();
            }
        }

        private IEnumerator DelayedApplySafeArea()
        {
            yield return new WaitForEndOfFrame();
            SafeAreaManager.Instance.Refresh();
            ApplySafeArea(SafeAreaManager.Instance.SafeArea);
            _delayedApplyCoroutine = null;
            _delayedApplyEditorCoroutine = null;
        }

        private void OnSafeAreaChanged(Rect safeArea)
        {
            _needsApply = true;
        }

        private void ApplySafeArea(Rect safeArea)
        {
            if (_rectTransform == null) return;
            if (_rootCanvasRect == null) return;

            // Get Canvas size
            Rect canvasRectData = _rootCanvasRect.rect;
            float canvasWidth = canvasRectData.width;
            float canvasHeight = canvasRectData.height;
            
            // Calculate SafeArea size in Canvas pixels
            // SafeArea is a ratio of Screen, apply that ratio to Canvas
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

#if UNITY_EDITOR
            // In Edit Mode, we need to handle both Game View and Device Simulator.
            // 1. Game View: Screen.width/height are correct (Game View resolution). Screen.currentResolution is Monitor resolution (Wrong).
            // 2. Device Simulator: Screen.width/height are Window size (Wrong). Screen.currentResolution is Simulated Resolution (Correct).
            //
            // Heuristic: If SafeArea fits inside Screen.width/height, assume Game View.
            // If SafeArea is larger than Screen.width/height, assume Device Simulator and use currentResolution.
            if (!Application.isPlaying)
            {
                // Check if SafeArea exceeds Screen bounds
                if (safeArea.x + safeArea.width > screenWidth || safeArea.y + safeArea.height > screenHeight)
                {
                    screenWidth = Screen.currentResolution.width;
                    screenHeight = Screen.currentResolution.height;
                }
            }
#endif
            
            float safeAreaWidth = (safeArea.width / screenWidth) * canvasWidth;
            float safeAreaHeight = (safeArea.height / screenHeight) * canvasHeight;
            
            float safeAreaX = (safeArea.x / screenWidth) * canvasWidth;
            float safeAreaY = (safeArea.y / screenHeight) * canvasHeight;
            
            // Set anchors to center
            _rectTransform.anchorMin = Vector2.one * 0.5f;
            _rectTransform.anchorMax = Vector2.one * 0.5f;
            _rectTransform.pivot = Vector2.one * 0.5f;
            
            // Set size
            _rectTransform.sizeDelta = new Vector2(safeAreaWidth, safeAreaHeight);
            
            // Set position (center of safe area relative to canvas center)
            float centerX = safeAreaX + safeAreaWidth * 0.5f - canvasWidth * 0.5f;
            float centerY = safeAreaY + safeAreaHeight * 0.5f - canvasHeight * 0.5f;
            _rectTransform.anchoredPosition = new Vector2(centerX, centerY);
            
            //Debug.Log($"[SafeArea] Applied - Canvas: {canvasWidth}x{canvasHeight}, Screen: {Screen.width}x{Screen.height}, SafeArea: {safeArea}, Calculated Size: {safeAreaWidth}x{safeAreaHeight}");
        }
        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            Init();
            _needsApply = true;
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            Init();
            _needsApply = true;
        }
    }
}
