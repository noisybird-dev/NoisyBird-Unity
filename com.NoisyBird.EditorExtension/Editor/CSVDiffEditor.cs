using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace NoisyBird.EditorExtension.Editor
{
    public enum CSVCellState
    {
        None,
        Diff,
        Add,
        Remove,
    }

    public class CSVCellData
    {
        public string columnKey;
        public string rowKey;
        public string content;
    }

    public class CsvDiffEditor : EditorWindow
    {
        protected Vector2 scroll;

        public virtual void LoadData()
        {
        }

        protected void DrawTable(List<List<string>> table)
        {
            if (table == null || table.Count <= 0)
            {
                return;
            }

            Dictionary<string, float> keyMaxSize = new();
            for (var i = 0; i < table.Count; i++)
            {
                var row = table[i];
                if (i == 0)
                {
                    for (var j = 0; j < row.Count; j++)
                    {
                        GUIStyle style = EditorStyles.label;
                        style.richText = true;
                        Vector2 size = style.CalcSize(new GUIContent(row[j]));
                        float width = Mathf.Max(size.x + 20f, 50f);
                        if (keyMaxSize.ContainsKey(row[j]))
                        {
                            keyMaxSize[row[j]] = width;
                        }
                        else
                        {
                            keyMaxSize.Add(row[j], width);
                        }
                    }
                }
                else
                {
                    for (var j = 0; j < row.Count; j++)
                    {
                        GUIStyle style = EditorStyles.label;
                        style.richText = true;
                        Vector2 size = style.CalcSize(new GUIContent(row[j]));
                        float newWidth = Mathf.Max(size.x + 20f, 50f);
                        if (keyMaxSize.TryGetValue(table[0][j], out var width))
                        {
                            if (newWidth > width)
                                keyMaxSize[table[0][j]] = newWidth;
                        }
                        else
                        {
                            keyMaxSize.Add(table[0][j], newWidth);
                        }
                    }
                }
            }

            for (var i = 0; i < table.Count; i++)
            {
                var row = table[i];
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(50f);
                for (var j = 0; j < row.Count; j++)
                {
                    DrawCell(row[j], keyMaxSize[table[0][j]], i == 0);
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        protected List<List<string>> CompareCsv(string originalStr, string updateStr, out bool isChanged,
            bool onlyChangedRow = false)
        {
            isChanged = false;
            List<List<string>> result = new();
            var cellDataA = ConvertToCellData(originalStr, out List<string> rowKeysA, out List<string> colKeysA);
            var cellDataB = ConvertToCellData(updateStr, out List<string> rowKeysB, out List<string> colKeysB);

            var titleCombine = CombineTitle(colKeysA, colKeysB);
            var rowKeysCombine = CombineTitle(rowKeysA, rowKeysB);

            result.Add(titleCombine.ConvertAll(x => $"<b>{x}</b>"));
            for (var i = 0; i < rowKeysCombine.Count; i++)
            {
                List<string> line = new();
                var rowKey = rowKeysCombine[i];
                var cellAList = cellDataA.FindAll(x => x.rowKey.Equals(rowKey));
                var cellBList = cellDataB.FindAll(x => x.rowKey.Equals(rowKey));
                bool isRowChanged = false;
                for (var j = 0; j < titleCombine.Count; j++)
                {
                    var colKey = titleCombine[j];
                    var cellA = cellAList.Find(x => x.columnKey.Equals(colKey) && x.rowKey.Equals(rowKey));
                    var cellB = cellBList.Find(x => x.columnKey.Equals(colKey) && x.rowKey.Equals(rowKey));

                    string content;
                    if (cellA == null && cellB == null) content = "";
                    else if (cellA == null) content = $"<color=#33FF33>[Add] {cellB.content}</color>";
                    else if (cellB == null) content = $"<color=#FF3333>[Remove] {cellA.content}</color>";
                    else if (cellA.content.Equals(cellB.content)) content = cellA.content;
                    else content = $"<color=#FF3333>{cellA.content}</color> => <color=#33FF33>{cellB.content}</color>";
                    line.Add(content);

                    if (cellA == null || cellB == null || cellA.content.Equals(cellB.content) == false)
                    {
                        isChanged = true;
                        isRowChanged = true;
                    }
                }

                if (onlyChangedRow == false || isRowChanged)
                {
                    result.Add(line);
                }
            }

            if (onlyChangedRow && isChanged == false)
            {
                return new List<List<string>>();
            }

            return result;
        }

        protected void DrawCell(string text, float width, bool isTitle = false)
        {
            float space = 10f;
            Rect rect = GUILayoutUtility.GetRect(width + space, 24, GUILayout.Width(width + space));
            GUIStyle style = new(isTitle ? EditorStyles.miniButton : EditorStyles.textField)
            {
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white },
                richText = true,
            };
            GUI.Label(rect, text, style);
        }

        protected List<CSVCellData> ConvertToCellData(string str, out List<string> rowKeys, out List<string> colKeys)
        {
            rowKeys = new List<string>();
            List<CSVCellData> cellData = new();
            var lines = str.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
            lines.RemoveAll(x => x.IsNullOrEmpty());
            colKeys = lines.Count > 0 ? lines[0].Split(',').ToList() : new List<string>();

            foreach (var title in colKeys)
            {
                cellData.Add(new CSVCellData
                {
                    columnKey = title,
                    rowKey = "#title",
                    content = title,
                });
            }

            for (var i = 2; i < lines.Count; i++)
            {
                var line = lines[i];
                var cells = line.Split(',').ToList();
                var rowKey = cells[0];
                if (rowKey.IsNullOrEmpty())
                {
                    continue;
                }

                rowKeys.Add(rowKey);

                for (var j = 0; j < cells.Count; j++)
                {
                    cellData.Add(new CSVCellData
                    {
                        columnKey = colKeys[j],
                        rowKey = rowKey,
                        content = cells[j],
                    });
                }
            }

            return cellData;
        }

        protected List<string> CombineTitle(List<string> titleA, List<string> titleB)
        {
            HashSet<string> combineHash = new HashSet<string>();
            combineHash.AddRange(titleA);
            combineHash.AddRange(titleB);

            return combineHash.ToList();

            if (titleA.Count <= 0) return titleB;
            if (titleB.Count <= 0) return titleA;

            int maxLength = titleA.Count + titleB.FindAll(x => titleA.Contains(x) == false).Count;
            List<string> combine = new();
            int aIndex = 0;
            int bIndex = 0;
            for (var i = 0; i < maxLength; i++)
            {
                var aKey = aIndex < titleA.Count ? titleA[aIndex] : "";
                var bKey = bIndex < titleB.Count ? titleB[bIndex] : "";

                if (aKey.Equals(bKey) && bKey.IsNullOrEmpty() == false)
                {
                    combine.Add(aKey);
                    aIndex++;
                    bIndex++;
                }
                else if (bIndex < titleB.Count)
                {
                    combine.Add(bKey);
                    bIndex++;
                }
                else if (aIndex < titleA.Count)
                {
                    combine.Add(aKey);
                    aIndex++;
                }
            }

            return combine;
        }
    }
}