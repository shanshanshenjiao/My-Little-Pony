using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using UnityEngine.Events;

namespace DevionGames
{
    [InitializeOnLoad]
    public class ModuleManagerWindow : EditorWindow
    {
        private const float LIST_MIN_WIDTH = 280f;
        private const float LIST_MAX_WIDTH = 600f;
        private const float LIST_RESIZE_WIDTH = 10f;

        private static string m_ModuleTxtPath = "https://deviongames.com/modules/modules.txt";
        private Rect m_SidebarRect = new Rect(0, 30, 280, 1000);
        private Vector2 m_ScrollPosition;
        private string m_SearchString = "Search...";
        private Vector2 m_SidebarScrollPosition;
        private ModuleItem selectedItem;
        private ModuleItem[] m_Items = new ModuleItem[0];
        private static string tempPath { get { return CombinePath(Application.dataPath, "..", "Temp", "Modules"); } }

        private int m_SelectedChangeLog;

        [MenuItem("Tools/Devion Games/Module Manager", false, -1000)]
        public static void ShowWindow()
        {
            ModuleManagerWindow window = EditorWindow.GetWindow<ModuleManagerWindow>(false, "Module Manager");
            window.minSize = new Vector2(500f, 300f);
            StartBackgroundTask(RequestModules(delegate (ModuleItem[] items) { window.m_Items = items; }));
        }

        static ModuleManagerWindow()
        {
            EditorApplication.update += UpdateCheck;
        }

        private static void UpdateCheck()
        {
            if (EditorApplication.timeSinceStartup > 5.0 && EditorApplication.timeSinceStartup < 10.0)
            {
                bool checkUpdates = EditorPrefs.GetBool("ModuleUpdateCheck", true);
                if (checkUpdates)
                {
                    StartBackgroundTask(RequestModules(delegate (ModuleItem[] items)
                    {
                        List<ModuleItem> updatedModules = new List<ModuleItem>();
                        for (int i = 0; i < items.Length; i++)
                        {
                            ModuleItem current = items[i];
                            if (current != null && current.IsInstalled && current.InstalledModule.version != current.version)
                            {
                                updatedModules.Add(current);
                            }
                        }

                        if (updatedModules.Count > 0)
                        {
                            UpdateNotificationWindow.ShowWindow(updatedModules.ToArray());
                        }
                    }));
                }
                EditorApplication.update -= UpdateCheck;
            }
        }

        // ✅ ✅ 已修复：不会再报 JSON 错误
        private static IEnumerator RequestModules(UnityAction<ModuleItem[]> result)
        {
            using (UnityWebRequest w = UnityWebRequest.Get(m_ModuleTxtPath))
            {
                yield return w.SendWebRequest();

                // 网络失败
                if (w.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("ModuleManager 请求失败: " + w.error);
                    result?.Invoke(new ModuleItem[0]);
                    yield break;
                }

                string json = w.downloadHandler.text;

                // 空数据
                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogWarning("ModuleManager 返回为空");
                    result?.Invoke(new ModuleItem[0]);
                    yield break;
                }

                ModuleItem[] items = new ModuleItem[0];

                try
                {
                    items = JsonHelper.FromJson<ModuleItem>(json);

                    if (items == null)
                    {
                        result?.Invoke(new ModuleItem[0]);
                        yield break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("JSON解析失败，已跳过");
                    Debug.LogWarning(json);
                    Debug.LogException(e);

                    result?.Invoke(new ModuleItem[0]);
                    yield break;
                }

                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] != null)
                    {
                        try
                        {
                            items[i].Initialize();
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("初始化模块失败: " + items[i]?.name);
                            Debug.LogException(e);
                        }
                    }
                }

                result?.Invoke(items);
            }
        }

        private void OnGUI()
        {
            int index = EditorPrefs.GetInt("ModuleEditorItemIndex", -1);
            if (index != -1 && index < m_Items.Length)
            {
                selectedItem = m_Items[index];
            }

            m_SidebarRect = new Rect(0f, 0f, m_SidebarRect.width, position.height);
            GUILayout.BeginArea(m_SidebarRect, "", Styles.background);

            DoSearchGUI();
            m_SidebarScrollPosition = GUILayout.BeginScrollView(m_SidebarScrollPosition);

            for (int i = 0; i < m_Items.Length; i++)
            {
                ModuleItem currentItem = m_Items[i];
                if (currentItem == null) continue;
                if (!MatchesSearch(currentItem, m_SearchString)) continue;

                if (GUILayout.Button(currentItem.name))
                {
                    selectedItem = currentItem;
                    EditorPrefs.SetInt("ModuleEditorItemIndex", i);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            Rect rect = new Rect(m_SidebarRect.width, 0, position.width - m_SidebarRect.width, position.height);
            GUILayout.BeginArea(rect);

            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
            if (selectedItem != null)
            {
                GUILayout.Label(selectedItem.name, EditorStyles.boldLabel);
                GUILayout.Label(selectedItem.description);
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private bool MatchesSearch(ModuleItem item, string search)
        {
            search = search.ToLower();
            return search.Equals("search...") ||
                   item.name.ToLower().Contains(search) ||
                   item.description.ToLower().Contains(search);
        }

        private void DoSearchGUI()
        {
            GUILayout.Space(3f);
            m_SearchString = GUILayout.TextField(m_SearchString);
            GUILayout.Space(3f);
        }

        private static void StartBackgroundTask(IEnumerator update)
        {
            EditorApplication.CallbackFunction callback = null;

            callback = () =>
            {
                try
                {
                    if (!update.MoveNext())
                    {
                        EditorApplication.update -= callback;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    EditorApplication.update -= callback;
                }
            };

            EditorApplication.update += callback;
        }

        private static string CombinePath(params string[] paths)
        {
            string combinedPath = "";
            foreach (var path in paths)
            {
                if (path != null)
                {
                    combinedPath = Path.Combine(combinedPath, path);
                }
            }
            return combinedPath;
        }

        private static class Styles
        {
            public static GUIStyle background = new GUIStyle("ProfilerLeftPane");
        }
    }
}