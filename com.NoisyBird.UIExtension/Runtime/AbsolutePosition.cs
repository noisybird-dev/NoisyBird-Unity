using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

namespace NoisyBird.UIExtension.UI
{
    /// <summary>
    /// SafeArea 또는 Canvas를 기준으로 절대 위치를 설정하는 컴포넌트
    /// 이 컴포넌트가 있으면 RectTransform은 이 컴포넌트를 통해서만 조절됩니다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class AbsolutePosition : UIBehaviour
    {
        public enum AnchorTarget
        {
            Canvas,
            SafeArea
        }

        public enum AnchorPreset
        {
            TopLeft,
            TopCenter,
            TopRight,
            MiddleLeft,
            MiddleCenter,
            MiddleRight,
            BottomLeft,
            BottomCenter,
            BottomRight
        }

        public enum StretchMode
        {
            None,
            Width,
            Height,
            All
        }

        [SerializeField] private AnchorTarget _anchorTarget = AnchorTarget.Canvas;
        [SerializeField] private AnchorPreset _anchorPreset = AnchorPreset.MiddleCenter;
        [SerializeField] private StretchMode _stretchMode = StretchMode.None;
        [SerializeField] private Vector2 _offset = Vector2.zero;
        [SerializeField] private Vector2 _size = new Vector2(100, 100);

        private RectTransform _rectTransform;
        private RectTransform _rootCanvasRect;
        private UnityEngine.Canvas _rootCanvas;
        
        private bool _needsUpdate = false;
        private Coroutine _delayedUpdateCoroutine;
#if UNITY_EDITOR
        private EditorCoroutine _delayedUpdateEditorCoroutine;
#endif
        private Vector2 _lastTargetSize;

        public AnchorTarget Target
        {
            get => _anchorTarget;
            set
            {
                if (_anchorTarget != value)
                {
                    _anchorTarget = value;
                    _needsUpdate = true;
                }
            }
        }

        public AnchorPreset Preset
        {
            get => _anchorPreset;
            set
            {
                if (_anchorPreset != value)
                {
                    _anchorPreset = value;
                    _needsUpdate = true;
                }
            }
        }

        public StretchMode Stretch
        {
            get => _stretchMode;
            set
            {
                if (_stretchMode != value)
                {
                    _stretchMode = value;
                    _needsUpdate = true;
                }
            }
        }

        public Vector2 Offset
        {
            get => _offset;
            set
            {
                if (_offset != value)
                {
                    _offset = value;
                    _needsUpdate = true;
                }
            }
        }

        public Vector2 Size
        {
            get => _size;
            set
            {
                if (_size != value)
                {
                    _size = value;
                    _needsUpdate = true;
                }
            }
        }

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

        private DrivenRectTransformTracker _tracker;

        protected override void OnEnable()
        {
            base.OnEnable();
            UnityEngine.Canvas.willRenderCanvases += OnWillRenderCanvases;
            
            Init();
            
            // SafeArea 타겟인 경우 SafeAreaManager 이벤트 구독
            if (_anchorTarget == AnchorTarget.SafeArea)
            {
                SafeArea.SafeAreaManager.Instance.OnSafeAreaChanged += OnSafeAreaChanged;
                SafeArea.SafeAreaManager.Instance.Refresh();
            }
            
            UpdatePosition();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnityEngine.Canvas.willRenderCanvases -= OnWillRenderCanvases;
            
            StopUpdateCoroutine();
            
            if (_anchorTarget == AnchorTarget.SafeArea && SafeArea.SafeAreaManager.Instance != null)
            {
                SafeArea.SafeAreaManager.Instance.OnSafeAreaChanged -= OnSafeAreaChanged;
            }
            
            _tracker.Clear();
        }

        // ... (중략) ...

        private void UpdatePosition()
        {
            if (_rectTransform == null || _rootCanvasRect == null) return;

            _tracker.Clear();
            _tracker.Add(this, _rectTransform, DrivenTransformProperties.All);

            Vector2 targetSize = GetTargetSize();
            Vector2 targetPosition = GetTargetPosition();
            
            // Anchor와 Pivot을 중앙으로 설정
            _rectTransform.anchorMin = Vector2.one * 0.5f;
            _rectTransform.anchorMax = Vector2.one * 0.5f;
            _rectTransform.pivot = Vector2.one * 0.5f;
            
            // 크기 설정 (Stretch 모드 고려)
            Vector2 finalSize = _size;
            if (_stretchMode == StretchMode.Width || _stretchMode == StretchMode.All)
                finalSize.x = targetSize.x;
            if (_stretchMode == StretchMode.Height || _stretchMode == StretchMode.All)
                finalSize.y = targetSize.y;
                
            _rectTransform.sizeDelta = finalSize;
            
            // 앵커 프리셋에 따른 위치 계산
            Vector2 anchorOffset = GetAnchorOffset(targetSize);
            Vector2 finalPosition = targetPosition + anchorOffset + _offset;
            
            _rectTransform.anchoredPosition = finalPosition;
        }

        private void StartUpdateCoroutine()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && gameObject)
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
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                yield return new WaitForEndOfFrame();
            }
            else
            {
                yield return new EditorWaitForSeconds(0.001f);
            }
#else
            yield return new WaitForEndOfFrame();
#endif
            
            if (_anchorTarget == AnchorTarget.SafeArea)
            {
                SafeArea.SafeAreaManager.Instance.Refresh(false);
            }
            
            UpdatePosition();
            _delayedUpdateCoroutine = null;
            _delayedUpdateEditorCoroutine = null;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            OnWillRenderCanvases();
        }

        private void OnWillRenderCanvases()
        {
            // 타겟 크기 변경 체크
            Vector2 currentTargetSize = GetTargetSize();
            if (_lastTargetSize != currentTargetSize)
            {
                _lastTargetSize = currentTargetSize;
                _needsUpdate = true;
            }
            
            if (_needsUpdate)
            {
                _needsUpdate = false;
                StopUpdateCoroutine();
                StartUpdateCoroutine();
            }
        }

        private void OnSafeAreaChanged(Rect safeArea)
        {
            _needsUpdate = true;
            StopUpdateCoroutine();
            StartUpdateCoroutine();
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

        private Vector2 GetTargetSize()
        {
            if (_anchorTarget == AnchorTarget.SafeArea)
            {
                Rect safeArea = SafeArea.SafeAreaManager.Instance.SafeArea;
                if (_rootCanvasRect != null)
                {
                    // SafeArea를 Canvas 좌표계로 변환
                    Rect canvasRect = _rootCanvasRect.rect;
                    float screenWidth = Screen.width;
                    float screenHeight = Screen.height;

#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        if (safeArea.x + safeArea.width > screenWidth || safeArea.y + safeArea.height > screenHeight)
                        {
                            screenWidth = Screen.currentResolution.width;
                            screenHeight = Screen.currentResolution.height;
                        }
                    }
#endif
                    
                    float safeAreaWidth = (safeArea.width / screenWidth) * canvasRect.width;
                    float safeAreaHeight = (safeArea.height / screenHeight) * canvasRect.height;
                    
                    return new Vector2(safeAreaWidth, safeAreaHeight);
                }
            }
            else // Canvas
            {
                if (_rootCanvasRect != null)
                {
                    return _rootCanvasRect.rect.size;
                }
            }
            
            return Vector2.zero;
        }

        private Vector2 GetTargetPosition()
        {
            if (_rootCanvasRect == null) return Vector2.zero;

            Rect canvasRect = _rootCanvasRect.rect;
            Vector2 targetSize = GetTargetSize();
            
            if (_anchorTarget == AnchorTarget.SafeArea)
            {
                Rect safeArea = SafeArea.SafeAreaManager.Instance.SafeArea;
                float screenWidth = Screen.width;
                float screenHeight = Screen.height;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (safeArea.x + safeArea.width > screenWidth || safeArea.y + safeArea.height > screenHeight)
                    {
                        screenWidth = Screen.currentResolution.width;
                        screenHeight = Screen.currentResolution.height;
                    }
                }
#endif
                
                float safeAreaX = (safeArea.x / screenWidth) * canvasRect.width;
                float safeAreaY = (safeArea.y / screenHeight) * canvasRect.height;
                
                // SafeArea의 좌하단 위치 (Canvas 중심 기준)
                return new Vector2(
                    safeAreaX - canvasRect.width * 0.5f,
                    safeAreaY - canvasRect.height * 0.5f
                );
            }
            else // Canvas
            {
                // Canvas의 좌하단 위치 (Canvas 중심 기준)
                return new Vector2(-canvasRect.width * 0.5f, -canvasRect.height * 0.5f);
            }
        }



        private Vector2 GetAnchorOffset(Vector2 targetSize)
        {
            Vector2 offset = Vector2.zero;
            
            // Stretch 모드일 때는 해당 축에 대해 중앙 정렬 강제
            bool forceCenterX = (_stretchMode == StretchMode.Width || _stretchMode == StretchMode.All);
            bool forceCenterY = (_stretchMode == StretchMode.Height || _stretchMode == StretchMode.All);
            
            // 수평 정렬
            if (forceCenterX)
            {
                offset.x = targetSize.x * 0.5f;
            }
            else
            {
                switch (_anchorPreset)
                {
                    case AnchorPreset.TopLeft:
                    case AnchorPreset.MiddleLeft:
                    case AnchorPreset.BottomLeft:
                        offset.x = _size.x * 0.5f;
                        break;
                    case AnchorPreset.TopCenter:
                    case AnchorPreset.MiddleCenter:
                    case AnchorPreset.BottomCenter:
                        offset.x = targetSize.x * 0.5f;
                        break;
                    case AnchorPreset.TopRight:
                    case AnchorPreset.MiddleRight:
                    case AnchorPreset.BottomRight:
                        offset.x = targetSize.x - _size.x * 0.5f;
                        break;
                }
            }
            
            // 수직 정렬
            if (forceCenterY)
            {
                offset.y = targetSize.y * 0.5f;
            }
            else
            {
                switch (_anchorPreset)
                {
                    case AnchorPreset.BottomLeft:
                    case AnchorPreset.BottomCenter:
                    case AnchorPreset.BottomRight:
                        offset.y = _size.y * 0.5f;
                        break;
                    case AnchorPreset.MiddleLeft:
                    case AnchorPreset.MiddleCenter:
                    case AnchorPreset.MiddleRight:
                        offset.y = targetSize.y * 0.5f;
                        break;
                    case AnchorPreset.TopLeft:
                    case AnchorPreset.TopCenter:
                    case AnchorPreset.TopRight:
                        offset.y = targetSize.y - _size.y * 0.5f;
                        break;
                }
            }
            
            return offset;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 값이 변경될 때 호출됩니다.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            
            if (!Application.isPlaying)
            {
                _needsUpdate = true;
            }
        }
#endif
    }
}
