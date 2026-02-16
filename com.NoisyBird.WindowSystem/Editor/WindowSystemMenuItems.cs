using UnityEngine;
using UnityEditor;

namespace NoisyBird.WindowSystem.Editor
{
    /// <summary>
    /// Window System 관련 메뉴 아이템을 제공합니다.
    /// </summary>
    public static class WindowSystemMenuItems
    {
        private const string MENU_ROOT = "GameObject/Noisy Bird/Window System/";

        [MenuItem(MENU_ROOT + "Create Window Manager with Containers", false, 0)]
        private static void CreateWindowManagerWithContainers()
        {
            // 이미 존재하는지 확인
            WindowManager existing = Object.FindObjectOfType<WindowManager>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Window Manager Exists",
                    "WindowManager already exists in the scene.", "OK");
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }

            // WindowManager GameObject 생성 (Awake에서 자동으로 Canvas와 Container 생성)
            GameObject go = new GameObject("WindowManager");
            WindowManager windowManager = go.AddComponent<WindowManager>();

            Undo.RegisterCreatedObjectUndo(go, "Create Window Manager with Containers");
            Selection.activeGameObject = go;

            Debug.Log("[WindowSystem] WindowManager with Containers created.");
        }

        [MenuItem(MENU_ROOT + "Create Window Manager (Legacy)", false, 1)]
        private static void CreateWindowManager()
        {
            // 이미 존재하는지 확인
            WindowManager existing = Object.FindObjectOfType<WindowManager>();
            if (existing != null)
            {
                EditorUtility.DisplayDialog("Window Manager Exists",
                    "WindowManager already exists in the scene.", "OK");
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }

            // 새로 생성
            GameObject go = new GameObject("WindowManager");
            go.AddComponent<WindowManager>();

            Undo.RegisterCreatedObjectUndo(go, "Create Window Manager");
            Selection.activeGameObject = go;

            Debug.Log("[WindowSystem] WindowManager created.");
        }

        [MenuItem(MENU_ROOT + "Create Empty Window", false, 11)]
        private static void CreateEmptyWindow()
        {
            GameObject go = new GameObject("NewWindow");
            var window = go.AddComponent<EmptyWindow>();
            window.WindowId = "NewWindow";
            
            Undo.RegisterCreatedObjectUndo(go, "Create Empty Window");
            Selection.activeGameObject = go;
            
            Debug.Log("[WindowSystem] Empty Window created. Please customize it.");
        }

        [MenuItem(MENU_ROOT + "Create Canvas with Window Root", false, 12)]
        private static void CreateCanvasWithWindowRoot()
        {
            // Canvas 생성
            GameObject canvasGo = new GameObject("WindowCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Window Root 생성
            GameObject rootGo = new GameObject("WindowRoot");
            rootGo.transform.SetParent(canvasGo.transform, false);
            
            RectTransform rootRect = rootGo.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;
            rootRect.anchoredPosition = Vector2.zero;

            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas with Window Root");
            Selection.activeGameObject = rootGo;
            
            Debug.Log("[WindowSystem] Canvas with Window Root created.");
        }

        [MenuItem("Noisy Bird/Window System/Documentation", false, 100)]
        private static void OpenDocumentation()
        {
            string readmePath = "Assets/NoisyBird-Unity/com.NoisyBird.WindowSystem/README.md";
            var readme = AssetDatabase.LoadAssetAtPath<TextAsset>(readmePath);
            
            if (readme != null)
            {
                Selection.activeObject = readme;
                EditorGUIUtility.PingObject(readme);
            }
            else
            {
                EditorUtility.DisplayDialog("Documentation", 
                    "README.md not found at: " + readmePath, "OK");
            }
        }

        [MenuItem("Noisy Bird/Window System/Open Window Manager", false, 101)]
        private static void OpenWindowManagerWindow()
        {
            WindowManagerEditorWindow.ShowWindow();
        }
    }

    /// <summary>
    /// 빈 Window 템플릿입니다. 사용자가 커스터마이징할 수 있습니다.
    /// </summary>
    public class EmptyWindow : WindowBase
    {
        public override WindowState CaptureState()
        {
            // TODO: 상태 저장 로직 구현
            return null;
        }

        public override void RestoreState(WindowState state)
        {
            // TODO: 상태 복구 로직 구현
        }

        private void Start()
        {
            // Window를 WindowManager에 등록
            WindowManager.Instance.RegisterWindow(this);
        }

        private void OnDestroy()
        {
            // Window를 WindowManager에서 등록 해제
            if (WindowManager.Instance != null)
            {
                WindowManager.Instance.UnregisterWindow(WindowId);
            }
        }
    }
}
