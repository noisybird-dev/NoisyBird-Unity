# NoisyBird Addressable Extension

Unity Addressable Asset System을 더 쉽고 효율적으로 사용하기 위한 확장 패키지입니다.
레퍼런스 카운팅 기반의 리소스 관리, 자동 그룹화, 리모트 다운로드 편의 기능 등을 제공합니다.

## 📦 설치 (Installation)

1. 이 패키지는 `com.unity.addressables` 패키지에 의존성이 있습니다.
2. `Packages/manifest.json`에 본 패키지를 추가하거나, Assets 폴더 내에 배치하여 사용합니다.

## ✨ 주요 기능 (Features)

### 1. 리소스 관리 (Resource Management)
`AddressableManager` 싱글톤을 통해 리소스를 중앙에서 관리합니다. 내부적으로 레퍼런스 카운팅(Reference Counting)을 사용하여 중복 로드를 방지하고 안전하게 해제합니다.

#### 기본 사용법
```csharp
// 비동기 로드
AddressableManager.Instance.LoadAsset<GameObject>("MyPrefab", (prefab) => {
    Debug.Log("Loaded: " + prefab.name);
});

// 동기 로드 (주의: 메인 스레드 차단)
var content = AddressableManager.Instance.LoadAssetSync<TextAsset>("MyData");
```

#### 리소스 해제
```csharp
// 단일 해제 (RefCount 감소)
AddressableManager.Instance.ReleaseAsset("MyPrefab");

// 강제 전체 해제 (RefCount 무시하고 즉시 메모리 해제)
AddressableManager.Instance.ReleaseAllRef("MyPrefab");
```

#### 태그(Tag) 기반 관리
특정 컨텍스트(예: 씬, 팝업) 단위로 리소스를 묶어서 관리할 수 있습니다.
```csharp
// 태그와 함께 로드
AddressableManager.Instance.LoadAsset<Texture>("MyImage", null, null, "IntroScene");

// 해당 태그로 로드된 모든 리소스를 일괄 해제
// (각 리소스의 로드 횟수만큼 Release를 수행하여 안전하게 정리)
AddressableManager.Instance.ReleaseByTag("IntroScene");
```

### 2. GameObject 생명주기 자동화
`LoadGameObject`를 사용하면, 인스턴스화된 GameObject에 `AddressableLifecycleLinker` 컴포넌트가 자동으로 부착됩니다.
이 컴포넌트는 GameObject가 `Destroy` 될 때 자동으로 `ReleaseAsset`을 호출하여 어드레서블 핸들을 정리합니다.

```csharp
AddressableManager.Instance.LoadGameObject("MyCharacterPrefab", parentTransform, (instance) => {
    // instance가 Destroy되면, 자동으로 Addressable RefCount가 감소함
});
```

### 3. 에디터 자동화 (Editor Automation)
특정 폴더에 있는 에셋들을 자동으로 Addressable Group에 할당하고 라벨을 붙여주는 기능을 제공합니다.

#### 설정 방법
1. 상단 메뉴 `Noisy Bird > Addressable > Config`를 클릭합니다.
2. `AutoAddressableConfig` 에셋이 `Assets/Resources/NoisyBird/AddressableExtension/` 경로에 생성되거나 선택됩니다.
3. `Rules` 리스트에 원하는 규칙을 추가합니다.
   - **FolderPath**: 모니터링할 폴더 경로 (예: `Assets/GameData/Items`)
   - **GroupName**: 할당할 Addressable 그룹 이름 (없는 경우 자동 생성)
   - **Labels**: 추가할 라벨 목록
   - **SimplifyAddress**: 체크 시 파일 확장자를 제외한 이름을 주소로 사용

설정 후 해당 폴더에 에셋이 추가되거나 변경되면, 자동으로 그룹과 라벨이 설정됩니다.

### 4. 리모트 다운로드 (Remote Download)
`AddressableDownloader`를 통해 리모트 리소스의 다운로드 크기를 확인하고, 진행률(Progress)을 모니터링하며 다운로드할 수 있습니다.

```csharp
// 다운로드 크기 확인
AddressableDownloader.GetDownloadSizeAsync("RemoteLabel", (size) => {
    Debug.Log($"Download Size: {size} bytes");
});

// 다운로드 및 진행률 표시
AddressableDownloader.DownloadDependenciesAsync("RemoteLabel", 
    (progress) => {
        Debug.Log($"Downloading... {progress * 100}%");
    },
    (success) => {
        if (success) Debug.Log("Download Complete!");
    }
);
```

## 🛠️ 요구 사항 (Requirements)
- Unity 2021.3 이상 (권장)
- Addressables 1.19.0 이상

## 📝 업데이트 내역 (Release Notes)

### 1.0.0
- **AddressableManager**: 싱글톤 매니저, 레퍼런스 카운팅, `LoadAssetSync`, `ReleaseAllRef`, `ReleaseByTag`.
- **GameObject Lifecycle**: `AddressableLifecycleLinker`를 통한 자동 해제.
- **Editor Automation**: `AutoAddressableConfig` 및 `AddressableConfigMenu`를 통한 그룹/라벨 자동 설정.
- **Remote Download**: `AddressableDownloader` 유틸리티 추가.
