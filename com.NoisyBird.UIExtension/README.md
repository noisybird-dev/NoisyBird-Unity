# UI Extension (NoisyBird)

## Unity UGUI


## Version

### 1.0.6
[SafeArea] Custom SafeArea Overlay 기능 추가
- Scene View에 Custom SafeArea Overlay 추가
- SafeArea 프리셋 저장/불러오기/삭제 기능
- 비율 기반 프리셋 시스템 (해상도 독립적)
- 단축키 지원 (Shift + 1~9)
- 디바이스 프리셋 로드 기능 (38개 디바이스 지원)
  - iPhone 시리즈 (14/15/13/12 Pro, SE, 8)
  - iPad 시리즈 (Pro 12.9", Pro 11", Air)
  - Samsung Galaxy 폴더블 (Z Fold 5, Z Flip 5)
  - Portrait/Landscape 모두 지원
- EditorPrefs를 통한 프리셋 영구 저장

### 1.0.5
[SafeArea] Edit Mode 해상도 이슈 수정
- Game View와 Device Simulator 간의 해상도 불일치 문제 해결
- Screen.currentResolution과 Screen.width/height를 상황에 맞게 사용하도록 개선

### 1.0.4
[SafeArea] SafeArea 회전 이슈 수정
- 회전 시 Screen.safeArea가 업데이트되지 않는 문제 수정
- WaitForEndOfFrame 후 SafeAreaManager.Refresh() 호출 추가

[AlwaysMaxScreen] AlwaysMaxScreen 회전 이슈 수정
- Canvas World Corners 대신 Canvas Rect 크기 직접 사용
- 회전 시 크기 및 위치 계산 오류 수정
- EditorCoroutine 지원 추가

[Package] com.unity.editorcoroutines 의존성 추가

### 1.0.3
[SafeArea] SafeArea 기능 추가
- SafeAreaManager 싱글톤 클래스 추가 (순수 C# 클래스)
- SafeArea 컴포넌트 추가 (UIBehaviour 상속)
- 에디터 모드 및 플레이 모드 지원 ([ExecuteAlways])
- CanvasScaler와 호환되는 자동 크기 조정
- Device Simulator 및 일반 Game View 지원
- 해상도 변경 및 회전 시 자동 업데이트

### 1.0.2
[ButtonEx] Scale Punch Transition 추가
- Click Transition 옵션 추가 (Scale Punch)
- Scale Punch 관련 설정 (Punch Scale, Punch Duration) 추가
- Animation Transition 과의 충돌 경고 기능 추가
- PlaySound 가상 함수 추가

[CanvasScalerEx] 기능 추가
- SetMatchValue 정적 함수 추가 (모든 CanvasScalerEx의 Match 값 일괄 변경)

[ImageEx]
- 기본 확장 클래스 추가

[RawImageEx]
- 기본 확장 클래스 추가

### 1.0.1
[Quickly Preset] 빠른 Default 설정 추가



### 1.0.0
[Quickly Preset] 빠른 프리셋 기능 추가