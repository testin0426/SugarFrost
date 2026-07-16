using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Counts Chinese text inside .nani files in a chosen folder.
/// It strips Naninovel command lines (lines starting with @) before counting.
/// </summary>
public class NaniWordCounter : EditorWindow
{
    private string _selectedFolder = "Assets/Scenario";
    private readonly List<string> _selectedFiles = new List<string>();
    private string _status = "Choose a folder or drag .nani files here, then click Count.";
    private int _fileCount;
    private int _totalChineseChars;
    private int _totalLines;
    private Vector2 _scroll;

    [MenuItem("Tools/Nani Word Counter")]
    private static void Open()
    {
        var window = GetWindow<NaniWordCounter>("Nani Word Counter");
        window.minSize = new Vector2(420, 260);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Count Chinese text in .nani files", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("You can count a whole folder or select specific .nani files. Drag files from the Project window into the box below.", MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Folder", GUILayout.Width(70));
        EditorGUILayout.TextField(_selectedFolder, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Select…", GUILayout.Width(80))) PickFolder();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Selected .nani files", EditorStyles.miniBoldLabel);
        Rect dropArea = GUILayoutUtility.GetRect(0f, 60f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag .nani files here\n(or click 'Add selected files')");
        switch (Event.current.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (dropArea.Contains(Event.current.mousePosition))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    if (Event.current.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        AddDroppedFiles();
                        Event.current.Use();
                    }
                }
                break;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add selected files", GUILayout.Height(28))) AddSelectedProjectFiles();
        if (GUILayout.Button("Clear files", GUILayout.Height(28), GUILayout.Width(100))) _selectedFiles.Clear();
        EditorGUILayout.EndHorizontal();

        if (_selectedFiles.Count > 0)
        {
            EditorGUILayout.LabelField("Current file list:");
            foreach (var path in _selectedFiles)
                EditorGUILayout.SelectableLabel(path, EditorStyles.textField, GUILayout.Height(18));
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Count selected files/folder", GUILayout.Height(30))) RunCount();
        if (GUILayout.Button("Clear", GUILayout.Height(30), GUILayout.Width(70))) ResetResult();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
        EditorGUILayout.HelpBox(_status, MessageType.None);

        if (_fileCount > 0)
        {
            EditorGUILayout.LabelField($"Files scanned: {_fileCount}");
            EditorGUILayout.LabelField($"Chinese characters counted: {_totalChineseChars}");
            EditorGUILayout.LabelField($"Text lines scanned: {_totalLines}");
        }

        EditorGUILayout.EndScrollView();
    }

    private void PickFolder()
    {
        string defaultPath = Application.dataPath;
        string picked = EditorUtility.OpenFolderPanel("Select folder to count .nani files", defaultPath, "");
        if (string.IsNullOrWhiteSpace(picked)) return;

        string rel = ToAssetsRelative(picked);
        if (string.IsNullOrEmpty(rel))
        {
            _status = "Please choose a folder under Assets/";
            return;
        }

        _selectedFolder = rel;
        _status = $"Folder selected: {_selectedFolder}";
    }

    private void RunCount()
    {
        var files = GetFilesToCount();
        if (files.Count == 0)
        {
            _status = "No .nani files selected. Choose a folder or drag/select specific .nani files.";
            ResetResult();
            return;
        }

        int fileCount = 0;
        int totalChineseChars = 0;
        int totalLines = 0;

        try
        {
            EditorUtility.DisplayProgressBar("Counting .nani text", "Scanning files...", 0f);

            for (int i = 0; i < files.Count; i++)
            {
                string path = files[i];
                string relativePath = ToAssetsRelative(path);
                EditorUtility.DisplayProgressBar("Counting .nani text", relativePath, (float)i / files.Count);

                string text = File.ReadAllText(path);
                string cleaned = RemoveNaniCommands(text);
                totalChineseChars += CountChineseCharacters(cleaned);
                totalLines += CountNonEmptyLines(cleaned);
                fileCount++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        _fileCount = fileCount;
        _totalChineseChars = totalChineseChars;
        _totalLines = totalLines;
        _status = $"Finished. Scanned {fileCount} .nani file(s).\nChinese characters counted: {totalChineseChars}.";

        Debug.Log($"[NaniWordCounter] Counted {totalChineseChars} Chinese characters in {fileCount} .nani file(s). Source: {_selectedFolder}, explicit files: {_selectedFiles.Count}");
    }

    private List<string> GetFilesToCount()
    {
        var files = new List<string>();

        if (_selectedFiles.Count > 0)
        {
            foreach (var item in _selectedFiles)
            {
                string full = ToFullPath(item);
                if (File.Exists(full) && full.EndsWith(".nani", StringComparison.OrdinalIgnoreCase))
                    files.Add(full);
            }
        }

        if (files.Count == 0)
        {
            string fullFolder = ToFullPath(_selectedFolder);
            if (Directory.Exists(fullFolder))
            {
                files.AddRange(Directory.GetFiles(fullFolder, "*.nani", SearchOption.AllDirectories)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
            }
        }

        return files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void AddSelectedProjectFiles()
    {
        foreach (var guid in Selection.assetGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".nani", StringComparison.OrdinalIgnoreCase) && !_selectedFiles.Contains(path))
                _selectedFiles.Add(path);
        }

        if (_selectedFiles.Count > 0)
            _status = $"Added {_selectedFiles.Count} .nani file(s).";
    }

    private void AddDroppedFiles()
    {
        foreach (var obj in DragAndDrop.objectReferences)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (path.EndsWith(".nani", StringComparison.OrdinalIgnoreCase) && !_selectedFiles.Contains(path))
                _selectedFiles.Add(path);
        }

        if (_selectedFiles.Count > 0)
            _status = $"Added {_selectedFiles.Count} .nani file(s) from drag-and-drop.";
    }

    private static string RemoveNaniCommands(string text)
    {
        var lines = new List<string>();
        using (var reader = new StringReader(text))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string trimmed = line.Trim();
                if (trimmed.StartsWith("@", StringComparison.Ordinal))
                    continue;

                lines.Add(line);
            }
        }

        return string.Join("\n", lines);
    }

    private static int CountChineseCharacters(string text)
    {
        int count = 0;
        foreach (char c in text)
        {
            if (IsChineseCharacter(c))
                count++;
        }
        return count;
    }

    private static bool IsChineseCharacter(char c)
    {
        return (c >= '\u4E00' && c <= '\u9FFF') ||
               (c >= '\u3400' && c <= '\u4DBF');
    }

    private static int CountNonEmptyLines(string text)
    {
        return text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static string ToAssetsRelative(string absolutePath)
    {
        string dataPath = Application.dataPath.Replace('\\', '/');
        string fullPath = absolutePath.Replace('\\', '/');

        if (fullPath == dataPath) return "Assets";
        if (fullPath.StartsWith(dataPath + "/", StringComparison.Ordinal))
            return "Assets" + fullPath.Substring(dataPath.Length);

        return null;
    }

    private static string ToFullPath(string assetRelativePath)
    {
        if (string.IsNullOrWhiteSpace(assetRelativePath)) return string.Empty;
        string clean = assetRelativePath.Replace('\\', '/');
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", clean));
    }

    private void ResetResult()
    {
        _fileCount = 0;
        _totalChineseChars = 0;
        _totalLines = 0;
        _selectedFiles.Clear();
        _status = "Choose a folder or drag .nani files here, then click Count.";
    }
}
