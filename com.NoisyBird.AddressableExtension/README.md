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
GameObject를 로드하는 세 가지 방법을 제공합니다. 모두 `AddressableLifecycleLinker` 컴포넌트가 자동으로 부착되어 GameObject가 `Destroy` 될 때 자동으로 리소스를 해제합니다.

#### 콜백 방식
```csharp
AddressableManager.Instance.LoadGameObject("MyCharacterPrefab", parentTransform, (instance) => {
    // instance가 Destroy되면, 자동으로 Addressable RefCount가 감소함
});
```

#### 동기 방식 (NEW!)
```csharp
GameObject enemy = AddressableManager.Instance.LoadGameObjectSync("Enemy", transform);
enemy.transform.position = new Vector3(0, 0, 0);
// Destroy 시 자동 해제됨
```

#### async/await 방식 (NEW!)
```csharp
async void Start()
{
    GameObject boss = await AddressableManager.Instance.LoadGameObjectAsync("Boss", transform, "Battle");
    boss.transform.position = new Vector3(10, 0, 0);
    // Destroy 시 자동 해제됨
}
```

### 3. 에디터 자동화 (Editor Automation)
특정 폴더에 있는 에셋들을 자동으로 Addressable Group에 할당하고 라벨을 붙여주는 기능을 제공합니다.

#### 설정 방법
1. 상단 메뉴 `Noisy Bird > Addressable > Config`를 클릭합니다.
2. `AutoAddressableConfig` 에셋이 `Assets/Resources/NoisyBird/AddressableExtension/` 경로에 생성되거나 선택됩니다.
3. `Rules` 리스트에 원하는 규칙을 추가합니다.
   - **FolderPath**: 모니터링할 폴더 경로 (예: `Assets/GameData/Items`)
   - **GroupName**: 할당할 Addressable 그룹 이름 (없는 경우 자동 생성, 드롭다운 선택 가능)
   - **Labels**: 추가할 라벨 목록 (드롭다운 선택 및 추가 가능)
   - **SimplifyAddress**: 체크 시 파일 확장자를 제외한 이름을 주소로 사용

#### 편의 기능
- **Context Menu**: Project 뷰에서 폴더 우클릭 > `Add Folder to Addressable Config`를 선택하면 자동으로 해당 폴더가 Config에 추가됩니다.
- **Singleton**: `AutoAddressableConfig.Instance`를 통해 코드에서 설정에 접근할 수 있습니다.

설정 후 해당 폴더에 에셋이 추가되거나 변경되면, 자동으로 그룹과 라벨이 설정됩니다.

### 4. 배치 로드 (Batch Loading) - NEW!
여러 에셋을 한 번에 로드할 수 있는 기능을 제공합니다.

#### 여러 키로 배치 로드
```csharp
// 콜백 방식
AddressableManager.Instance.LoadAssets<Sprite>(
    new[] { "Icon1", "Icon2", "Icon3" },
    (sprites) => {
        foreach (var sprite in sprites)
        {
            Debug.Log($"Loaded: {sprite.name}");
        }
    },
    tag: "UI"
);

// async/await 방식
var sprites = await AddressableManager.Instance.LoadAssetsAsync<Sprite>(
    new[] { "Icon_Sword", "Icon_Shield", "Icon_Potion" },
    "UI"
);
```

#### Label 기반 로드
```csharp
// 콜백 방식
AddressableManager.Instance.LoadAssetsByLabel<Texture>(
    "UITextures",
    (textures) => {
        Debug.Log($"Loaded {textures.Count} textures");
    }
);

// async/await 방식
var uiSprites = await AddressableManager.Instance.LoadAssetsByLabelAsync<Sprite>("UI");
```

### 5. Scene 관리 (Scene Management) - NEW!
Addressable Scene을 로드/언로드하는 기능을 제공합니다. RefCount를 관리하여 안전하게 Scene을 다룰 수 있습니다.

```csharp
// Scene 로드 (콜백 방식)
AddressableManager.Instance.LoadSceneAsync(
    "BattleScene",
    LoadSceneMode.Additive,
    (sceneInstance) => {
        Debug.Log("Battle scene loaded!");
    },
    tag: "Battle"
);

// Scene 로드 (async/await 방식)
var sceneInstance = await AddressableManager.Instance.LoadSceneTaskAsync(
    "BattleScene",
    LoadSceneMode.Additive,
    "Battle"
);

// Scene 언로드
await AddressableManager.Instance.UnloadSceneTaskAsync("BattleScene");

// Scene이 로드되어 있는지 확인
if (AddressableManager.Instance.IsSceneLoaded("BattleScene"))
{
    Debug.Log("Battle scene is loaded");
}
```

### 6. 프리로드 (Preloading) - NEW!
자주 사용되는 에셋을 미리 메모리에 로드하여 즉시 사용할 수 있도록 합니다.

```csharp
// 프리로드 (콜백 방식)
AddressableManager.Instance.PreloadAssets<Object>(
    new[] { "UI_Button", "Effect_Hit", "Sound_Click" },
    () => {
        Debug.Log("All common assets preloaded!");
    },
    tag: "Common"
);

// 프리로드 (async/await 방식)
await AddressableManager.Instance.PreloadAssetsAsync<Texture>(
    new[] { "Tex1", "Tex2" },
    "Common"
);

// 프리로드된 에셋을 RefCount 증가 없이 사용
var texture = AddressableManager.Instance.GetLoadedAsset<Texture>("Tex1");
if (texture != null)
{
    // 즉시 사용 가능, ReleaseAsset 호출 불필요
}

// 에셋이 로드되어 있는지 확인
if (AddressableManager.Instance.IsAssetLoaded("Tex1"))
{
    Debug.Log("Tex1 is loaded and ready");
}
```

### 7. 오브젝트 풀링 (Object Pooling) - NEW!
Addressables 내장 풀링 시스템을 사용하여 GameObject를 효율적으로 관리합니다.

**주의**: `InstantiateAsync`로 생성된 GameObject는 `LoadGameObject`와 달리 **수동으로 `ReleaseInstance`를 호출**해야 합니다.

```csharp
// 풀링 인스턴스 생성 (async/await)
var bullet = await AddressableManager.Instance.InstantiateTaskAsync("Bullet", transform);
bullet.transform.position = Vector3.zero;

// 사용 후 반드시 수동 해제
AddressableManager.Instance.ReleaseInstance(bullet);

// 풀 시스템 예제
private List<GameObject> _bulletPool = new List<GameObject>();

async void InitPool()
{
    for (int i = 0; i < 10; i++)
    {
        var bullet = await AddressableManager.Instance.InstantiateTaskAsync("Bullet", transform);
        bullet.SetActive(false);
        _bulletPool.Add(bullet);
    }
}

void OnDestroy()
{
    // 풀링된 객체는 수동으로 해제
    foreach (var bullet in _bulletPool)
    {
        AddressableManager.Instance.ReleaseInstance(bullet);
    }
}
```

#### LoadGameObject vs InstantiateAsync 비교

| 특징 | LoadGameObject / LoadGameObjectAsync | InstantiateAsync / InstantiateTaskAsync |
|------|--------------------------------------|----------------------------------------|
| 해제 방식 | 자동 (AddressableLifecycleLinker) | 수동 (ReleaseInstance 필요) |
| 오브젝트 풀링 | 미지원 | Addressables 내장 풀링 지원 |
| 사용 사례 | 일반적인 GameObject 생성 | 빈번한 생성/삭제 (총알, 이펙트 등) |

### 8. 진단 및 디버깅 도구 (Diagnostic Tools) - NEW!
현재 로드된 리소스 상태를 모니터링하고 디버깅할 수 있는 도구를 제공합니다.

```csharp
// RefCount 조회
int refCount = AddressableManager.Instance.GetRefCount("MyPrefab");
Debug.Log($"RefCount: {refCount}");

// 로드된 모든 에셋 키 조회
var loadedKeys = AddressableManager.Instance.GetLoadedKeys();
foreach (var key in loadedKeys)
{
    Debug.Log($"Loaded: {key}");
}

// 로드된 모든 Scene 키 조회
var loadedScenes = AddressableManager.Instance.GetLoadedSceneKeys();

// 특정 태그의 키 조회
var battleKeys = AddressableManager.Instance.GetTagKeys("Battle");

// 전체 상태 로그 출력 (디버깅용)
AddressableManager.Instance.LogStatus();
```

### 9. 리모트 다운로드 (Remote Download)
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

## 📋 API 요약 (API Summary)

### GameObject 로드
- `LoadGameObject(key, parent, onComplete, onError, tag)` - 콜백 방식
- `LoadGameObjectSync(key, parent, tag)` - 동기 방식 ⚠️
- `LoadGameObjectAsync(key, parent, tag)` - async/await 방식

### 에셋 로드
- `LoadAsset<T>(key, onComplete, onError, tag)` - 콜백 방식
- `LoadAssetSync<T>(key, tag)` - 동기 방식 ⚠️
- `LoadAssetAsync<T>(key, tag)` - async/await 방식
- `LoadAssets<T>(keys, onComplete, onError, tag)` - 배치 로드 (콜백)
- `LoadAssetsAsync<T>(keys, tag)` - 배치 로드 (async/await)
- `LoadAssetsByLabel<T>(label, onComplete, onError, tag)` - Label 기반 (콜백)
- `LoadAssetsByLabelAsync<T>(label, tag)` - Label 기반 (async/await)

### Scene 관리
- `LoadSceneAsync(key, loadMode, onComplete, onError, tag)` - Scene 로드 (콜백)
- `LoadSceneTaskAsync(key, loadMode, tag)` - Scene 로드 (async/await)
- `UnloadSceneAsync(key, onComplete, onError)` - Scene 언로드 (콜백)
- `UnloadSceneTaskAsync(key)` - Scene 언로드 (async/await)
- `IsSceneLoaded(key)` - Scene 로드 상태 확인

### 프리로드
- `PreloadAsset<T>(key, onComplete, onError, tag)` - 단일 프리로드 (콜백)
- `PreloadAssetAsync<T>(key, tag)` - 단일 프리로드 (async/await)
- `PreloadAssets<T>(keys, onComplete, onError, tag)` - 배치 프리로드 (콜백)
- `PreloadAssetsAsync<T>(keys, tag)` - 배치 프리로드 (async/await)

### 오브젝트 풀링
- `InstantiateAsync(key, parent, onComplete, onError, tag)` - 풀링 생성 (콜백) ⚠️ 수동 해제 필요
- `InstantiateTaskAsync(key, parent, tag)` - 풀링 생성 (async/await) ⚠️ 수동 해제 필요
- `ReleaseInstance(instance)` - 풀링 인스턴스 해제

### 리소스 해제
- `ReleaseAsset(key)` - RefCount 감소
- `ReleaseAllRef(key)` - 강제 전체 해제
- `ReleaseByTag(tag)` - 태그 기반 일괄 해제

### 상태 확인 및 디버깅
- `IsAssetLoaded(key)` - 에셋 로드 상태 확인
- `GetLoadedAsset<T>(key)` - 로드된 에셋 반환 (RefCount 증가 없음)
- `GetRefCount(key)` - RefCount 조회
- `GetLoadedKeys()` - 로드된 모든 에셋 키
- `GetLoadedSceneKeys()` - 로드된 모든 Scene 키
- `GetTagKeys(tag)` - 특정 태그의 키
- `LogStatus()` - 전체 상태 로그 출력

## 📝 업데이트 내역 (Release Notes)

### 2.0.0 (2026-02-08)
- **GameObject 반환 함수**: `LoadGameObjectSync`, `LoadGameObjectAsync` 추가
- **배치 로드**: `LoadAssets`, `LoadAssetsAsync`, `LoadAssetsByLabel`, `LoadAssetsByLabelAsync` 추가
- **Scene 관리**: `LoadSceneAsync`, `LoadSceneTaskAsync`, `UnloadSceneAsync`, `UnloadSceneTaskAsync` 추가
- **프리로드**: `PreloadAsset`, `PreloadAssets`, `PreloadAssetsAsync` 추가
- **상태 확인**: `IsAssetLoaded`, `IsSceneLoaded`, `GetLoadedAsset` 추가
- **오브젝트 풀링**: `InstantiateAsync`, `InstantiateTaskAsync`, `ReleaseInstance` 추가
- **진단 도구**: `GetRefCount`, `GetLoadedKeys`, `GetLoadedSceneKeys`, `GetTagKeys`, `LogStatus` 추가
- **async/await 지원**: 대부분의 메서드에 async/await 버전 추가

### 1.0.1
- **Editor Enhancements**:
  - `AutoAddressableConfig` 싱글톤 접근 지원.
  - 인스펙터 개선: GroupName, Labels 드롭다운 UI 적용.
  - Project 창 폴더 우클릭 컨텍스트 메뉴(`Add Folder to Addressable Config`) 추가.

### 1.0.0
- **AddressableManager**: 싱글톤 매니저, 레퍼런스 카운팅, `LoadAssetSync`, `ReleaseAllRef`, `ReleaseByTag`.
- **GameObject Lifecycle**: `AddressableLifecycleLinker`를 통한 자동 해제.
- **Editor Automation**: `AutoAddressableConfig` 및 `AddressableConfigMenu`를 통한 그룹/라벨 자동 설정.
- **Remote Download**: `AddressableDownloader` 유틸리티 추가.
