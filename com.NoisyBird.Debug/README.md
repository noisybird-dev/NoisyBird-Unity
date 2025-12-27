# NoisyBird Debug Package

`com.NoisyBird.Debug` 패키지는 개발 생산성을 높이기 위한 디버그 유틸리티와 강력한 런타임/에디터 치트 시스템을 제공합니다.

## Features

### 1. Conditional Debugger
`USE_DEBUG` 심볼이 정의된 경우에만 로그를 출력합니다. 빌드 시 불필요한 로그 비용을 제거할 수 있습니다.

```csharp
using NoisyBird.Debug;

// 기본 로그 (DebugType 미지정 시 [Temp] 태그)
Debugger.Log("Temporary Log"); 

// 타입 지정 로그
Debugger.Log(DebugType.Server, "Server Connected");
Debugger.Log(DebugType.Data, "Data Loaded");
```

### 2. Attribute-Based Cheat System
메소드, 프로퍼티, 필드에 `[NBCheat]` 어트리뷰트를 붙여 에디터 윈도우와 런타임 UI에서 제어할 수 있습니다.
- **지원 대상**: `Static` 멤버, 씬에 존재하는 `Instance` 멤버 (MonoBehaviour)
- **지원 타입**: `int`, `float`, `string`, `bool`, `Enum`, `Method` (파라미터 없음)

#### 사용 예시
```csharp
using UnityEngine;
using NoisyBird.Debug;

public enum MyCheatCategory { Player, System }

public class PlayerController : MonoBehaviour
{
    // 카테고리와 정렬 순서 지정
    [NBCheat(MyCheatCategory.Player, 0)]
    public float MoveSpeed { get; set; } = 5.0f;

    [NBCheat(MyCategory.Player, 1)]
    private void Kill() 
    { 
        Debugger.Log("Player Killed"); 
    }
    
    // 카테고리를 문자열로 지정 가능
    [NBCheat("Game System", 0)]
    public static void ResetGame() 
    { 
        Debugger.Log("Game Reset"); 
    }
}
```

### 3. Cheat Window (Editor)
- **메뉴 위치**: `NoisyBird > Cheat Window`
- **기능**:
    - **Toggle USE_DEBUG**: `USE_DEBUG` 디파인 심볼 활성화/비활성화
    - **Toggle USE_CHEAT**: 런타임 치트 UI 활성화/비활성화
    - **Cheat Controls**: `[NBCheat]`가 적용된 모든 항목 제어

### 4. Runtime Cheat UI (In-Game)
- **활성화 조건**: `!UNITY_EDITOR` (빌드 환경) && `USE_CHEAT` 심볼 정의 시
- **사용법**: 
    - 화면 좌측 상단의 **Cheat** 버튼 클릭 (Safe Area 및 해상도 대응)
    - 에디터와 동일한 치트 컨트롤 사용 가능

## Installation

1. `NoisyBird` 패키지들이 설치된 프로젝트에 `com.NoisyBird.Debug` 폴더를 추가합니다.
2. Unity Package Manager가 패키지를 인식하면 사용 가능합니다.
