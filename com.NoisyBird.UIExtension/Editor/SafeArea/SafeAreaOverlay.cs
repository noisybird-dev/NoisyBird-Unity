using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;
using NoisyBird.UIExtension.SafeArea;
using System.Collections.Generic;
using System;

namespace NoisyBird.UIExtension.Editor.SafeArea
{
    [Serializable]
    public class SafeAreaPreset
    {
        public string name;
        // Store as normalized ratios (0-1) instead of absolute pixels
        public float xRatio;
        public float yRatio;
        public float widthRatio;
        public float heightRatio;

        public SafeAreaPreset(string name, Rect safeArea, Vector2 screenSize)
        {
            this.name = name;
            // Convert absolute values to ratios
            this.xRatio = safeArea.x / screenSize.x;
            this.yRatio = safeArea.y / screenSize.y;
            this.widthRatio = safeArea.width / screenSize.x;
            this.heightRatio = safeArea.height / screenSize.y;
        }

        // Convert ratio back to Rect based on current screen size
        public Rect ToRect(Vector2 screenSize)
        {
            return new Rect(
                xRatio * screenSize.x,
                yRatio * screenSize.y,
                widthRatio * screenSize.x,
                heightRatio * screenSize.y
            );
        }
    }

    [Serializable]
    public class SafeAreaPresetList
    {
        public List<SafeAreaPreset> presets = new List<SafeAreaPreset>();
    }

    [Overlay(typeof(SceneView), "Custom SafeArea", true)]
    public class SafeAreaOverlay : Overlay, ITransientOverlay
    {
        private bool _isEditing = false;
        private List<SafeAreaPreset> _presets = new List<SafeAreaPreset>();
        private string _newPresetName = "New Preset";
        
        private const string PREFS_KEY = "NoisyBird_SafeArea_Presets";

        private VisualElement _root;
        private VisualElement _mainContainer;
        private VisualElement _presetContainer;
        private VisualElement _addPresetContainer;
        private VisualElement _rectFieldsContainer; // Dedicated container for dynamic fields
        private Button _toggleButton;
        private Button _resetButton;

        public bool visible => true;

        public override VisualElement CreatePanelContent()
        {
            _root = new VisualElement();
            _root.style.width = 250;
            _root.style.paddingTop = 5;
            _root.style.paddingBottom = 5;
            _root.style.paddingLeft = 5;
            _root.style.paddingRight = 5;

            // Main Toggle Button (Start/Stop Editing)
            _toggleButton = new Button(OnToggleClicked)
            {
                text = "Edit Custom SafeArea"
            };
            _root.Add(_toggleButton);

            // Container for editing UI (hidden by default)
            _mainContainer = new VisualElement();
            _mainContainer.style.display = DisplayStyle.None;
            _mainContainer.style.marginTop = 10;
            _root.Add(_mainContainer);

            // Presets Label
            var presetsLabel = new Label("Presets");
            presetsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            presetsLabel.style.marginBottom = 5;
            _mainContainer.Add(presetsLabel);

            // Preset List Container
            _presetContainer = new VisualElement();
            _mainContainer.Add(_presetContainer);

            // Add Preset Button
            var addPresetBtn = new Button(OnAddPresetClicked)
            {
                text = "Add Custom SafeArea"
            };
            addPresetBtn.style.marginTop = 10;
            _mainContainer.Add(addPresetBtn);

            // Add Preset Dialog (Hidden by default)
            _addPresetContainer = new VisualElement();
            _addPresetContainer.style.display = DisplayStyle.None;
            _addPresetContainer.style.marginTop = 10;
            _addPresetContainer.style.backgroundColor = new Color(0, 0, 0, 0.2f);
            _addPresetContainer.style.paddingTop = 5;
            _addPresetContainer.style.paddingBottom = 5;
            _addPresetContainer.style.paddingLeft = 5;
            _addPresetContainer.style.paddingRight = 5;
            _mainContainer.Add(_addPresetContainer);

            var newPresetLabel = new Label("New Preset Settings");
            newPresetLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _addPresetContainer.Add(newPresetLabel);

            var nameField = new TextField("Name");
            nameField.value = _newPresetName;
            nameField.RegisterValueChangedCallback(evt => _newPresetName = evt.newValue);
            _addPresetContainer.Add(nameField);

            // Load from Device button
            var loadFromDeviceBtn = new Button(OnLoadFromDeviceClicked)
            {
                text = "Load from Device",
                style = { marginTop = 5, marginBottom = 5 }
            };
            _addPresetContainer.Add(loadFromDeviceBtn);

            // Dynamic Rect Fields Container
            _rectFieldsContainer = new VisualElement();
            _addPresetContainer.Add(_rectFieldsContainer);

            var saveRow = new VisualElement();
            saveRow.style.flexDirection = FlexDirection.Row;
            saveRow.style.justifyContent = Justify.SpaceBetween;
            saveRow.style.marginTop = 5;
            _addPresetContainer.Add(saveRow);

            var saveBtn = new Button(() => OnSavePreset(nameField.value)) { text = "Save", style = { flexGrow = 1 } };
            var cancelBtn = new Button(OnCancelAddPreset) { text = "Cancel", style = { flexGrow = 1 } };
            saveRow.Add(saveBtn);
            saveRow.Add(cancelBtn);

            // Reset Button
            _resetButton = new Button(OnResetClicked)
            {
                text = "Reset to Screen",
                style = { marginTop = 10 }
            };
            _mainContainer.Add(_resetButton);

            // Register KeyDown callback to handle shortcuts when Overlay has focus
            _root.RegisterCallback<KeyDownEvent>(OnOverlayKeyDown);
            
            // Make root focusable so it can receive key events if clicked directly (though buttons usually take focus)
            _root.focusable = true;

            return _root;
        }

        private void OnOverlayKeyDown(KeyDownEvent evt)
        {
            if (!_isEditing) return;

            // Check for Ctrl + Shift + Number
            if (evt.shiftKey)
            {
                int index = -1;
                switch (evt.keyCode)
                {
                    case KeyCode.Alpha1: index = 0; break;
                    case KeyCode.Alpha2: index = 1; break;
                    case KeyCode.Alpha3: index = 2; break;
                    case KeyCode.Alpha4: index = 3; break;
                    case KeyCode.Alpha5: index = 4; break;
                    case KeyCode.Alpha6: index = 5; break;
                    case KeyCode.Alpha7: index = 6; break;
                    case KeyCode.Alpha8: index = 7; break;
                    case KeyCode.Alpha9: index = 8; break;
                }

                if (index >= 0 && index < _presets.Count)
                {
                    Vector2 screenSize = GetCurrentScreenSize();
                    Rect safeArea = _presets[index].ToRect(screenSize);
                    SafeAreaManager.Instance.SetSimulatedSafeArea(safeArea);
                    evt.StopPropagation();
                    
                    // Force repaint
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    SceneView.RepaintAll();
                }
            }
        }

        private Vector2 GetCurrentScreenSize()
        {
            float width = Screen.width;
            float height = Screen.height;
            
            if (!Application.isPlaying)
            {
                width = Screen.currentResolution.width;
                height = Screen.currentResolution.height;
            }
            
            return new Vector2(width, height);
        }

        private void OnToggleClicked()
        {
            _isEditing = !_isEditing;
            _toggleButton.text = _isEditing ? "Stop Editing" : "Edit Custom SafeArea";
            _mainContainer.style.display = _isEditing ? DisplayStyle.Flex : DisplayStyle.None;
            
            if (_isEditing)
            {
                SceneView.duringSceneGui += OnSceneGUI;
                LoadPresets();
                RefreshPresetList();
                
                // Initialize custom safe area if not set
                if (SafeAreaManager.Instance.SafeArea == Rect.zero || SafeAreaManager.Instance.SafeArea == Screen.safeArea)
                {
                    float width = Screen.width;
                    float height = Screen.height;
                    
                    if (!Application.isPlaying)
                    {
                        width = Screen.currentResolution.width;
                        height = Screen.currentResolution.height;
                    }
                    
                    Rect defaultRect = new Rect(width * 0.05f, height * 0.05f, width * 0.9f, height * 0.9f);
                    SafeAreaManager.Instance.SetSimulatedSafeArea(defaultRect);
                }
            }
            else
            {
                SceneView.duringSceneGui -= OnSceneGUI;
                SafeAreaManager.Instance.ClearSimulatedSafeArea();
            }
            
            SceneView.RepaintAll();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (!_isEditing) return;

            Event e = Event.current;
            // Use Ctrl + Shift + Number as requested
            if (e.type == EventType.KeyDown && e.shift)
            {
                int index = -1;
                switch (e.keyCode)
                {
                    case KeyCode.Alpha1: index = 0; break;
                    case KeyCode.Alpha2: index = 1; break;
                    case KeyCode.Alpha3: index = 2; break;
                    case KeyCode.Alpha4: index = 3; break;
                    case KeyCode.Alpha5: index = 4; break;
                    case KeyCode.Alpha6: index = 5; break;
                    case KeyCode.Alpha7: index = 6; break;
                    case KeyCode.Alpha8: index = 7; break;
                    case KeyCode.Alpha9: index = 8; break;
                }

                if (index >= 0 && index < _presets.Count)
                {
                    Vector2 screenSize = GetCurrentScreenSize();
                    Rect safeArea = _presets[index].ToRect(screenSize);
                    SafeAreaManager.Instance.SetSimulatedSafeArea(safeArea);
                    e.Use();
                    
                    // Force repaint of all views to ensure immediate visual feedback
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    SceneView.RepaintAll();
                }
            }
        }

        private void OnAddPresetClicked()
        {
            _addPresetContainer.style.display = DisplayStyle.Flex;
            _newPresetName = "New Preset " + (_presets.Count + 1);
            
            // Find the TextField and update its value
            var nameField = _addPresetContainer.Q<TextField>();
            if (nameField != null) nameField.value = _newPresetName;
            
            // Clear previous fields
            _rectFieldsContainer.Clear();
            
            Rect current = SafeAreaManager.Instance.SafeArea;
            
            var xField = new FloatField("X") { value = current.x, name = "xField" };
            var yField = new FloatField("Y") { value = current.y, name = "yField" };
            var wField = new FloatField("Width") { value = current.width, name = "wField" };
            var hField = new FloatField("Height") { value = current.height, name = "hField" };
            
            xField.RegisterValueChangedCallback(e => UpdateSafeArea(xField.value, yField.value, wField.value, hField.value));
            yField.RegisterValueChangedCallback(e => UpdateSafeArea(xField.value, yField.value, wField.value, hField.value));
            wField.RegisterValueChangedCallback(e => UpdateSafeArea(xField.value, yField.value, wField.value, hField.value));
            hField.RegisterValueChangedCallback(e => UpdateSafeArea(xField.value, yField.value, wField.value, hField.value));
            
            _rectFieldsContainer.Add(xField);
            _rectFieldsContainer.Add(yField);
            _rectFieldsContainer.Add(wField);
            _rectFieldsContainer.Add(hField);
        }

        private void OnLoadFromDeviceClicked()
        {
            GenericMenu menu = new GenericMenu();
            
            // iPhone 14 Series
            menu.AddItem(new GUIContent("iPhone/iPhone 14 Pro/Portrait"), false, () => 
                LoadDeviceSafeArea(1179, 2556, new Rect(0, 102, 1179, 2352)));
            menu.AddItem(new GUIContent("iPhone/iPhone 14 Pro/Landscape"), false, () => 
                LoadDeviceSafeArea(2556, 1179, new Rect(102, 0, 2352, 1179)));
            
            menu.AddItem(new GUIContent("iPhone/iPhone 14 Pro Max/Portrait"), false, () => 
                LoadDeviceSafeArea(1290, 2796, new Rect(0, 102, 1290, 2592)));
            menu.AddItem(new GUIContent("iPhone/iPhone 14 Pro Max/Landscape"), false, () => 
                LoadDeviceSafeArea(2796, 1290, new Rect(102, 0, 2592, 1290)));
            
            // iPhone 15 Series
            menu.AddItem(new GUIContent("iPhone/iPhone 15 Pro/Portrait"), false, () => 
                LoadDeviceSafeArea(1179, 2556, new Rect(0, 102, 1179, 2352)));
            menu.AddItem(new GUIContent("iPhone/iPhone 15 Pro/Landscape"), false, () => 
                LoadDeviceSafeArea(2556, 1179, new Rect(102, 0, 2352, 1179)));
            
            menu.AddItem(new GUIContent("iPhone/iPhone 15 Pro Max/Portrait"), false, () => 
                LoadDeviceSafeArea(1290, 2796, new Rect(0, 102, 1290, 2592)));
            menu.AddItem(new GUIContent("iPhone/iPhone 15 Pro Max/Landscape"), false, () => 
                LoadDeviceSafeArea(2796, 1290, new Rect(102, 0, 2592, 1290)));
            
            // iPhone 13 Series
            menu.AddItem(new GUIContent("iPhone/iPhone 13 Pro/Portrait"), false, () => 
                LoadDeviceSafeArea(1170, 2532, new Rect(0, 102, 1170, 2328)));
            menu.AddItem(new GUIContent("iPhone/iPhone 13 Pro/Landscape"), false, () => 
                LoadDeviceSafeArea(2532, 1170, new Rect(102, 0, 2328, 1170)));
            
            menu.AddItem(new GUIContent("iPhone/iPhone 13 Pro Max/Portrait"), false, () => 
                LoadDeviceSafeArea(1284, 2778, new Rect(0, 102, 1284, 2574)));
            menu.AddItem(new GUIContent("iPhone/iPhone 13 Pro Max/Landscape"), false, () => 
                LoadDeviceSafeArea(2778, 1284, new Rect(102, 0, 2574, 1284)));
            
            // iPhone 12 Series
            menu.AddItem(new GUIContent("iPhone/iPhone 12 Pro/Portrait"), false, () => 
                LoadDeviceSafeArea(1170, 2532, new Rect(0, 102, 1170, 2328)));
            menu.AddItem(new GUIContent("iPhone/iPhone 12 Pro/Landscape"), false, () => 
                LoadDeviceSafeArea(2532, 1170, new Rect(102, 0, 2328, 1170)));
            
            menu.AddItem(new GUIContent("iPhone/iPhone 12 Pro Max/Portrait"), false, () => 
                LoadDeviceSafeArea(1284, 2778, new Rect(0, 102, 1284, 2574)));
            menu.AddItem(new GUIContent("iPhone/iPhone 12 Pro Max/Landscape"), false, () => 
                LoadDeviceSafeArea(2778, 1284, new Rect(102, 0, 2574, 1284)));
            
            // iPhone SE & 8 (No notch)
            menu.AddItem(new GUIContent("iPhone/iPhone SE (3rd gen)/Portrait"), false, () => 
                LoadDeviceSafeArea(750, 1334, new Rect(0, 0, 750, 1334)));
            menu.AddItem(new GUIContent("iPhone/iPhone SE (3rd gen)/Landscape"), false, () => 
                LoadDeviceSafeArea(1334, 750, new Rect(0, 0, 1334, 750)));
            
            menu.AddItem(new GUIContent("iPhone/iPhone 8/Portrait"), false, () => 
                LoadDeviceSafeArea(750, 1334, new Rect(0, 0, 750, 1334)));
            menu.AddItem(new GUIContent("iPhone/iPhone 8/Landscape"), false, () => 
                LoadDeviceSafeArea(1334, 750, new Rect(0, 0, 1334, 750)));
            
            // iPad Pro 12.9"
            menu.AddItem(new GUIContent("iPad/iPad Pro 12.9\"/Portrait"), false, () => 
                LoadDeviceSafeArea(2048, 2732, new Rect(0, 0, 2048, 2732)));
            menu.AddItem(new GUIContent("iPad/iPad Pro 12.9\"/Landscape"), false, () => 
                LoadDeviceSafeArea(2732, 2048, new Rect(0, 0, 2732, 2048)));
            
            // iPad Pro 11"
            menu.AddItem(new GUIContent("iPad/iPad Pro 11\"/Portrait"), false, () => 
                LoadDeviceSafeArea(1668, 2388, new Rect(0, 0, 1668, 2388)));
            menu.AddItem(new GUIContent("iPad/iPad Pro 11\"/Landscape"), false, () => 
                LoadDeviceSafeArea(2388, 1668, new Rect(0, 0, 2388, 1668)));
            
            // iPad Air
            menu.AddItem(new GUIContent("iPad/iPad Air/Portrait"), false, () => 
                LoadDeviceSafeArea(1640, 2360, new Rect(0, 0, 1640, 2360)));
            menu.AddItem(new GUIContent("iPad/iPad Air/Landscape"), false, () => 
                LoadDeviceSafeArea(2360, 1640, new Rect(0, 0, 2360, 1640)));
            
            // Samsung Galaxy Z Fold (Unfolded)
            menu.AddItem(new GUIContent("Samsung/Galaxy Z Fold 5/Unfolded Portrait"), false, () => 
                LoadDeviceSafeArea(1812, 2176, new Rect(0, 0, 1812, 2176)));
            menu.AddItem(new GUIContent("Samsung/Galaxy Z Fold 5/Unfolded Landscape"), false, () => 
                LoadDeviceSafeArea(2176, 1812, new Rect(0, 0, 2176, 1812)));
            
            // Samsung Galaxy Z Fold (Folded - Cover Screen)
            menu.AddItem(new GUIContent("Samsung/Galaxy Z Fold 5/Folded Portrait"), false, () => 
                LoadDeviceSafeArea(904, 2176, new Rect(0, 0, 904, 2176)));
            menu.AddItem(new GUIContent("Samsung/Galaxy Z Fold 5/Folded Landscape"), false, () => 
                LoadDeviceSafeArea(2176, 904, new Rect(0, 0, 2176, 904)));
            
            // Samsung Galaxy Z Flip (Unfolded)
            menu.AddItem(new GUIContent("Samsung/Galaxy Z Flip 5/Unfolded Portrait"), false, () => 
                LoadDeviceSafeArea(1080, 2640, new Rect(0, 0, 1080, 2640)));
            menu.AddItem(new GUIContent("Samsung/Galaxy Z Flip 5/Unfolded Landscape"), false, () => 
                LoadDeviceSafeArea(2640, 1080, new Rect(0, 0, 2640, 1080)));
            
            // Samsung Galaxy Z Flip (Folded - Cover Screen)
            menu.AddItem(new GUIContent("Samsung/Galaxy Z Flip 5/Cover Screen Portrait"), false, () => 
                LoadDeviceSafeArea(720, 748, new Rect(0, 0, 720, 748)));
            menu.AddItem(new GUIContent("Samsung/Galaxy Z Flip 5/Cover Screen Landscape"), false, () => 
                LoadDeviceSafeArea(748, 720, new Rect(0, 0, 748, 720)));
            
            // Current Screen.safeArea option
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Current Screen.safeArea"), false, () => 
                LoadDeviceSafeArea(Screen.width, Screen.height, Screen.safeArea));
            
            menu.ShowAsContext();
        }

        private void LoadDeviceSafeArea(float deviceWidth, float deviceHeight, Rect deviceSafeArea)
        {
            // Get current screen size
            Vector2 currentScreenSize = GetCurrentScreenSize();
            
            // Convert device SafeArea to ratio
            float xRatio = deviceSafeArea.x / deviceWidth;
            float yRatio = deviceSafeArea.y / deviceHeight;
            float widthRatio = deviceSafeArea.width / deviceWidth;
            float heightRatio = deviceSafeArea.height / deviceHeight;
            
            // Apply ratio to current screen size
            Rect scaledSafeArea = new Rect(
                xRatio * currentScreenSize.x,
                yRatio * currentScreenSize.y,
                widthRatio * currentScreenSize.x,
                heightRatio * currentScreenSize.y
            );
            
            // Update fields
            var xField = _rectFieldsContainer.Q<FloatField>("xField");
            var yField = _rectFieldsContainer.Q<FloatField>("yField");
            var wField = _rectFieldsContainer.Q<FloatField>("wField");
            var hField = _rectFieldsContainer.Q<FloatField>("hField");
            
            if (xField != null) xField.value = scaledSafeArea.x;
            if (yField != null) yField.value = scaledSafeArea.y;
            if (wField != null) wField.value = scaledSafeArea.width;
            if (hField != null) hField.value = scaledSafeArea.height;
            
            // Update Manager
            UpdateSafeArea(scaledSafeArea.x, scaledSafeArea.y, scaledSafeArea.width, scaledSafeArea.height);
        }

        private void UpdateSafeArea(float x, float y, float w, float h)
        {
            SafeAreaManager.Instance.SetSimulatedSafeArea(new Rect(x, y, w, h));
        }

        private void OnSavePreset(string name)
        {
            Vector2 screenSize = GetCurrentScreenSize();
            _presets.Add(new SafeAreaPreset(name, SafeAreaManager.Instance.SafeArea, screenSize));
            SavePresets();
            _addPresetContainer.style.display = DisplayStyle.None;
            RefreshPresetList();
        }

        private void OnCancelAddPreset()
        {
            _addPresetContainer.style.display = DisplayStyle.None;
        }

        private void OnResetClicked()
        {
            SafeAreaManager.Instance.ClearSimulatedSafeArea();
            _isEditing = false;
            _toggleButton.text = "Edit Custom SafeArea";
            _mainContainer.style.display = DisplayStyle.None;
            SceneView.RepaintAll();
        }

        private void RefreshPresetList()
        {
            _presetContainer.Clear();
            
            foreach (var preset in _presets)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.marginBottom = 2;
                
                var applyBtn = new Button(() => {
                    Vector2 screenSize = GetCurrentScreenSize();
                    Rect safeArea = preset.ToRect(screenSize);
                    SafeAreaManager.Instance.SetSimulatedSafeArea(safeArea);
                })
                {
                    text = preset.name,
                    style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft},
                };
                
                var deleteBtn = new Button(() => {
                    _presets.Remove(preset);
                    SavePresets();
                    RefreshPresetList();
                })
                {
                    text = "X",
                    style = { width = 25 }
                };
                
                row.Add(applyBtn);
                row.Add(deleteBtn);
                _presetContainer.Add(row);
            }
        }

        private void LoadPresets()
        {
            string json = EditorPrefs.GetString(PREFS_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    SafeAreaPresetList list = JsonUtility.FromJson<SafeAreaPresetList>(json);
                    _presets = list.presets;
                }
                catch
                {
                    _presets = new List<SafeAreaPreset>();
                }
            }
            else
            {
                _presets = new List<SafeAreaPreset>();
            }
        }

        private void SavePresets()
        {
            SafeAreaPresetList list = new SafeAreaPresetList { presets = _presets };
            string json = JsonUtility.ToJson(list);
            EditorPrefs.SetString(PREFS_KEY, json);
        }
    }
}
