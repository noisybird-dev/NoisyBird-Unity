#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace NoisyBird.UIExtension.PresetExtension
{
    [InitializeOnLoad]
    public static class PresetExtension
    {
        private const string InjectedMarkerClass = "nb-component-injected";
        private const string CatcherClass = "nb-object-picker-catcher";
        private const string PrefShowKey = "Editor_Preset_Show";
        private const string MenuPath = "Noisy Bird/UI Extension/[On,Off] Quickly Preset";

        private static double _nextInjectAt;
        private const double DebounceSec = 0.05;

        private static readonly HashSet<int> HookedWindows = new HashSet<int>();

        private static bool IsShow
        {
            get => EditorPrefs.GetBool(PrefShowKey, true);
            set => EditorPrefs.SetBool(PrefShowKey, value);
        }

        static PresetExtension()
        {
            EditorApplication.delayCall += EnsureHookAllInspectors;

            Selection.selectionChanged += RequestReInject;
            EditorApplication.hierarchyChanged += RequestReInject;
            EditorApplication.projectChanged += RequestReInject;

            EditorApplication.update += OnUpdate;
        }

        [MenuItem(MenuPath)]
        private static void ToggleShowPresetExtension()
        {
            IsShow = !IsShow;
            Menu.SetChecked(MenuPath, IsShow);
            RequestReInject();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleShowPresetExtension_Validate()
        {
            Menu.SetChecked(MenuPath, IsShow);
            return true;
        }

        private static void RequestReInject()
        {
            if (!IsShow) return;
            _nextInjectAt = EditorApplication.timeSinceStartup + DebounceSec;
        }

        private static void OnUpdate()
        {
            if (!IsShow) return;
            if (_nextInjectAt > 0 && EditorApplication.timeSinceStartup >= _nextInjectAt)
            {
                _nextInjectAt = 0;
                EnsureHookAllInspectors();
            }
        }

        private static void EnsureHookAllInspectors()
        {
            if (!IsShow) return;

            var inspectorType = Type.GetType("UnityEditor.InspectorWindow, UnityEditor");
            if (inspectorType == null) return;

            var windows = Resources.FindObjectsOfTypeAll(inspectorType);
            foreach (var w in windows)
                TryHookWindow(w as EditorWindow);
        }

        private static void TryHookWindow(EditorWindow win)
        {
            if (win == null) return;

            int id = win.GetInstanceID();

            var root = win.rootVisualElement;
            if (root == null) return;
        
            if (HookedWindows.Add(id))
            {
                root.RegisterCallback<DetachFromPanelEvent>(_ => HookedWindows.Remove(id));
            }

            if (root.Q<IMGUIContainer>(className: CatcherClass) == null)
            {
                var catcher = new IMGUIContainer { pickingMode = PickingMode.Ignore };
                catcher.AddToClassList(CatcherClass);
                root.Add(catcher);
            }

            root.RegisterCallback<GeometryChangedEvent>(_ => RequestReInject());

            InjectPerSectionToolbar(root);
        }

        private static void InjectPerSectionToolbar(VisualElement root)
        {
            if (!IsShow || root == null) return;

            var editors = ActiveEditorTracker.sharedTracker?.activeEditors;
            if (editors == null || editors.Length == 0) return;

            var sections = root.Query<VisualElement>(className: "unity-inspector-element").ToList();
            if (sections == null || sections.Count == 0) return;

            int count = Math.Min(sections.Count, editors.Length);
            for (int i = 0; i < count; i++)
            {
                var section = sections[i];
                var editor = editors[i];
                if (section == null || editor == null) continue;

                if (editor.target is GameObject) continue;

                if (section.ClassListContains(InjectedMarkerClass)) continue;

                var toolbar = BuildToolbarFor(editor);
                var insertIndex = Math.Min(1, section.childCount);
                section.Insert(insertIndex, toolbar);

                section.AddToClassList(InjectedMarkerClass);
            }
        }

        private static VisualElement BuildToolbarFor(Editor editor)
        {
            var bar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexEnd,
                    alignItems = Align.Center,
                    marginTop = 2,
                    marginBottom = 2
                }
            };

            var quick = new Button(() => ShowPresetApplyMenu(editor.targets)) { text = "Apply ▼" };
            quick.style.height = 20; quick.style.marginRight = 4;

            var settings = new Button(() => ShowPresetOpenMenu(editor.targets)) { text = "Settings ▼" };
            settings.style.height = 20; settings.style.marginRight = 4;

            var save = new Button(() =>
                {
                    var target = editor.targets != null && editor.targets.Length > 0 ? editor.targets[0] : null;
                    if (target != null)
                    {
                        var typeName = target.GetType().Name;
                        SavePresetWithDialog(target, $"Presets/{typeName}");
                    }
                })
                { text = "Save Preset" };
            save.style.height = 20;

            string menuName = "None";
            var firstObj = editor.targets?.FirstOrDefault();
            if (firstObj != null)
            {
                var presets = EnumerateCompatiblePresets(firstObj).ToList();
                if (presets.Count > 0)
                {
                    menuName = presets[0].displayName;
                }
            }
            var setDefault = new Button(() => ShowSetDefaultMenu(editor.targets))
                { text = $"[Default] {menuName} ▼" };
            setDefault.style.height = 20;

            bar.Add(quick);
            bar.Add(settings);
            bar.Add(setDefault);
            bar.Add(save);
            return bar;
        }

        // ===== 프리셋 메뉴 =====
        private static void ShowPresetApplyMenu(Object[] targets)
        {
            var menu = new GenericMenu();
            var sample = targets?.FirstOrDefault();

            if (!sample)
            {
                menu.AddDisabledItem(new GUIContent("Select Object"));
                menu.ShowAsContext();
                return;
            }

            var presets = EnumerateCompatiblePresets(sample).ToList();
            if (presets.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Preset"));
            }
            else
            {
                foreach (var (preset, displayName) in presets)
                {
                    menu.AddItem(new GUIContent(displayName), false, () => ApplyPresetToTargets(preset, targets));
                }
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Preset Settings"), false, () => PresetSelector.ShowSelector(targets, null, true));
            menu.AddItem(new GUIContent("Preset Manager"), false,
                () => SettingsService.OpenProjectSettings("Project/Preset Manager"));

            menu.ShowAsContext();
        }

        private static void ShowPresetOpenMenu(Object[] targets)
        {
            var menu = new GenericMenu();
            var sample = targets?.FirstOrDefault();

            if (!sample)
            {
                menu.AddDisabledItem(new GUIContent("Select Object"));
                menu.ShowAsContext();
                return;
            }

            var presets = EnumerateCompatiblePresets(sample).ToList();
            if (presets.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Preset"));
            }
            else
            {
                foreach (var (preset, displayName) in presets)
                {
                    menu.AddItem(new GUIContent($"[Setting] {displayName}"), false, () => OpenLockedInspector(preset));
                }
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Preset Setting"), false, () => PresetSelector.ShowSelector(targets, null, true));
            menu.AddItem(new GUIContent("Preset Manager"), false,
                () => SettingsService.OpenProjectSettings("Project/Preset Manager"));

            menu.ShowAsContext();
        }

        private static IEnumerable<(Preset preset, string displayName)> EnumerateCompatiblePresets(Object sample)
        {
            // t:Preset 전체를 탐색하되 예외 안전하게 로드
            foreach (var guid in AssetDatabase.FindAssets("t:Preset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                Preset p = null;
                try { p = AssetDatabase.LoadAssetAtPath<Preset>(path); }
                catch { /* 무시 */ }

                if (!p) continue;
                if (!p.CanBeAppliedTo(sample)) continue;

                var name = string.IsNullOrEmpty(p.name)
                    ? Path.GetFileNameWithoutExtension(path)
                    : p.name;

                yield return (p, name);
            }
        }

        private static void ApplyPresetToTargets(Preset preset, Object[] targets)
        {
            if (!preset || targets == null || targets.Length == 0) return;

            Undo.IncrementCurrentGroup();
            var group = Undo.GetCurrentGroup();

            foreach (var t in targets)
            {
                if (!t) continue;
                if (!preset.CanBeAppliedTo(t)) continue;

                Undo.RecordObject(t, "Apply Preset");
                preset.ApplyTo(t);

                if (t is AssetImporter ai)
                    ai.SaveAndReimport();

                EditorUtility.SetDirty(t);
            }

            Undo.CollapseUndoOperations(group);
        }

        // ===== 프리셋 저장 유틸 =====
        /// <summary>
        /// 주어진 객체의 현재 설정값으로 Preset을 만들어 .preset 에셋으로 저장(대화상자).
        /// </summary>
        public static Preset SavePresetWithDialog(Object source, string defaultFolder = "Presets")
        {
            if (!source)
            {
                EditorUtility.DisplayDialog("Save Preset", "No Selected Object", "Ok");
                return null;
            }

            // 폴더 보장
            EnsureFolderExists($"Assets/{defaultFolder}");

            var typeName = source.GetType().Name;
            var defaultName = GetUniquePresetFileName(typeName, defaultFolder);

            var path = EditorUtility.SaveFilePanelInProject(
                "Save Preset",
                defaultName,
                "preset",
                "Set Folder & FileName",
                $"Assets/{defaultFolder}"
            );

            if (string.IsNullOrEmpty(path)) return null; // 취소

            return SavePresetToPath(source, path);
        }

        private static string GetUniquePresetFileName(string typeName, string defaultFolder)
        {
            var i = 0;
            while (true)
            {
                var name = i == 0 ? $"{typeName}.preset" : $"{typeName}{i}.preset";
                var abs = $"{Application.dataPath}/{defaultFolder}/{name}";
                if (!File.Exists(abs)) return name;
                i++;
            }
        }

        /// <summary>
        /// 지정 경로에 Preset 저장(기존 있으면 교체). 저장/리프레시 및 Ping.
        /// </summary>
        public static Preset SavePresetToPath(Object source, string assetPath)
        {
            if (!source || string.IsNullOrEmpty(assetPath)) return null;

            var preset = new Preset(source);

            var existing = AssetDatabase.LoadAssetAtPath<Preset>(assetPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(assetPath);

            AssetDatabase.CreateAsset(preset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(preset);
            Debug.Log($"[PresetExtension] Saved: {assetPath}");
            return preset;
        }

        private static void EnsureFolderExists(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;

            var abs = Path.Combine(Directory.GetCurrentDirectory(), assetFolder.Replace("Assets/", "Assets\\"));
            Directory.CreateDirectory(abs);
            AssetDatabase.Refresh();
        }

        // EditorUtility.OpenPropertyEditor는 해당 인스턴스를 잠그고 보여주는 별도 인스펙터
        private static void OpenLockedInspector(Preset preset)
        {
            if (!preset) return;
            EditorUtility.OpenPropertyEditor(preset);
        }
    
        private static void ShowSetDefaultMenu(Object[] targets)
        {
            var menu = new GenericMenu();
            var sample = targets?.FirstOrDefault();

            if (!sample)
            {
                menu.AddDisabledItem(new GUIContent("Select Object"));
                menu.ShowAsContext();
                return;
            }

            // 호환 프리셋 목록
            var presets = EnumerateCompatiblePresets(sample).ToList();
            if (presets.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("No Preset"));
            }
            else
            {
                foreach (var (preset, displayName) in presets)
                {
                    var list = Preset.GetDefaultPresetsForType(preset.GetPresetType()).ToList();
                    var existing = list.Exists(dp => dp.preset == preset);
                    menu.AddItem(new GUIContent($"{displayName}"), existing, () => SetAsDefaultPreset(preset));
                }
            }

            menu.ShowAsContext();
        }

        private static void SetAsDefaultPreset(Preset preset)
        {
            if (!preset) return;

            // 이 Preset이 기본 프리셋 시스템 대상인지 확인
            var pType = preset.GetPresetType(); // PresetType
            if (!pType.IsValidDefault())
            {
                EditorUtility.DisplayDialog("Set Default Preset",
                    $"Cannot Set Default Preset\n({pType.GetManagedTypeName()})",
                    "Ok");
                return;
            }

            var list = Preset.GetDefaultPresetsForType(pType).ToList();
            var existingIdx = list.FindIndex(dp => dp.preset == preset);
            if (existingIdx >= 0)
            {
                var dp = list[existingIdx];
                list.RemoveAt(existingIdx);
                list.Insert(0, dp);
            }
            else
            {
                var dp = new DefaultPreset
                {
                    preset = preset,
                    filter = string.Empty,
                    enabled = true
                };
                list.Insert(0, dp);
            }

            Preset.SetDefaultPresetsForType(pType, list.ToArray());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Preset Extension", $"기본 프리셋 지정: {preset.name} → {pType.GetManagedTypeName()}", "Ok");
        }
    }
}
#endif
