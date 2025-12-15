# Window System Editor Tools

Window System 패키지의 에디터 도구 사용 가이드입니다.

## 📋 목차

1. [Window Manager 에디터 윈도우](#window-manager-에디터-윈도우)
2. [WindowBase 커스텀 인스펙터](#windowbase-커스텀-인스펙터)
3. [메뉴 아이템](#메뉴-아이템)

---

## Window Manager 에디터 윈도우

**메뉴**: `NoisyBird > Window System > Window Manager`

플레이 모드에서 모든 Window를 실시간으로 모니터링하고 제어할 수 있는 에디터 윈도우입니다.

### 주요 기능

#### 1. Registered Windows (등록된 Window)
- 현재 등록된 모든 Window 목록 표시
- 각 Window의 상세 정보:
  - Window ID
  - Window Type (Screen, Popup, Overlay, Toast)
  - Scene Rule (씬 전환 규칙)
  - 열림 상태
  - GameObject 이름
- 버튼:
  - **Open**: Window 열기
  - **Close**: Window 닫기 (상태 저장)
  - **Save State**: 현재 상태 저장
  - **Select**: Hierarchy에서 GameObject 선택

#### 2. Open Windows Stack (열린 Window 스택)
- 현재 열려있는 Window들을 스택 순서대로 표시
- 나중에 열린 Window가 위에 표시됨
- 각 Window를 개별적으로 닫을 수 있음

#### 3. Saved States (저장된 상태)
- 저장된 모든 Window 상태 목록
- AutoWindowState의 경우 저장된 프로퍼티 상세 정보 표시
- 버튼:
  - **Restore**: 저장된 상태로 Window 열기
  - **Clear**: 저장된 상태 삭제

#### 4. 툴바 기능
- **Refresh**: 수동 새로고침
- **Auto Refresh**: 자동 새로고침 토글 (0.5초마다)
- **Close All Windows**: 모든 Window 닫기
- **Clear All States**: 모든 저장된 상태 삭제

---

## WindowBase 커스텀 인스펙터

WindowBase를 상속받은 모든 컴포넌트에 자동으로 적용되는 커스텀 인스펙터입니다.

### 기능

#### 1. Window Configuration
- **Window ID**: Window 식별자
  - Auto-fill 버튼으로 GameObject 이름 자동 입력
- **Window Type**: Window 타입 선택
- **Scene Rule**: 씬 전환 규칙 선택
  - 각 규칙에 대한 설명 표시

#### 2. Runtime Information (플레이 모드)
- **Is Open**: 현재 열림 상태
- **Is Registered**: WindowManager에 등록 여부
- 제어 버튼:
  - **Open Window**: Window 열기
  - **Close Window**: Window 닫기
  - **Save State**: 상태 저장
  - **Restore State**: 상태 복구
  - **Open Window Manager**: Window Manager 에디터 윈도우 열기

---

## 메뉴 아이템

### GameObject 메뉴

**경로**: `GameObject > NoisyBird > Window System`

#### Create Window Manager
- WindowManager 싱글톤 GameObject 생성
- 이미 존재하는 경우 기존 객체 선택

#### Create Empty Window
- 빈 Window 템플릿 생성
- 사용자가 커스터마이징 가능

#### Create Canvas with Window Root
- Canvas와 Window Root 자동 생성
- Screen Space Overlay 모드
- CanvasScaler, GraphicRaycaster 자동 추가

### NoisyBird 메뉴

**경로**: `NoisyBird > Window System`

#### Documentation
- README.md 파일 열기

#### Open Window Manager
- Window Manager 에디터 윈도우 열기

---

## 사용 예제

### 1. 새 Window 생성하기

```
1. GameObject > NoisyBird > Window System > Create Empty Window
2. Inspector에서 Window ID, Type, Scene Rule 설정
3. 스크립트를 수정하여 CaptureState/RestoreState 구현
4. UI 요소 추가
```

### 2. Window 디버깅하기

```
1. 플레이 모드 진입
2. NoisyBird > Window System > Open Window Manager
3. 실시간으로 Window 상태 모니터링
4. 버튼으로 Window 제어 및 상태 관리
```

### 3. 상태 저장/복구 테스트

```
1. Window Manager 에디터 윈도우 열기
2. Window 열고 UI 조작
3. "Save State" 버튼 클릭
4. Window 닫기
5. "Saved States" 섹션에서 "Restore" 클릭
6. 이전 상태로 복구되는지 확인
```

---

## 팁

- **Auto Refresh**: 실시간 모니터링이 필요하면 켜두고, 성능이 중요하면 끄세요
- **Window ID**: GameObject 이름과 동일하게 유지하면 관리가 편합니다
- **Scene Rule**: DontDestroyOnLoad가 필요한 Window는 KeepOnSceneChange 사용
- **State Debugging**: AutoWindowState 사용 시 저장된 프로퍼티를 에디터에서 확인 가능
