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
        private bool _needsApply = false;
        private Coroutine _delayedApplyCoroutine;
#if UNITY_EDITOR
        private EditorCoroutine _delayedApplyEditorCoroutine;
#endif
        private Vector2 _lastCanvasSize;

        protected override void Awake()
        {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SafeAreaManager.Instance.OnSafeAreaChanged += OnSafeAreaChanged;
            UnityEngine.Canvas.willRenderCanvases += OnWillRenderCanvases;
            
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

            OnWillRenderCanvases();
        }

        private void OnWillRenderCanvases()
        {
            // Check if Canvas size changed (resolution/rotation)
            UnityEngine.Canvas canvas = GetComponentInParent<UnityEngine.Canvas>();
            if (canvas != null)
            {
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    Vector2 currentCanvasSize = canvasRect.rect.size;
                    if (_lastCanvasSize != currentCanvasSize)
                    {
                        //Debug.Log($"[SafeArea] Canvas size changed: {_lastCanvasSize} -> {currentCanvasSize}");
                        _lastCanvasSize = currentCanvasSize;
                        SafeAreaManager.Instance.CheckUpdate();
                        _needsApply = true;
                    }
                }
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

            UnityEngine.Canvas canvas = GetComponentInParent<UnityEngine.Canvas>();
            if (canvas == null) return;
            
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null) return;

            // Get Canvas size
            Rect canvasRectData = canvasRect.rect;
            float canvasWidth = canvasRectData.width;
            float canvasHeight = canvasRectData.height;
            
            // Calculate SafeArea size in Canvas pixels
            // SafeArea is a ratio of Screen, apply that ratio to Canvas
            float safeAreaWidth = (safeArea.width / Screen.width) * canvasWidth;
            float safeAreaHeight = (safeArea.height / Screen.height) * canvasHeight;
            
            float safeAreaX = (safeArea.x / Screen.width) * canvasWidth;
            float safeAreaY = (safeArea.y / Screen.height) * canvasHeight;
            
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
    }
}
