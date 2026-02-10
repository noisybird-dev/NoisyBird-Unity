# NoisyBird Editor Extension Package

`com.NoisyBird.EditorExtension` 패키지는 Unity 에디터 확장 기능을 제공합니다.

## Features

### 1. Define Symbol Editor
스크립팅 Define Symbol을 쉽게 관리할 수 있는 에디터 윈도우입니다.
- Define Symbol 추가/삭제
- 프로파일별 Define Symbol 저장/로드
- 에디터에 즉시 적용

### 2. Scene Toolbar
Unity 에디터 툴바에 씬 전환 기능을 추가합니다.
- 빠른 씬 전환
- 커스텀 씬 추가
- 첫 번째 씬으로 플레이 모드 시작

### 3. CSV Diff Editor
CSV 파일 비교 도구입니다.
- CSV 파일 간 차이점 시각화
- 변경된 셀 하이라이트
- 추가/삭제/수정 내용 표시

### 4. Editor Utilities

#### EditorGUIHelper
에디터 GUI 헬퍼 함수들을 제공합니다.
- 커스텀 헤더 그리기
- 토글 가능한 섹션

#### TextInputPopup
간단한 텍스트 입력 팝업 윈도우입니다.

#### OSHelper
OS별 프로세스 실행 및 폴더 열기 기능을 제공합니다.

### 5. Extension Methods

#### StringExtensions
- `IsNullOrEmpty()`: 문자열이 null이거나 비어있는지 확인

#### CollectionExtensions
- `AddRange()`: HashSet에 여러 항목 추가

#### SceneAssetExtensions
- `GetPath()`: SceneAsset의 경로 가져오기

### 6. Utility Classes

#### DefineSymbolManager
Unity 스크립팅 Define Symbol 관리 유틸리티입니다.
- `IsSymbolAlreadyDefined()`: Symbol 정의 여부 확인
- `AddDefineSymbol()`: Symbol 추가
- `RemoveDefineSymbol()`: Symbol 제거

#### JsonConvert
Unity JsonUtility를 래핑한 JSON 직렬화 유틸리티입니다.
- `SerializeObject()`: 객체를 JSON 문자열로 변환
- `DeserializeObject<T>()`: JSON 문자열을 객체로 변환

#### ToolbarExtender
Unity 에디터 툴바를 확장할 수 있는 유틸리티입니다.
- 좌측/우측 툴바에 커스텀 GUI 추가 가능

## Installation

1. `NoisyBird` 패키지들이 설치된 프로젝트에 `com.NoisyBird.EditorExtension` 폴더를 추가합니다.
2. Unity Package Manager가 패키지를 인식하면 사용 가능합니다.

