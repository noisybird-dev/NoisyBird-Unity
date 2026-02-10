using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NoisyBird.EditorExtension.Editor
{
    [Serializable]
    public class DefineSymbolItem
    {
        public string name;
        public string desc;
    }

    [Serializable]
    public class DefineSymbolData
    {
        public List<string> symbols = new List<string>();
    }

    [Serializable]
    public class BaseDefineSymbolData
    {
        public int selectIndex = 0;
        public List<DefineSymbolItem> defineSymbols = new List<DefineSymbolItem>();
    }

    public class DefineSymbolEditor : EditorWindow
    {
        private readonly string folderPath =
            Path.Join(Directory.GetParent(Application.dataPath).FullName, @"\DefineSymbolData");

        private BaseDefineSymbolData _baseDefines;

        private DefineSymbolData loadedDefineSymbol;

        private List<string> filePaths = new List<string>();

        private Dictionary<string, bool> modifiedSymbols = new();

        private bool isLoadData = false;

        private bool isAddMode = false;
        private string addSymbolName = "";
        private string addSymbolDesc = "";

        private void OnGUI()
        {
            if (EditorApplication.isCompiling)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    var listIcon = EditorGUIUtility.IconContent("UnityEditor.ConsoleWindow");
                    listIcon.text = "Editor 적용중...";
                    EditorGUILayout.LabelField(listIcon);
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                return;
            }
            
            if (isLoadData == false || modifiedSymbols.Count <= 0)
            {
                _baseDefines = LoadBaseDefineSymbol();
                filePaths = GetDefineFilePath();
                loadedDefineSymbol = LoadDefineSymbolData(filePaths.Count > _baseDefines.selectIndex
                    ? Path.Join(folderPath, filePaths[_baseDefines.selectIndex])
                    : "");
                isLoadData = true;
            }

            EditorGUILayout.BeginVertical();
            {
                GUILayout.Space(20f);

                GUI_RefreshButton();

                GUILayout.Space(20f);

                var listIcon = EditorGUIUtility.IconContent("UnityEditor.ConsoleWindow");
                listIcon.text = "Define Symbol 목록";
                EditorGUILayout.LabelField(listIcon);
                //Define Symbol 켜고 끄는 토글
                ToggleDefineSymbol();

                GUILayout.Space(10f);
                RefreshApplyButton();

                GUILayout.Space(20f);
                var fileIcon = EditorGUIUtility.IconContent("Folder Icon");
                fileIcon.text = "Define Symbol 파일";
                EditorGUILayout.LabelField(fileIcon);
                GUI_DefineSymbolDropDown();
                GUI_DefineSymbolButtons();
            }
            EditorGUILayout.EndVertical();
        }

        private void ToggleDefineSymbol()
        {
            var removeSymbols = new List<DefineSymbolItem>();
            GUILayout.Label("E", GUILayout.Width(20f));
            _baseDefines.defineSymbols.ForEach(x =>
            {
                EditorGUILayout.BeginHorizontal();
                {
                    bool isEditorOn = DefineSymbolManager.IsSymbolAlreadyDefined(x.name);
                    bool isOriginalOn = loadedDefineSymbol.symbols.Contains(x.name);
                    bool isCurrentOn = modifiedSymbols[x.name];

                    var editorCheck = isEditorOn
                        ? EditorGUIUtility.IconContent("TestPassed")
                        : EditorGUIUtility.IconContent("winbtn_win_close");
                    EditorGUILayout.LabelField(editorCheck, GUILayout.Width(20f));
                    var tempOn = EditorGUILayout.Toggle("", isCurrentOn, GUILayout.Width(20f));

                    GUI.contentColor = isOriginalOn != isCurrentOn ? Color.green : Color.white;
                    if (GUILayout.Button(x.name, GUILayout.Width(200f)) || tempOn != isCurrentOn)
                    {
                        modifiedSymbols[x.name] = !isCurrentOn;
                    }

                    GUI.contentColor = Color.white;
                    GUILayout.Label(x.desc);
                    if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash"), GUILayout.Width(50f)))
                    {
                        if (EditorUtility.DisplayDialog("Warning", $"{x.name} 을 정말 삭제 하시겠습니까?", "Delete", "Cancel"))
                        {
                            removeSymbols.Add(x);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            });

            if (removeSymbols.Count > 0)
            {
                removeSymbols.ForEach(x =>
                {
                    _baseDefines.defineSymbols.Remove(x);
                    modifiedSymbols.Remove(x.name);
                });
                SaveBaseDefineSymbol();
            }

            if (isAddMode)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    addSymbolName = EditorGUILayout.TextField("", addSymbolName, GUILayout.MinWidth(50f));
                    addSymbolDesc = EditorGUILayout.TextField("", addSymbolDesc, GUILayout.MinWidth(50f));
                    if (GUILayout.Button(EditorGUIUtility.IconContent("TestPassed")))
                    {
                        if (addSymbolName.IsNullOrEmpty() || addSymbolDesc.IsNullOrEmpty())
                        {
                            EditorUtility.DisplayDialog("Error", "모든 필드를 입력해주세요.", "Ok");
                        }

                        _baseDefines.defineSymbols.Add(new DefineSymbolItem()
                        {
                            name = addSymbolName,
                            desc = addSymbolDesc,
                        });
                        modifiedSymbols[addSymbolName] = false;
                        SaveBaseDefineSymbol();
                        isAddMode = false;
                    }

                    if (GUILayout.Button(EditorGUIUtility.IconContent("winbtn_win_close")))
                    {
                        isAddMode = false;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            else if (GUILayout.Button("Defile Symbol 추가"))
            {
                isAddMode = true;
                addSymbolName = "Define Symbol";
                addSymbolDesc = "Desc";
            }
        }

        private void GUI_DefineSymbolButtons()
        {
            EditorGUILayout.BeginHorizontal();
            {
                //다른이름으로 저장
                var differentSave = EditorGUIUtility.IconContent("SaveAs");
                differentSave.text = " 다른 이름으로 저장하기";
                if (GUILayout.Button(differentSave))
                {
                    var newSymbolList = GetCurrentDefineSymbols();
                    var fileName = filePaths.Count > _baseDefines.selectIndex
                        ? filePaths[_baseDefines.selectIndex].Replace(".json", "")
                        : "";
                    fileName = fileName.Replace(@"\", "");
                    var savePath = EditorUtility.SaveFilePanel("다른 이름으로 저장하기", folderPath, fileName, "json");
                    DefineSymbolData data = new DefineSymbolData
                    {
                        symbols = newSymbolList
                    };
                    if (savePath.IsNullOrEmpty() == false)
                    {
                        try
                        {
                            File.WriteAllText(savePath, JsonConvert.SerializeObject(data));
                            filePaths = GetDefineFilePath();
                            var index = filePaths.FindIndex(x =>
                            {
                                var folderPath2 = folderPath.Replace(@"\", "/");
                                var str = savePath.Replace(folderPath2, "", StringComparison.Ordinal);
                                str = str.Replace("/", "");
                                return x.Contains(str, StringComparison.Ordinal);
                            });
                            if (index >= 0)
                            {
                                _baseDefines.selectIndex = index;
                                SaveBaseDefineSymbol();
                                isLoadData = false;
                            }

                            EditorUtility.DisplayDialog("Save Data", $"{filePaths[_baseDefines.selectIndex]} 저장 성공",
                                "확인");
                        }
                        catch (Exception e)
                        {
                            EditorUtility.DisplayDialog("Error!", $"{savePath} 저장에 실패했습니다.\n{e.Message}", "Ok");
                        }
                    }
                }

                //저장
                var save = EditorGUIUtility.IconContent("SaveActive");
                save.text = " 저장하기";
                if (filePaths.Count > _baseDefines.selectIndex && GUILayout.Button(save))
                {
                    var newSymbolList = GetCurrentDefineSymbols();
                    DefineSymbolData data = new DefineSymbolData
                    {
                        symbols = newSymbolList
                    };
                    var savePath = Path.Join(folderPath, filePaths[_baseDefines.selectIndex]);
                    try
                    {
                        File.WriteAllText(savePath, JsonConvert.SerializeObject(data));
                        isLoadData = false;
                        EditorUtility.DisplayDialog("Save Data", $"{filePaths[_baseDefines.selectIndex]} 저장 성공", "확인");
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("Error!", $"{savePath} 저장에 실패했습니다.\n{e.Message}", "Ok");
                    }
                }
                
                //되돌리기
                if (GUILayout.Button("되돌리기", GUILayout.Width(150f)))
                {
                    _baseDefines.defineSymbols.ForEach(x =>
                    {
                        modifiedSymbols[x.name] = loadedDefineSymbol.symbols.Contains(x.name);
                    });
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void GUI_DefineSymbolDropDown()
        {
            int selectedIndex = EditorGUILayout.Popup(_baseDefines.selectIndex, filePaths.ToArray());
            if (selectedIndex != _baseDefines.selectIndex)
            {
                if (EditorUtility.DisplayDialog("Define Symbol Load",
                        $"{filePaths[selectedIndex]}를 로드하시겠습니까?", "Load", "Cancel"))
                {
                    var filePath = Path.Join(folderPath, filePaths[selectedIndex]);
                    _baseDefines.selectIndex = selectedIndex;
                    SaveBaseDefineSymbol();
                    loadedDefineSymbol = LoadDefineSymbolData(filePath);
                }
            }
        }

        private void GUI_RefreshButton()
        {
            EditorGUILayout.BeginHorizontal();
            {
                var refreshIcon = EditorGUIUtility.IconContent("d_Refresh");
                refreshIcon.text = "새로 고침";
                if (GUILayout.Button(refreshIcon, GUILayout.Width(150f)))
                {
                    _baseDefines = LoadBaseDefineSymbol();
                    filePaths = GetDefineFilePath();
                    loadedDefineSymbol = LoadDefineSymbolData(filePaths.Count > _baseDefines.selectIndex
                        ? Path.Join(folderPath, filePaths[_baseDefines.selectIndex])
                        : "");
                    isLoadData = true;
                }

                GUILayout.Label("Define Symbol 데이터 새로 고침");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshApplyButton()
        {
            var icon = EditorGUIUtility.IconContent("SaveAs");
            icon.text = "에디터에 적용하기";
            EditorGUILayout.LabelField(icon);
            var modifiedLists = modifiedSymbols.Keys.ToList();
            if (modifiedLists.TrueForAll(x => DefineSymbolManager.IsSymbolAlreadyDefined(x) == modifiedSymbols[x]))
            {
                GUILayout.Label("*에디터 Define Symbol 과 같습니다.", EditorStyles.largeLabel);
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                {
                    //적용 버튼
                    if (GUILayout.Button("적용하기", GUILayout.Width(150f)))
                    {
                        modifiedLists.ForEach(x =>
                        {
                            if (modifiedSymbols[x])
                            {
                                DefineSymbolManager.AddDefineSymbol(x);
                            }
                            else
                            {
                                DefineSymbolManager.RemoveDefineSymbol(x);
                            }
                        });
                    }

                    //되돌리기
                    if (GUILayout.Button("되돌리기", GUILayout.Width(150f)))
                    {
                        _baseDefines.defineSymbols.ForEach(x =>
                        {
                            modifiedSymbols[x.name] = DefineSymbolManager.IsSymbolAlreadyDefined(x.name);
                        });
                    }
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Label("*적용하기를 눌러야 적용됩니다.", EditorStyles.largeLabel);
            }
        }

        private BaseDefineSymbolData LoadBaseDefineSymbol()
        {
            string fileName = @"\BaseDefineSymbols.json";
            string filePath = Path.Join(folderPath, fileName);
            if (File.Exists(filePath))
            {
                string jsonStr = File.ReadAllText(filePath);
                var data = JsonConvert.DeserializeObject<BaseDefineSymbolData>(jsonStr);
                if (data != null)
                {
                    modifiedSymbols.Clear();
                    data.defineSymbols.ForEach(x => { modifiedSymbols.Add(x.name, false); });
                    return data;
                }
            }

            return new BaseDefineSymbolData();
        }

        private void SaveBaseDefineSymbol()
        {
            string fileName = @"\BaseDefineSymbols.json";
            string filePath = Path.Join(folderPath, fileName);
            string jsonStr = JsonConvert.SerializeObject(_baseDefines);
            File.WriteAllText(filePath, jsonStr);
        }

        private List<string> GetDefineFilePath()
        {
            var path = Directory.GetFiles(folderPath).ToList().ConvertAll(x => x.Replace(folderPath, ""));
            path.RemoveAll(x => x.Contains("BaseDefineSymbols.json", StringComparison.Ordinal));
            return path;
        }

        private DefineSymbolData LoadDefineSymbolData(string path)
        {
            if (File.Exists(path))
            {
                string jsonStr = File.ReadAllText(path);
                var data = JsonConvert.DeserializeObject<DefineSymbolData>(jsonStr);
                if (data != null)
                {
                    _baseDefines.defineSymbols.ForEach(x =>
                    {
                        modifiedSymbols[x.name] = data.symbols.Contains(x.name);
                    });
                    return data;
                }
            }
            else
            {
                isLoadData = false;
            }

            return new DefineSymbolData();
        }

        private List<string> GetCurrentDefineSymbols()
        {
            List<string> newSymbolList = new();
            var modifiedSymbolList = modifiedSymbols.Keys.ToList();
            modifiedSymbolList.ForEach(x =>
            {
                if (modifiedSymbols[x])
                {
                    newSymbolList.Add(x);
                }
            });

            return newSymbolList;
        }
    }
}