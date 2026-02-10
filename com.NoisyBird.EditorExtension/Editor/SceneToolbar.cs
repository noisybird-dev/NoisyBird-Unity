#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NoisyBird.EditorExtension.Editor
{
    [InitializeOnLoad]
    public class SceneToolbar
{
    private static readonly string customScene = "[추가 or 삭제]";
    private static readonly string selectedScenePrefsName = "SceneToolbar_SelectedSceneIndex";
    private static readonly string customScenePrefsName = "SceneToolbar_CustomSceneName";
    private static string[] scenePaths;
    private static string[] sceneNames;
    private static int firstSceneIndex;
    private static readonly int pickerControlID = 10001;
    private static SceneAsset lastPickScene;
    private static bool isLoaded = false;

    static SceneToolbar()
    {
        ToolbarExtender.LeftToolbarGUI.Add(OnLeftGUI);
        ToolbarExtender.RightToolbarGUI.Add(OnRightGUI);
        EditorApplication.playModeStateChanged += StartPlayModeAfterSceneLoad;
    }

    static void LoadFromBuildSettingsScene()
    {
        if (isLoaded) return;
        var scenes = EditorBuildSettings.scenes;
        var customs = EditorPrefs.GetString(customScenePrefsName, "").Split("@").ToList();
        customs.RemoveAll(x => x.IsNullOrEmpty());
        var customScenes = customs.Count <= 0
            ? new List<(string, string)>()
            : customs.ConvertAll(x =>
            {
                var sceneSplit = x.Split('#');
                return (sceneSplit[0], sceneSplit[1]);
            });
        int length = scenes.Length + customScenes.Count + 1;

        if (scenes == null) return;
        bool isDiffScene = sceneNames == null || length != sceneNames.Length;
        bool isDiffEnable = scenes.Length <= firstSceneIndex || scenes[firstSceneIndex].enabled == false;
        if (isDiffScene == false && isDiffEnable == false)
        {
            return;
        }

        scenePaths = new string[length];
        sceneNames = new string[length];
        firstSceneIndex = 0;

        for (int i = 0; i < scenes.Length; i++)
        {
            scenePaths[i] = scenes[i].path;
            sceneNames[i] = System.IO.Path.GetFileNameWithoutExtension(scenes[i].path);
            if (firstSceneIndex == 0 && scenes[i].enabled) firstSceneIndex = i;
        }

        for (int i = scenes.Length; i < scenes.Length + customScenes.Count; i++)
        {
            scenePaths[i] = customScenes[i - scenes.Length].Item2;
            sceneNames[i] = $"{customScenes[i - scenes.Length].Item1}";
        }

        scenePaths[^1] = customScenePrefsName;
        sceneNames[^1] = customScene;
        isLoaded = true;
    }

    static void OnLeftGUI()
    {
        if (EditorApplication.isPlaying)
        {
            return;
        }

        LoadFromBuildSettingsScene();
        GUILayout.FlexibleSpace();
        DrawSceneDropDown();
        DrawPlayButton();
    }

    static void OnRightGUI()
    {
        if (EditorApplication.isPlaying)
        {
            return;
        }
    }

    static void DrawPlayButton()
    {
        if (GUILayout.Button($"▶{sceneNames[firstSceneIndex]}", GUILayout.Width(100)))
        {
            EditorApplication.isPlaying = true;
        }
    }

    static void StartPlayModeAfterSceneLoad(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode)
        {
            return;
        }

        if (SceneManager.GetActiveScene().name == "TitleScene")
        {
            return;
        }

        SceneManager.LoadScene("TitleScene");
    }

    static void DrawSceneDropDown()
    {
        int selectedIndex = sceneNames.ToList().FindIndex(x => x.Contains(SceneManager.GetActiveScene().name));
        int newSelected = EditorGUILayout.Popup(selectedIndex, sceneNames, GUILayout.Width(100));
        if (newSelected == sceneNames.Length - 1)
        {
            EditorGUIUtility.ShowObjectPicker<SceneAsset>(null, false, "", pickerControlID);
        }
        else if (newSelected != selectedIndex)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePaths[newSelected]);
            }
        }

        if (Event.current.commandName == "ObjectSelectorClosed" &&
            EditorGUIUtility.GetObjectPickerControlID() == pickerControlID)
        {
            var scene = EditorGUIUtility.GetObjectPickerObject() as SceneAsset;
            if (scene != null && lastPickScene != scene)
            {
                lastPickScene = scene;
                AddOrRemoveSceneName(scene.name, scene.GetPath());
                isLoaded = false;
            }
            else
            {
                lastPickScene = null;
            }
        }
    }

    private static void AddOrRemoveSceneName(string sceneName, string scenePath)
    {
        string customSceneNames = EditorPrefs.GetString(customScenePrefsName, "");
        var splits = customSceneNames.Split('@').ToList();
        splits.RemoveAll(x => x.IsNullOrEmpty());
        var sceneNamePath = splits.Count > 0
            ? splits.ConvertAll(x =>
            {
                var sceneSplit = x.Split('#');
                return (sceneSplit[0], sceneSplit[1]);
            })
            : new List<(string, string)>();
        if (sceneNamePath.Count > 0 &&
            sceneNamePath.Exists(x => x.Item1.Equals(sceneName) && x.Item2.Equals(scenePath)))
        {
            sceneNamePath.RemoveAll(x => x.Item1.Equals(sceneName) && x.Item2.Equals(scenePath));
        }
        else
        {
            sceneNamePath.Add((sceneName, scenePath));
        }

        EditorPrefs.SetString(customScenePrefsName,
            string.Join("@", sceneNamePath.ConvertAll(x => $"{x.Item1}#{x.Item2}")));
    }
    }
}
#endif