using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

namespace NoisyBird.UIExtension.UI
{
    [ExecuteAlways]
    public class AlwaysMaxScreen : UIBehaviour
    {
        private RectTransform self;
        private RectTransform rootCanvasRect;
        
        private bool _needsUpdate = false;
        private Coroutine _delayedUpdateCoroutine;
#if UNITY_EDITOR
        private EditorCoroutine _delayedUpdateEditorCoroutine;
#endif
        private Vector2 _lastCanvasSize;

        protected override void Awake()
        {
            base.Awake();
            Init();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UnityEngine.Canvas.willRenderCanvases += OnWillRenderCanvases;
            Init();
            UpdateRect();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnityEngine.Canvas.willRenderCanvases -= OnWillRenderCanvases;
            StopUpdateCoroutine();
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            Init();
            _needsUpdate = true;
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            Init();
            _needsUpdate = true;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            OnWillRenderCanvases();
        }

        private void OnWillRenderCanvases()
        {
            // Check if Canvas size changed
            if (rootCanvasRect != null)
            {
                Vector2 currentCanvasSize = rootCanvasRect.rect.size;
                if (_lastCanvasSize != currentCanvasSize)
                {
                    //Debug.Log($"[AlwaysMaxScreen] Canvas size changed: {_lastCanvasSize} -> {currentCanvasSize}");
                    _lastCanvasSize = currentCanvasSize;
                    _needsUpdate = true;
                }
            }
            
            if (_needsUpdate)
            {
                //Debug.Log($"[AlwaysMaxScreen] Triggering delayed update");
                _needsUpdate = false;
                StopUpdateCoroutine();
                StartUpdateCoroutine();
            }
        }

        private void StartUpdateCoroutine()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                _delayedUpdateEditorCoroutine = EditorCoroutineUtility.StartCoroutine(DelayedUpdate(), gameObject);
            }
            else
            {
                _delayedUpdateCoroutine = StartCoroutine(DelayedUpdate());
            }
#else
            _delayedUpdateCoroutine = StartCoroutine(DelayedUpdate());
#endif
        }

        private void StopUpdateCoroutine()
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                if (_delayedUpdateEditorCoroutine != null)
                {
                    EditorCoroutineUtility.StopCoroutine(_delayedUpdateEditorCoroutine);
                    _delayedUpdateEditorCoroutine = null;
                }
            }
            else
            {
                if (_delayedUpdateCoroutine != null)
                {
                    StopCoroutine(_delayedUpdateCoroutine);
                    _delayedUpdateCoroutine = null;
                }
            }
#else
            if (_delayedUpdateCoroutine != null)
            {
                StopCoroutine(_delayedUpdateCoroutine);
                _delayedUpdateCoroutine = null;
            }
#endif
        }

        private IEnumerator DelayedUpdate()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            
            // Re-check Canvas size after frame end (similar to SafeArea's Refresh)
            if (rootCanvasRect != null)
            {
                _lastCanvasSize = rootCanvasRect.rect.size;
                //Debug.Log($"[AlwaysMaxScreen] After WaitForEndOfFrame - Canvas size: {_lastCanvasSize}");
            }
            
            UpdateRect();
            _delayedUpdateCoroutine = null;
            _delayedUpdateEditorCoroutine = null;
        }

        private void Init()
        {
            self = transform as RectTransform;

            UnityEngine.Canvas canvas = GetComponentInParent<UnityEngine.Canvas>()?.rootCanvas;
            if (canvas != null)
                rootCanvasRect = canvas.GetComponent<RectTransform>();
        }

        private void UpdateRect()
        {
            if (self == null || rootCanvasRect == null || self.parent == null)
                return;

            RectTransform parentRect = self.parent as RectTransform;
            if (parentRect == null) return;

            // Get Canvas size directly from rect (not world corners which may not be updated yet)
            Rect canvasRectData = rootCanvasRect.rect;
            float canvasWidth = canvasRectData.width;
            float canvasHeight = canvasRectData.height;

            // 자식은 가운데 anchor + pivot 고정
            self.anchorMin = new Vector2(0.5f, 0.5f);
            self.anchorMax = new Vector2(0.5f, 0.5f);
            self.pivot = new Vector2(0.5f, 0.5f);

            // 크기 설정 - Canvas 전체 크기
            self.sizeDelta = new Vector2(canvasWidth, canvasHeight);

            // 위치 설정 - Canvas 중앙 (부모가 Canvas가 아닐 경우를 대비)
            // 부모의 중앙에 위치
            self.anchoredPosition = Vector2.zero;
            
            //Debug.Log($"[AlwaysMaxScreen] Updated - Canvas: {canvasWidth}x{canvasHeight}, Size: {canvasWidth}x{canvasHeight}, Position: (0, 0), Parent: {parentRect.name}");
        }
    }
}