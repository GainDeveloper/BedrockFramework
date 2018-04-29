using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;
using BedrockFramework.Utilities;

namespace BedrockFramework.DevTools
{
    public class LoggerEditor : EditorWindow
    {
        [System.Serializable]
        private class LoggerLogEditor
        {
            //public string logCategory;
            public string logMessage;
            public string logStackTrace;
            public LogType logType;
            GUIContent logGUI;

            public LoggerLogEditor(Logger.LoggerLog newLog)
            {
                //logCategory = newLog.logCategory;
                logMessage = newLog.logMessage;
                logType = newLog.logType;
                logStackTrace = newLog.logStackTrace;
                logGUI = new GUIContent(newLog.logTime.ToString("F3") + " : " + logMessage);
            }

            public void DrawLog(bool isOdd, LoggerEditor logEditor)
            {
                if (!logEditor.showLog && logType == LogType.Log)
                    return;
                if (!logEditor.showWarnings && logType == LogType.Warning)
                    return;
                if (!logEditor.showErrors && logType == LogType.Error)
                    return;

                if (logEditor.selected == this)
                {
                    logEditor.boxStyle.normal.background = logEditor.boxBgSelected;
                }
                else
                {
                    if (isOdd)
                    {
                        logEditor.boxStyle.normal.background = logEditor.boxBgOdd;
                    }
                    else
                    {
                        logEditor.boxStyle.normal.background = logEditor.boxBgEven;
                    }
                }

                logEditor.SetIconToType(false, logType);
                logGUI.image = logEditor.icon;
                if (GUILayout.Button(logGUI, logEditor.boxStyle, GUILayout.ExpandWidth(true), GUILayout.Height(logEditor.boxStyle.CalcSize(logGUI).y)))
                    logEditor.selected = this;
            }

            public void OpenScript()
            {
                string lastLine = logStackTrace.Split("\n\r".ToCharArray(), System.StringSplitOptions.RemoveEmptyEntries).Last();
                int startIndex = lastLine.IndexOf("(at ") + 4;
                lastLine = lastLine.Substring(startIndex, lastLine.Count() - startIndex - 1);

                string[] filenameLine = lastLine.Split(':');
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<Object>(filenameLine[0]), lineNumber: int.Parse(filenameLine[1]));
            }
        }

        [System.Serializable]
        private class LoggerEditorCategory
        {
            public string category;
            public GUIContent categoryContent;
            public List<LoggerLogEditor> categoryLogs;
            bool enabled = true;
            LogType lowestLogType = LogType.Log;

            public LoggerEditorCategory(string category)
            {
                this.category = category;
                categoryContent = new GUIContent(category);
                categoryLogs = new List<LoggerLogEditor>();
            }

            public void AddLog(LoggerEditor logEditor, Logger.LoggerLog logToAdd)
            {
                categoryLogs.Add(new LoggerLogEditor(logToAdd));
                categoryContent.image = logEditor.icon;
                categoryContent.text = category + " (" + categoryLogs.Count + ")";

                if (logToAdd.logType < lowestLogType)
                    lowestLogType = logToAdd.logType;

                if (logEditor.errorPause && logToAdd.logType == LogType.Error)
                    EditorApplication.isPaused = true;
            }

            public void DrawCategoryButtons(LoggerEditor logEditor)
            {
                logEditor.SetIconToType(true, lowestLogType);
                categoryContent.image = logEditor.icon;
                if (GUILayout.Toggle(enabled, categoryContent, EditorStyles.toolbarButton, GUILayout.Width(EditorStyles.toolbarButton.CalcSize(categoryContent).x)))
                {
                    foreach (LoggerEditorCategory logCategory in logEditor.activeCategoryLogs)
                        logCategory.enabled = false;
                    enabled = true;
                }
            }

            public void DrawCategoryLogs(LoggerEditor logEditor)
            {
                if (!enabled)
                    return;

                for (int i = 0; i < categoryLogs.Count; i++)
                {
                    categoryLogs[i].DrawLog(i % 2 != 0, logEditor);
                }
            }
        }

        // Add menu named "My Window" to the Window menu
        [MenuItem("Tools/Logger", priority = 50)]
        static void Init()
        {
            LoggerEditor window = (LoggerEditor)EditorWindow.GetWindow(typeof(LoggerEditor));
            window.Show();
        }

        [SerializeField]
        private List<LoggerEditorCategory> activeCategoryLogs;
        [SerializeField]
        private float sizeRatio = 0.5f;
        [SerializeField]
        private bool clearOnPlay = true;
        [SerializeField]
        private bool errorPause;
        [SerializeField]
        private bool showLog = true;
        [SerializeField]
        private bool showWarnings = true;
        [SerializeField]
        private bool showErrors = true;

        private GUIContent clearContent = new GUIContent("Clear");
        private GUIContent clearOnPlayContent = new GUIContent("Clear on Play");
        private GUIContent errorPauseContent = new GUIContent("ErrorPause");

        private Rect upperPanel;
        private Rect lowerPanel;
        private Rect resizer;
        private bool isResizing;
        private float resizerHeight = 5f;
        private float menuBarHeight = 35f;

        private Texture2D boxBgOdd;
        private Texture2D boxBgEven;
        private Texture2D boxBgSelected;
        private Texture2D icon;
        private Texture2D errorIcon;
        private Texture2D errorIconSmall;
        private Texture2D warningIcon;
        private Texture2D warningIconSmall;
        private Texture2D infoIcon;
        private Texture2D infoIconSmall;

        private Vector2 upperPanelScroll, lowerPanelScroll;
        GUIStyle resizerStyle, boxStyle, textAreaStyle;

        private LoggerLogEditor selected;

        public void OnEnable()
        {
            if (activeCategoryLogs == null)
            {
                activeCategoryLogs = new List<LoggerEditorCategory>();

                foreach (Logger.LoggerLog log in Logger.currentLogs)
                    Logger_OnLogAdded(log);
            }

            Logger.OnLogAdded += Logger_OnLogAdded;
            Application.logMessageReceived += LogMessageReceived;
            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;

            //GUI Setup
            titleContent = new GUIContent("Logger", EditorGUIUtility.Load("icons/d_unityeditor.consolewindow.png") as Texture2D);

            errorIcon = EditorGUIUtility.Load("icons/console.erroricon.png") as Texture2D;
            warningIcon = EditorGUIUtility.Load("icons/console.warnicon.png") as Texture2D;
            infoIcon = EditorGUIUtility.Load("icons/console.infoicon.png") as Texture2D;

            errorIconSmall = EditorGUIUtility.Load("icons/console.erroricon.sml.png") as Texture2D;
            warningIconSmall = EditorGUIUtility.Load("icons/console.warnicon.sml.png") as Texture2D;
            infoIconSmall = EditorGUIUtility.Load("icons/console.infoicon.sml.png") as Texture2D;

            resizerStyle = new GUIStyle();
            resizerStyle.normal.background = EditorGUIUtility.Load("icons/d_AvatarBlendBackground.png") as Texture2D;

            boxStyle = new GUIStyle();
            boxStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            boxBgOdd = EditorGUIUtility.Load("builtin skins/darkskin/images/cn entrybackodd.png") as Texture2D;
            boxBgEven = EditorGUIUtility.Load("builtin skins/darkskin/images/cnentrybackeven.png") as Texture2D;
            boxBgSelected = EditorGUIUtility.Load("builtin skins/darkskin/images/menuitemhover.png") as Texture2D;

            textAreaStyle = new GUIStyle();
            textAreaStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            textAreaStyle.normal.background = EditorGUIUtility.Load("builtin skins/darkskin/images/projectbrowsericonareabg.png") as Texture2D;

            // Editor Prefs Loading
            if (EditorPrefs.HasKey("sizeRatio"))
                sizeRatio = EditorPrefs.GetFloat("sizeRatio");
            if (EditorPrefs.HasKey("clearOnPlay"))
                clearOnPlay = EditorPrefs.GetBool("clearOnPlay");
            if (EditorPrefs.HasKey("errorPause"))
                errorPause = EditorPrefs.GetBool("errorPause");
            if (EditorPrefs.HasKey("showLog"))
                showLog = EditorPrefs.GetBool("showLog");
            if (EditorPrefs.HasKey("showWarnings"))
                showWarnings = EditorPrefs.GetBool("showWarnings");
            if (EditorPrefs.HasKey("showErrors"))
                showErrors = EditorPrefs.GetBool("showErrors");
        }

        private void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
        {
            if (clearOnPlay && obj == PlayModeStateChange.ExitingEditMode)
                ClearLogs();
        }

        private void OnDisable()
        {
            Logger.OnLogAdded -= Logger_OnLogAdded;
            Application.logMessageReceived -= LogMessageReceived;
            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
            SavePrefs();
        }

        private void OnDestroy()
        {
            OnDisable();
        }

        void SavePrefs()
        {
            EditorPrefs.SetFloat("sizeRatio", sizeRatio);
            EditorPrefs.SetBool("clearOnPlay", clearOnPlay);
            EditorPrefs.SetBool("errorPause", errorPause);
            EditorPrefs.SetBool("showLog", showLog);
            EditorPrefs.SetBool("showWarnings", showWarnings);
            EditorPrefs.SetBool("showErrors", showErrors);
        }

        private LoggerEditorCategory GetCategory(string category)
        {
            foreach (LoggerEditorCategory existingCategory in activeCategoryLogs)
            {
                if (existingCategory.category == category)
                    return existingCategory;
            }

            LoggerEditorCategory newCategory = new LoggerEditorCategory(category);
            activeCategoryLogs.Add(newCategory);
            return newCategory;
        }

        private void Logger_OnLogAdded(Logger.LoggerLog newLog)
        {
            GetCategory(newLog.logCategory).AddLog(this, newLog);
            Repaint();
        }

        private void LogMessageReceived(string condition, string stackTrace, LogType type)
        {
            GetCategory(Logger.defaultCategory).AddLog(this, new Logger.LoggerLog(Logger.defaultCategory, condition, type, stackTrace));
            Repaint();
        }

        private void SetIconToType(bool small, LogType logType)
        {
            if (small)
            {
                switch (logType)
                {
                    case LogType.Error: icon = errorIconSmall; break;
                    case LogType.Exception: icon = errorIconSmall; break;
                    case LogType.Assert: icon = errorIconSmall; break;
                    case LogType.Warning: icon = warningIconSmall; break;
                    case LogType.Log: icon = infoIconSmall; break;
                }
            } else
            {
                switch (logType)
                {
                    case LogType.Error: icon = errorIcon; break;
                    case LogType.Exception: icon = errorIcon; break;
                    case LogType.Assert: icon = errorIcon; break;
                    case LogType.Warning: icon = warningIcon; break;
                    case LogType.Log: icon = infoIcon; break;
                }
            }
        }

        //
        // GUI Drawing
        //

        void OnGUI()
        {
            DrawOptionsBar();
            DrawCategoriesBar();

            DrawUpperPanel();
            DrawLowerPanel();
            DrawResizer();

            ProcessEvents(Event.current);

            if (GUI.changed) Repaint();
        }

        void DrawOptionsBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(EditorStyles.toolbar.fixedHeight), GUILayout.ExpandWidth(true));
            {
                if (GUILayout.Button(clearContent, EditorStyles.toolbarButton, GUILayout.Width(EditorStyles.toolbarButton.CalcSize(clearContent).x)))
                    ClearLogs();
                GUILayout.Space(7);
                clearOnPlay = GUILayout.Toggle(clearOnPlay, clearOnPlayContent, EditorStyles.toolbarButton, GUILayout.Width(EditorStyles.toolbarButton.CalcSize(clearOnPlayContent).x));
                errorPause = GUILayout.Toggle(errorPause, errorPauseContent, EditorStyles.toolbarButton, GUILayout.Width(EditorStyles.toolbarButton.CalcSize(errorPauseContent).x));

                GUILayout.FlexibleSpace();

                showLog = GUILayout.Toggle(showLog, new GUIContent("L", infoIconSmall), EditorStyles.toolbarButton, GUILayout.Width(30));
                showWarnings = GUILayout.Toggle(showWarnings, new GUIContent("W", warningIconSmall), EditorStyles.toolbarButton, GUILayout.Width(30));
                showErrors = GUILayout.Toggle(showErrors, new GUIContent("E", errorIconSmall), EditorStyles.toolbarButton, GUILayout.Width(30));
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawCategoriesBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(EditorStyles.toolbar.fixedHeight), GUILayout.ExpandWidth(true));
            foreach (LoggerEditorCategory category in activeCategoryLogs)
                category.DrawCategoryButtons(this);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawUpperPanel()
        {
            upperPanel = new Rect(0, menuBarHeight, position.width, ((position.height - menuBarHeight) * sizeRatio));

            GUILayout.BeginArea(upperPanel);
            upperPanelScroll = GUILayout.BeginScrollView(upperPanelScroll);

            foreach (LoggerEditorCategory category in activeCategoryLogs)
                category.DrawCategoryLogs(this);

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawLowerPanel()
        {
            lowerPanel = new Rect(0, (position.height - menuBarHeight) * sizeRatio + menuBarHeight + resizerHeight, position.width, (position.height * (1 - sizeRatio)) - resizerHeight);

            GUILayout.BeginArea(lowerPanel);
            lowerPanelScroll = GUILayout.BeginScrollView(lowerPanelScroll);

            if (selected != null)
            {
                GUILayout.TextArea(selected.logStackTrace, textAreaStyle);
            }
           
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawResizer()
        {
            resizer = new Rect(0, (position.height - menuBarHeight) * sizeRatio + menuBarHeight - resizerHeight, position.width, resizerHeight * 2);

            GUILayout.BeginArea(new Rect(resizer.position + (Vector2.up * resizerHeight), new Vector2(position.width, 2)), resizerStyle);
            GUILayout.EndArea();

            EditorGUIUtility.AddCursorRect(resizer, MouseCursor.ResizeVertical);
        }

        private void ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case EventType.Used:
                    if (e.clickCount == 2 && upperPanel.Contains(e.mousePosition))
                    {
                        if (selected != null)
                            selected.OpenScript();
                    }
                    break;
                case EventType.MouseDown:
                    if (e.button == 0 && resizer.Contains(e.mousePosition))
                    {
                        isResizing = true;
                    }
                    break;

                case EventType.MouseUp:
                    isResizing = false;
                    break;
            }

            Resize(e);
        }

        private void ClearLogs()
        {
            activeCategoryLogs.Clear();
            selected = null;
        }

        private void Resize(Event e)
        {
            if (isResizing)
            {
                sizeRatio = (e.mousePosition.y - menuBarHeight) / (position.height - menuBarHeight);
                sizeRatio = Mathf.Clamp(sizeRatio, 0.1f, 0.9f);
                Repaint();
            }
        }
    }
}