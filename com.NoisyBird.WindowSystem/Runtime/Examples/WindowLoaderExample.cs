using UnityEngine;
using NoisyBird.WindowSystem;

namespace NoisyBird.WindowSystem.Examples
{
    /// <summary>
    /// Window 로더 사용 예제입니다.
    /// 프로젝트별로 리소스 로딩 방식을 커스터마이징할 수 있습니다.
    /// </summary>
    public class WindowLoaderExample : MonoBehaviour
    {
        [Header("Window Loader Settings")]
        [Tooltip("Window Prefab이 저장된 Resources 폴더 경로")]
        [SerializeField] private string _windowResourcePath = "Windows/";

        private void Start()
        {
            // Window 로더 설정
            WindowManager.Instance.SetWindowLoader(LoadWindowFromResources);
            
            Debug.Log("[WindowLoaderExample] Window loader has been set.");
        }

        /// <summary>
        /// Resources 폴더에서 Window를 로드하는 예제 함수입니다.
        /// 프로젝트에 맞게 수정하여 사용하세요.
        /// </summary>
        /// <param name="windowId">로드할 Window ID</param>
        /// <returns>로드된 WindowBase 인스턴스, 실패 시 null</returns>
        private WindowBase LoadWindowFromResources(string windowId)
        {
            // Resources 폴더에서 Prefab 로드
            string path = _windowResourcePath + windowId;
            GameObject prefab = Resources.Load<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogError($"[WindowLoaderExample] Failed to load window prefab at path: {path}");
                return null;
            }

            // Prefab 인스턴스화
            GameObject instance = Instantiate(prefab);
            WindowBase window = instance.GetComponent<WindowBase>();

            if (window == null)
            {
                Debug.LogError($"[WindowLoaderExample] Prefab '{windowId}' does not have a WindowBase component.");
                Destroy(instance);
                return null;
            }

            // Window ID 설정 (Prefab에 설정되어 있지 않은 경우)
            if (string.IsNullOrEmpty(window.WindowId))
            {
                window.WindowId = windowId;
            }

            Debug.Log($"[WindowLoaderExample] Successfully loaded window '{windowId}' from Resources.");
            return window;
        }

        // ===== 다른 로딩 방식 예제 =====

        /// <summary>
        /// Addressables를 사용하는 예제 (비동기)
        /// 실제 사용 시 async/await 패턴 필요
        /// </summary>
        private WindowBase LoadWindowFromAddressables(string windowId)
        {
            // Addressables 사용 예제 (주석 처리)
            // var handle = Addressables.LoadAssetAsync<GameObject>(windowId);
            // await handle.Task;
            // GameObject instance = Instantiate(handle.Result);
            // return instance.GetComponent<WindowBase>();
            
            Debug.LogWarning("[WindowLoaderExample] Addressables loading is not implemented in this example.");
            return null;
        }

        /// <summary>
        /// AssetBundle을 사용하는 예제
        /// </summary>
        private WindowBase LoadWindowFromAssetBundle(string windowId)
        {
            // AssetBundle 사용 예제 (주석 처리)
            // AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
            // GameObject prefab = bundle.LoadAsset<GameObject>(windowId);
            // GameObject instance = Instantiate(prefab);
            // return instance.GetComponent<WindowBase>();
            
            Debug.LogWarning("[WindowLoaderExample] AssetBundle loading is not implemented in this example.");
            return null;
        }

        /// <summary>
        /// 커스텀 로딩 시스템 예제
        /// </summary>
        private WindowBase LoadWindowCustom(string windowId)
        {
            // 프로젝트의 커스텀 리소스 관리 시스템 사용
            // WindowBase window = MyResourceManager.LoadWindow(windowId);
            // return window;
            
            Debug.LogWarning("[WindowLoaderExample] Custom loading is not implemented in this example.");
            return null;
        }

        // ===== 테스트 메서드 =====

        /// <summary>
        /// Window 로더를 테스트합니다.
        /// </summary>
        public void TestWindowLoader()
        {
            // 등록되지 않은 Window를 열면 자동으로 로드됨
            bool success = WindowManager.Instance.OpenWindow("TestWindow");
            
            if (success)
            {
                Debug.Log("[WindowLoaderExample] Window loaded and opened successfully!");
            }
            else
            {
                Debug.LogError("[WindowLoaderExample] Failed to load and open window.");
            }
        }
    }
}
