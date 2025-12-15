# NoisyBird Window System

Unity 기반 게임에서 사용할 수 있는 강력한 공통 UI Window 시스템입니다.

## 주요 기능

### 🎯 핵심 기능
- **5단계 Window 타입**: Underlay, Screen, Popup, Overlay, Toast
- **스택 기반 관리**: Screen/Popup은 자동 스택 관리 (LIFO)
- **자동 Hierarchy 정렬**: WindowType에 따라 렌더링 순서 자동 관리
- **상태 저장/복구**: 씬 전환 후에도 Window 상태 완벽 복구
- **씬 전환 규칙**: Window별로 씬 전환 시 동작 정의 (파괴/숨김/유지)

### 🔧 고급 기능
- **자동 상태 관리**: `AutoStateAttribute` 기반 자동 상태 저장/복구
- **커스텀 리소스 로더**: 프로젝트별 Window 로딩 방식 커스터마이징
- **에디터 도구**: 실시간 모니터링 및 디버깅 도구

## Window 타입

렌더링 순서 (아래 → 위):

```
Underlay (0)  ← 배경 UI
    ↓
Screen/Popup (1)  ← 스택 관리, 나중에 열린 것이 위
    ↓
Overlay (2)  ← 글로벌 HUD, 상단바
    ↓
Toast (3)  ← 알림, 최상위
```

- **Underlay**: 배경 UI (비스택)
- **Screen**: 전체 화면 UI (스택)
- **Popup**: 팝업 UI (스택)
- **Overlay**: 항상 떠있는 UI (비스택)
- **Toast**: 임시 알림 (비스택)

## 설치

Unity Package Manager를 통해 설치할 수 있습니다.

## 빠른 시작

### 1. Window 생성

```csharp
using NoisyBird.WindowSystem;

public class MyWindow : WindowBase
{
    private void Start()
    {
        // Window 등록
        WindowManager.Instance.RegisterWindow(this);
    }

    public override WindowState CaptureState()
    {
        // Window 상태 저장 로직
        return new MyWindowState();
    }

    public override void RestoreState(WindowState state)
    {
        // Window 상태 복구 로직
    }
}
```

### 2. Window 관리

```csharp
// Window 열기
WindowManager.Instance.OpenWindow("MyWindowId");

// Window 닫기
WindowManager.Instance.CloseWindow("MyWindowId");

// 최상위 스택 Window 닫기 (Screen/Popup만)
WindowManager.Instance.CloseTopWindow();

// 모든 Window 닫기
WindowManager.Instance.CloseAllWindows(saveStates: true);
```

### 3. 자동 상태 관리

```csharp
public class SettingsWindow : AutoStateWindow
{
    [AutoState] private ScrollRect scrollRect;
    [AutoState] private Toggle soundToggle;
    [AutoState] private Slider volumeSlider;
    
    // 자동으로 상태 저장/복구됨!
}
```

### 4. 커스텀 Window 로더

```csharp
// 게임 시작 시 로더 설정
WindowManager.Instance.SetWindowLoader(LoadWindowFromResources);

private WindowBase LoadWindowFromResources(string windowId)
{
    GameObject prefab = Resources.Load<GameObject>("Windows/" + windowId);
    GameObject instance = Instantiate(prefab);
    return instance.GetComponent<WindowBase>();
}

// 이제 등록되지 않은 Window도 자동 로드됨
WindowManager.Instance.OpenWindow("NewWindow");
```

## 에디터 도구

### Window Manager 에디터 윈도우
**메뉴**: `NoisyBird > Window System > Window Manager`

- 📊 실시간 Window 모니터링
- 🎮 Window 열기/닫기 제어
- 💾 상태 저장/복구 관리
- 📋 스택/비스택 Window 구분 표시

### 커스텀 인스펙터
`WindowBase`를 상속받는 모든 컴포넌트에 자동 적용:
- Window ID 자동 채우기
- WindowType 및 SceneRule 설정
- 런타임 제어 버튼

### 메뉴 아이템
**메뉴**: `GameObject > NoisyBird > Window System`
- Create Window Manager
- Create Empty Window
- Create Canvas with Window Root

자세한 내용은 [Editor Tools 문서](Editor/EDITOR_TOOLS.md)를 참고하세요.

## 고급 사용법

### Hierarchy 자동 정렬

Window가 열릴 때 자동으로 WindowType에 따라 Hierarchy 순서가 정렬됩니다:

```csharp
// 자동으로 올바른 순서로 정렬됨
WindowManager.Instance.OpenWindow("Background");  // Underlay
WindowManager.Instance.OpenWindow("Inventory");   // Screen
WindowManager.Instance.OpenWindow("Confirm");     // Popup (Screen보다 위)
WindowManager.Instance.OpenWindow("HUD");         // Overlay (Popup보다 위)
WindowManager.Instance.OpenWindow("Toast");       // Toast (최상위)
```

### 씬 전환 규칙

```csharp
public class MyWindow : WindowBase
{
    private void Awake()
    {
        // 씬 전환 시 파괴
        SceneRule = WindowSceneRule.DestroyOnSceneChange;
        
        // 씬 전환 시 숨김 (상태는 유지)
        // SceneRule = WindowSceneRule.HideOnSceneChange;
        
        // 씬 전환 시 유지 (DontDestroyOnLoad)
        // SceneRule = WindowSceneRule.KeepOnSceneChange;
    }
}
```

## 예제

패키지에 포함된 예제:
- `InventoryWindow`: 수동 상태 관리 예제
- `SettingsWindow`: 자동 상태 관리 예제
- `ConfirmPopup`: 간단한 팝업 예제
- `WindowSystemExample`: 시스템 테스트 스크립트
- `WindowLoaderExample`: 커스텀 로더 예제

## 버전 히스토리

### 1.0.2 (현재)
- **Window 타입 확장**
  - Underlay 타입 추가
  - 5단계 렌더링 순서 (Underlay < Screen/Popup < Overlay < Toast)
- **스택 관리 개선**
  - Screen/Popup을 `Stack<WindowBase>`로 변경
  - `CloseTopWindow()` 메서드 추가
  - 자동 Hierarchy 정렬 시스템
- **커스텀 로더 시스템**
  - `WindowLoaderDelegate` 추가
  - 프로젝트별 리소스 로딩 커스터마이징
- **버그 수정**
  - `OnSceneUnloaded` 스택 처리 수정

### 1.0.1
- 에디터 도구 추가
  - WindowManagerEditorWindow (실시간 모니터링)
  - WindowBaseEditor (커스텀 인스펙터)
  - WindowSystemMenuItems (메뉴 아이템)
- 에디터 도구 문서 추가

### 1.0.0
- 초기 릴리즈
- WindowBase, WindowManager, WindowState 핵심 클래스
- WindowType, WindowSceneRule Enum
- 기본 상태 저장/복구 시스템

## 라이선스

MIT License

## 지원

문의사항이나 버그 리포트는 이슈 트래커를 이용해주세요.
