# NoisyBird Unity Packages

Unity 게임 개발을 위한 NoisyBird 패키지 모음입니다.

## 📦 패키지 목록

### 1. [Window System](./com.NoisyBird.WindowSystem/README.md) ⭐ NEW
**Version**: `1.0.2`

강력한 UI Window 관리 시스템
- 5단계 Window 타입 (Underlay, Screen, Popup, Overlay, Toast)
- 스택 기반 관리 및 자동 Hierarchy 정렬
- 상태 저장/복구 시스템
- 커스텀 리소스 로더

#### Install via UPM (Git URL)
```text
https://github.com/noisybird-dev/NoisyBird-Unity.git?path=/com.NoisyBird.WindowSystem#v1.0.2
```

---

### 2. [UI Extension](./com.NoisyBird.UIExtension/README.md)
**Version**: `1.0.1`

Unity UI 컴포넌트 확장 라이브러리

#### Install via UPM (Git URL)
```text
https://github.com/noisybird-dev/NoisyBird-Unity.git?path=/com.NoisyBird.UIExtension
```

---

### 3. [Mono Extension](./com.NoisyBird.MonoExtension/README.md)
**Version**: `1.0.0`

MonoBehaviour 확장 유틸리티

#### Install via UPM (Git URL)
```text
https://github.com/noisybird-dev/NoisyBird-Unity.git?path=/com.NoisyBird.MonoExtension
```

---

## 🚀 빠른 설치

### Unity Package Manager 사용
1. Unity 에디터에서 `Window > Package Manager` 열기
2. 좌측 상단 `+` 버튼 클릭
3. `Add package from git URL...` 선택
4. 위의 Git URL 중 하나를 입력

### manifest.json 사용
프로젝트의 `Packages/manifest.json` 파일에 추가:

```json
{
  "dependencies": {
    "com.noisybird.windowsystem": "https://github.com/noisybird-dev/NoisyBird-Unity.git?path=/com.NoisyBird.WindowSystem#v1.0.2",
    "com.noisybird.uiextension": "https://github.com/noisybird-dev/NoisyBird-Unity.git?path=/com.NoisyBird.UIExtension",
    "com.noisybird.monoextension": "https://github.com/noisybird-dev/NoisyBird-Unity.git?path=/com.NoisyBird.MonoExtension"
  }
}
```

---

## 📝 라이선스

MIT License

## 🔗 링크

- GitHub: https://github.com/noisybird-dev/NoisyBird-Unity
