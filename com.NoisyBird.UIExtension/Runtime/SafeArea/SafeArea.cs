using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NoisyBird.UIExtension.SafeArea
{
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class SafeArea : UIBehaviour
    {
        private RectTransform _rectTransform;
        private bool _needsApply = false;
        private int _lastUpdateFrame = 0;
        private Coroutine _delayedApplyCoroutine;
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
            
            // Initial check and apply
            SafeAreaManager.Instance.Refresh();
            ApplySafeArea(SafeAreaManager.Instance.SafeArea);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            UnityEngine.Canvas.willRenderCanvases -= OnWillRenderCanvases;
            
            // Stop any pending coroutine
            if (_delayedApplyCoroutine != null)
            {
                StopCoroutine(_delayedApplyCoroutine);
                _delayedApplyCoroutine = null;
            }
            
            if (SafeAreaManager.Instance != null)
            {
                SafeAreaManager.Instance.OnSafeAreaChanged -= OnSafeAreaChanged;
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            
            // When screen rotates or resolution changes, the Canvas size changes, 
            // triggering this on all children.
            SafeAreaManager.Instance.CheckUpdate();
            
            // Mark that we need to apply on next canvas render
            // This ensures CanvasScaler has finished updating
            _needsApply = true;
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
                        _lastCanvasSize = currentCanvasSize;
                        SafeAreaManager.Instance.CheckUpdate();
                        _needsApply = true;
                    }
                }
            }
            
            if (_needsApply && _lastUpdateFrame != Time.frameCount)
            {
                _needsApply = false;
                _lastUpdateFrame = Time.frameCount;
                Debug.Log(_lastUpdateFrame);
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    // In edit mode, use EditorApplication.delayCall
                    EditorApplication.delayCall += () => 
                    {
                        if (this != null && enabled)
                        {
                            ApplySafeArea(SafeAreaManager.Instance.SafeArea);
                        }
                    };
                }
                else
                {
                    // In play mode, use coroutine
                    if (_delayedApplyCoroutine != null)
                    {
                        StopCoroutine(_delayedApplyCoroutine);
                    }
                    _delayedApplyCoroutine = StartCoroutine(DelayedApplySafeArea());
                }
#else
                // In build, always use coroutine
                if (_delayedApplyCoroutine != null)
                {
                    StopCoroutine(_delayedApplyCoroutine);
                }
                _delayedApplyCoroutine = StartCoroutine(DelayedApplySafeArea());
#endif
            }
        }

        private IEnumerator DelayedApplySafeArea()
        {
            yield return null;
            ApplySafeArea(SafeAreaManager.Instance.SafeArea);
            _delayedApplyCoroutine = null;
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
        }
    }
}
