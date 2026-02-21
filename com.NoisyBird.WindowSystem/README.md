# NoisyBird Window System

Unity 기반 게임에서 사용할 수 있는 강력한 공통 UI Window 시스템입니다.

## 주요 기능

### 핵심 기능
- **5단계 Window 타입**: Underlay, Screen, Popup, Toast, Overlay
- **ScreenGroup 기반 관리**: Screen + Popup을 그룹으로 묶어 전환 관리
- **전용 UI 카메라**: UI 레이어만 촬영하는 전용 카메라 자동 생성
- **WindowType별 Canvas**: 각 타입별 독립 Canvas (ScreenSpace-Camera, Sort Order 자동 설정)
- **애니메이션 시스템**: async/await 기반 열기/닫기 애니메이션 지원
- **상태 저장/복구**: Window 상태 완벽 복구

### 고급 기능
- **자동 상태 관리**: `AutoStateAttribute` 기반 자동 상태 저장/복구
- **커스텀 리소스 로더**: 프로젝트별 Window 로딩 방식 커스터마이징
- **애니메이션 delegate**: `OnWindowAnim` 으로 터치 차단 등 프로젝트별 처리
- **에디터 도구**: 실시간 모니터링 및 디버깅 도구

## Window 타입

렌더링 순서 (Sort Order):

```
Underlay (0)  → Sort Order 100  ← 배경 UI, 닫기 시 Destroy
    ↓
Screen (1)    → Sort Order 200  ← 전체 화면, ScreenGroup으로 관리
    ↓
Popup (2)     → Sort Order 300  ← Screen 위 팝업, 닫기 시 Destroy
    ↓
Toast (3)     → Sort Order 400  ← 알림, 닫기 시 SetActive(false)
    ↓
Overlay (4)   → Sort Order 500  ← 글로벌 HUD, 닫기 시 SetActive(false)
```

### 닫기 규칙
| WindowType | 닫기 방식 | 비고 |
|---|---|---|
| Underlay | Destroy | 항상 파괴 |
| Screen | Destroy + 이전 그룹 복원 | ScreenGroup 단위 관리 |
| Popup | Destroy | 소속 Screen 숨김 시 함께 숨김 |
| Toast | SetActive(false) | `DestroyAllWindows()`로만 파괴 |
| Overlay | SetActive(false) | `DestroyAllWindows()`로만 파괴 |

## 설치

Unity Package Manager를 통해 설치할 수 있습니다.

## 빠른 시작

### 0. Window Manager 설정

```
Unity 메뉴 > GameObject > Noisy Bird > Window System > Create Window Manager with Containers
```

다음과 같은 계층 구조가 생성됩니다:

```
WindowManager (DontDestroyOnLoad)
├── WindowSystemCamera (Camera: depth-only, UI layer, orthographic)
├── UnderlayContainer (Canvas: ScreenSpace-Camera, SortOrder=100)
├── ScreenContainer (Canvas: ScreenSpace-Camera, SortOrder=200)
├── PopupContainer (Canvas: ScreenSpace-Camera, SortOrder=300)
├── ToastContainer (Canvas: ScreenSpace-Camera, SortOrder=400)
└── OverlayContainer (Canvas: ScreenSpace-Camera, SortOrder=500)
```

### 1. Window 생성

```csharp
using NoisyBird.WindowSystem;

public class MyWindow : WindowBase
{
    private void Start()
    {
        WindowManager.Instance.RegisterWindow(this);
    }

    public override WindowState CaptureState()
    {
        return new MyWindowState();
    }

    public override void RestoreState(WindowState state)
    {
        // 상태 복구 로직
    }
}
```

### 2. Window 관리

```csharp
// Window 열기 (async)
await WindowManager.Instance.OpenWindow("MyWindowId");

// Window 닫기 (async)
await WindowManager.Instance.CloseWindow("MyWindowId");

// 최상위 Window 닫기 (Popup 우선, 없으면 Screen)
await WindowManager.Instance.CloseTopWindow();

// 모든 ScreenGroup 파괴 (Toast/Overlay 유지)
WindowManager.Instance.CloseAllScreenGroups();

// 모든 Window 닫기 (ScreenGroup Destroy + Toast/Overlay SetActive false)
WindowManager.Instance.CloseAllWindows(saveStates: true);

// 전체 리셋 (로그아웃 등, 모든 Window Destroy)
WindowManager.Instance.DestroyAllWindows();
```

### 3. ScreenGroup 동작

```
1. Screen A 열림 → ScreenGroup A 생성
2. Popup X 열림 → ScreenGroup A에 추가
3. Screen B 열림 → ScreenGroup A 숨김 → ScreenGroup B 생성
   (Screen B 열기 애니메이션 + Screen A 닫기 애니메이션 동시 재생)
4. Screen B 닫힘 → ScreenGroup B Destroy → ScreenGroup A 복원
   (Screen A 열기 애니메이션 + Screen B 닫기 애니메이션 동시 재생)
```

### 4. 애니메이션

```csharp
public class MyScreen : WindowBase
{
    [SerializeField] private CanvasGroup _canvasGroup;

    protected override async Task PlayOpenAnimation()
    {
        // DOTween, Animator 등 사용 가능
        _canvasGroup.alpha = 0f;
        while (_canvasGroup.alpha < 1f)
        {
            _canvasGroup.alpha += Time.deltaTime * 3f;
            await Task.Yield();
        }
    }

    protected override async Task PlayCloseAnimation()
    {
        while (_canvasGroup.alpha > 0f)
        {
            _canvasGroup.alpha -= Time.deltaTime * 3f;
            await Task.Yield();
        }
    }

    // ... CaptureState, RestoreState 생략
}
```

### 5. 터치 차단 (프로젝트별 설정)

```csharp
WindowManager.Instance.OnWindowAnim = (isAnimating) =>
{
    touchBlocker.SetActive(isAnimating);
};
```

### 6. Destroy 이벤트 (Addressable 해제 등)

```csharp
var window = WindowManager.Instance.GetWindow("MyWindow");
window.OnWindowDestroy += (w) =>
{
    // Addressable 에셋 해제 등
    Addressables.Release(handle);
};
```

### 7. 자동 상태 관리

```csharp
public class SettingsWindow : AutoStateWindow
{
    [AutoState] private ScrollRect scrollRect;
    [AutoState] private Toggle soundToggle;
    [AutoState] private Slider volumeSlider;

    // 자동으로 상태 저장/복구됨!
}
```

### 8. 커스텀 Window 로더

```csharp
WindowManager.Instance.SetWindowLoader(LoadWindowFromResources);

private WindowBase LoadWindowFromResources(string windowId)
{
    GameObject prefab = Resources.Load<GameObject>("Windows/" + windowId);
    GameObject instance = Instantiate(prefab);
    return instance.GetComponent<WindowBase>();
}

// 등록되지 않은 Window도 자동 로드됨
await WindowManager.Instance.OpenWindow("NewWindow");
```

## 에디터 도구

### Window Manager 에디터 윈도우
**메뉴**: `NoisyBird > Window System > Window Manager`

- 실시간 Window 모니터링
- ScreenGroup 트리 구조 시각화
- Window 열기/닫기 제어
- 상태 저장/복구 관리
- Destroy All 버튼

### 커스텀 인스펙터
`WindowBase`를 상속받는 모든 컴포넌트에 자동 적용:
- Window ID 자동 채우기
- WindowType 설정
- 런타임 제어 버튼

### 메뉴 아이템
**메뉴**: `GameObject > Noisy Bird > Window System`
- **Create Window Manager with Containers** - 전용 카메라 + Container 기반 WindowManager 생성
- Create Empty Window
- Create Canvas with Window Root

자세한 내용은 [Editor Tools 문서](Editor/EDITOR_TOOLS.md)를 참고하세요.

## 예제

패키지에 포함된 예제:
- `InventoryWindow`: 수동 상태 관리 예제
- `SettingsWindow`: 자동 상태 관리 예제
- `ConfirmPopup`: 간단한 팝업 예제
- `WindowSystemExample`: 시스템 테스트 스크립트
- `WindowLoaderExample`: 커스텀 로더 예제

## 버전 히스토리

### 1.1.0 (현재)
- **전용 UI 카메라**
  - WindowSystemCamera 자동 생성 (depth-only, UI 레이어만 촬영, orthographic)
- **WindowType별 독립 Canvas**
  - 각 Container에 ScreenSpace-Camera Canvas 자동 부착
  - SortingLayer: UI, Sort Order: (WindowType + 1) * 100
  - CanvasScaler (ScaleWithScreenSize, 1920x1080)
- **ScreenGroup 구조 도입**
  - Screen + Popup을 그룹으로 묶어 관리
  - Screen 전환 시 이전 그룹 자동 숨김/복원
  - `Stack<ScreenGroup>` 기반 LIFO 관리
- **닫기 모드 분리**
  - Underlay/Screen/Popup: Destroy (완전 닫기)
  - Toast/Overlay: SetActive(false) (임시 닫기)
  - `DestroyAllWindows()`: 전체 리셋용
  - `CloseAllScreenGroups()`: ScreenGroup만 파괴
- **애니메이션 시스템**
  - `PlayOpenAnimation()` / `PlayCloseAnimation()` async Task 메서드
  - Screen 전환 시 양쪽 애니메이션 동시 재생 (`Task.WhenAll`)
  - `OnWindowAnim` delegate로 터치 차단 위임
- **Destroy 이벤트**
  - `OnWindowDestroy` 이벤트 (Addressable 에셋 해제 등에 활용)
- **WindowSceneRule 제거**
  - 씬 전환 규칙 삭제 (ScreenGroup 구조로 대체)
- **OpenWindow/CloseWindow async 전환**
  - `Task<bool>` 반환으로 애니메이션 완료 대기 가능

### 1.0.4
- 계층 구조 개선 (WindowType별 Container)
- Hierarchy 순서 업데이트 최적화

### 1.0.3
- 버그 수정

### 1.0.2
- Underlay 타입 추가
- Stack 관리 개선, CloseTopWindow 추가
- 커스텀 로더 시스템

### 1.0.1
- 에디터 도구 추가

### 1.0.0
- 초기 릴리즈

## 라이선스

MIT License

## 지원

문의사항이나 버그 리포트는 이슈 트래커를 이용해주세요.
