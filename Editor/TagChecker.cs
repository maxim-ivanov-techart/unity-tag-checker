using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

public class TagChecker : EditorWindow
{
    private string[] _availableTags;
    private int _selectedTagIndex;
    private bool _includeInactive;
    private List<GameObject> _results = new ();
    private string _searchedTag;
    private Vector2 _scrollPosition;

    [MenuItem("Tools/Tag Checker")]
    public static void ShowWindow()
    {
        GetWindow<TagChecker>("Tag Checker").minSize = new Vector2(300, 200);
    }

    private void OnEnable()
    {
        _availableTags = BuildTagList();
    }

    private void OnGUI()
    {
        HandleHotkeys();
        
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Tag Checker", EditorStyles.boldLabel);
        DrawSeparator();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Tag", GUILayout.Width(30));
        _selectedTagIndex = EditorGUILayout.Popup(_selectedTagIndex, _availableTags);
        if (GUILayout.Button("↻", EditorStyles.miniButton, GUILayout.Width(22)))
        {
            _availableTags = BuildTagList();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(4);
        _includeInactive = EditorGUILayout.ToggleLeft("Include inactive objects", _includeInactive);
        EditorGUILayout.Space(2);

        EditorGUI.BeginDisabledGroup(_selectedTagIndex == 0);
        if (GUILayout.Button(_searchedTag != null ? $"Search ({_results.Count} found)" : "Search", GUILayout.Height(26)))
        {
            Search();
        }
        EditorGUI.EndDisabledGroup();

        if (_searchedTag == null)
        {
            return;
        }

        DrawSeparator();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Results — {_results.Count} object{(_results.Count != 1 ? "s" : "")}", EditorStyles.boldLabel);
        if (_results.Count > 0 && GUILayout.Button("Select All", EditorStyles.miniButton, GUILayout.Width(64)))
        {
            Selection.objects = _results.FindAll(obj => obj != null).ToArray();
        }
        if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(44)))
        {
            _searchedTag = null;
            _results.Clear();
            _scrollPosition = Vector2.zero;
        }
        EditorGUILayout.EndHorizontal();

        if (_results.Count == 0)
        {
            EditorGUILayout.HelpBox("No objects found with this tag.", MessageType.Info);
            return;
        }
        
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        for (int i = 0; i < _results.Count; i++)
        {
            GameObject obj = _results[i];
            if (obj == null)
            {
                continue;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            if (!obj.activeInHierarchy)
            {
                EditorGUILayout.LabelField("[off]", EditorStyles.miniLabel, GUILayout.Width(26));
            }

            if (GUILayout.Button(GetHierarchyPath(obj), EditorStyles.label, GUILayout.ExpandWidth(true)))
            {
                Selection.activeGameObject = obj;
                EditorGUIUtility.PingObject(obj);
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                }
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }

    private void HandleHotkeys()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return && _selectedTagIndex > 0)
        {
            Search();
            e.Use();
            Repaint();
        }
    }

    private void Search()
    {
        _results.Clear();
        _scrollPosition = Vector2.zero;
        _searchedTag = _availableTags[_selectedTagIndex];

        if (_includeInactive)
        {
            foreach (GameObject obj in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!obj.scene.isLoaded)
                {
                    continue;
                }

                if (obj.hideFlags == HideFlags.NotEditable || obj.hideFlags == HideFlags.HideAndDontSave)
                {
                    continue;
                }

                if (obj.CompareTag(_searchedTag))
                {
                    _results.Add(obj);
                }
            }
        }
        else
        {
            _results.AddRange(GameObject.FindGameObjectsWithTag(_searchedTag));
        }

        _results.Sort((a, b) => string.Compare(GetHierarchyPath(a), GetHierarchyPath(b)));
    }

    private string[] BuildTagList()
    {
        string[] projectTags = InternalEditorUtility.tags;
        string[] list = new string[projectTags.Length + 1];
        list[0] = "<Select Tag>";
        projectTags.CopyTo(list, 1);
        return list;
    }

    private string GetHierarchyPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + " / " + path;
            parent = parent.parent;
        }
        return path;
    }

    private void DrawSeparator()
    {
        EditorGUILayout.Space(4);
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(false, 1), new Color(0.5f, 0.5f, 0.5f, 0.3f));
        EditorGUILayout.Space(4);
    }
}