/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Creates a editor window displaying the current scenes definition file.
Allows user to quickly create and modify additional scenes.
********************************************************/

using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace BedrockFramework.Scenes
{
    [InitializeOnLoad]
    public class EditorSceneManager_EditorWindow : EditorWindow
    {
        private class SceneManager_Scene
        {
            string sceneName;
            GUIContent logGUI;
            int index;
            EditorSceneManager_EditorWindow sceneManager;

            public SceneManager_Scene(string sceneName, EditorSceneManager_EditorWindow sceneManager, int i)
            {
                this.sceneName = sceneName;
                this.logGUI = new GUIContent(" " + sceneName, sceneManager.sceneIcon);
                this.index = i;
                this.sceneManager = sceneManager;
            }

            public void DrawScene(bool isOdd)
            {
                if (sceneManager.selected == this)
                {
                    sceneManager.boxStyle.normal.background = sceneManager.boxBgSelected;
                }
                else
                {
                    if (isOdd)
                    {
                        sceneManager.boxStyle.normal.background = sceneManager.boxBgOdd;
                    }
                    else
                    {
                        sceneManager.boxStyle.normal.background = sceneManager.boxBgEven;
                    }
                }

                if (GUILayout.Button(logGUI, sceneManager.boxStyle, GUILayout.ExpandWidth(true), GUILayout.Height(sceneManager.boxStyle.CalcSize(logGUI).y)))
                {
                    sceneManager.selected = this;
                    if (Event.current.button == 1)
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Remove " + sceneName), false, RemoveScene);
                        menu.ShowAsContext();
                    }
                }
            }

            public void RemoveScene()
            {
                sceneManager.RemoveSceneAt(index);
            }
        }

        List<SceneManager_Scene> currentAdditionalScenes = new List<SceneManager_Scene>();

        private Rect sceneArea;
        private float menuBarHeight = 15f;

        private Texture2D boxBgOdd;
        private Texture2D boxBgEven;
        private Texture2D boxBgSelected;
        private Texture2D sceneIcon, refreshIcon, addIcon;

        Vector2 panelScroll;
        GUIStyle boxStyle, infoStyle;

        SceneManager_Scene selected;
        bool requiresRefresh = false;

        EditorSceneManager_EditorWindow()
        {
            EditorSceneManager_Loader.OnDefinitionChange += EditorSceneManager_Loader_OnDefinitionChange;
        }

        private void EditorSceneManager_Loader_OnDefinitionChange()
        {
            currentAdditionalScenes = new List<SceneManager_Scene>();

            if (EditorSceneManager_Loader.currentDefinition == null)
            {
                Repaint();
                return;
            }

            int i = 0;
            foreach (string sceneName in EditorSceneManager_Loader.currentDefinition.AdditionalScenes)
            {
                currentAdditionalScenes.Add(new SceneManager_Scene(sceneName, this, i));
                i++;
            }

            Repaint();
        }

        [MenuItem("Tools/Scene Manager")]
        public static void OpenGameScenes()
        {
            EditorSceneManager_EditorWindow window = (EditorSceneManager_EditorWindow)EditorWindow.GetWindow(typeof(EditorSceneManager_EditorWindow), false, "Scene Manager");
        }

        void OnEnable()
        {
            boxBgOdd = EditorGUIUtility.Load("builtin skins/darkskin/images/cn entrybackodd.png") as Texture2D;
            boxBgEven = EditorGUIUtility.Load("builtin skins/darkskin/images/cnentrybackeven.png") as Texture2D;
            boxBgSelected = EditorGUIUtility.Load("builtin skins/darkskin/images/menuitemhover.png") as Texture2D;

            sceneIcon = EditorGUIUtility.FindTexture("BuildSettings.SelectedIcon");
            refreshIcon = EditorGUIUtility.FindTexture("d_RotateTool");
            addIcon = EditorGUIUtility.FindTexture("d_Toolbar Plus");

            boxStyle = new GUIStyle();
            boxStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            boxStyle.alignment = TextAnchor.MiddleLeft;
            boxStyle.padding = new RectOffset(6, 0, 1, 1);

            infoStyle = new GUIStyle();
            infoStyle.normal.textColor = new Color(0.3f, 0.3f, 0.3f);
            infoStyle.alignment = TextAnchor.MiddleCenter;
            infoStyle.padding = new RectOffset(6, 6, 6, 6);

            EditorSceneManager_Loader_OnDefinitionChange();
        }

        void OnGUI()
        {
            if (EditorApplication.isPlaying)
            {
                DrawPlayMode();
                return;
            }

            DrawHeader();

            if (EditorSceneManager_Loader.currentDefinition == null)
            {
                DrawSceneDefinitionBuilder();
            }
            else
            {
                DrawScenes();
                DragArea();
            }

            if (requiresRefresh)
            {
                requiresRefresh = false;
                EditorSceneManager_Loader.RefreshCurrentSceneDefinition();
            }
        }

        void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(EditorStyles.toolbar.fixedHeight), GUILayout.ExpandWidth(true));
            {
                if (EditorSceneManager_Loader.currentDefinition != null)
                {
                    GUILayout.Label("Root Scene : " + EditorSceneManager_Loader.currentDefinition.PrimaryScene);

                    GUILayout.FlexibleSpace();
                }
                else
                {
                    GUILayout.Label("No Root Scene");
                }

                GUILayout.FlexibleSpace();
                GUI.enabled = EditorSceneManager_Loader.currentDefinition != null;

                if (GUILayout.Button(new GUIContent(addIcon, "Create New Additional Scene"), EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    CreateNewScene();
                }

                if (GUILayout.Button(new GUIContent(refreshIcon, "Reload Scenes"), EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    EditorSceneManager_Loader.RefreshLoadedScenes();
                }

                GUI.enabled = true;
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawSceneDefinitionBuilder()
        {
            if (EditorSceneManager.GetActiveScene().name != "" && EditorSceneManager.GetActiveScene().path != SceneDefinition.entryScenePath)
            {
                if (GUILayout.Button("Create Scene Definition for " + EditorSceneManager.GetActiveScene().name, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    SceneDefinition_Editor.CreateFromScene(EditorSceneManager.GetActiveScene());
                    EditorSceneManager_Loader.RefreshCurrentSceneDefinition();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No Saved Scene Loaded", MessageType.Warning);
            }

        }

        void DrawPlayMode()
        {
            sceneArea = new Rect(0, 0, position.width, position.height);
            GUILayout.BeginArea(sceneArea);
            EditorGUILayout.LabelField("Disabled During PlayMode", infoStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.EndArea();
        }

        void DrawScenes()
        {
            sceneArea = new Rect(0, menuBarHeight, position.width, position.height - menuBarHeight);
            GUILayout.BeginArea(sceneArea);
            panelScroll = GUILayout.BeginScrollView(panelScroll);

            for (int i = 0; i < currentAdditionalScenes.Count; i++)
            {
                currentAdditionalScenes[i].DrawScene(i % 2 != 0);
            }

            EditorGUILayout.LabelField("Drop Scenes Here", infoStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        void DragArea()
        {
            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!sceneArea.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (Object dragged_object in DragAndDrop.objectReferences)
                        {
                            if (dragged_object.GetType() == typeof(SceneAsset))
                                AddScene((SceneAsset)dragged_object);
                        }
                    }
                    break;
            }
        }

        void RemoveSceneAt(int i)
        {
            if (EditorSceneManager_Loader.currentDefinition == null)
                return;

            SerializedObject serializedObject = new SerializedObject(EditorSceneManager_Loader.currentDefinition);
            SerializedProperty additionalScenesProperty = serializedObject.FindProperty("additionalScenes");

            additionalScenesProperty.DeleteArrayElementAtIndex(i);

            serializedObject.ApplyModifiedProperties();
            requiresRefresh = true;
        }

        void CreateNewScene()
        {
            SceneAsset newScene = EditorSceneManager_Loader.CreateNewScene();

            if (newScene != null)
            {
                AddScene(newScene);
            }
        }

        void AddScene(SceneAsset sceneAsset)
        {
            if (EditorSceneManager_Loader.currentDefinition == null)
                return;

            SerializedObject serializedObject = new SerializedObject(EditorSceneManager_Loader.currentDefinition);
            SerializedProperty additionalScenesProperty = serializedObject.FindProperty("additionalScenes");

            int i = additionalScenesProperty.arraySize;
            additionalScenesProperty.InsertArrayElementAtIndex(i);

            additionalScenesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("m_SceneAsset").objectReferenceValue = sceneAsset;
            additionalScenesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("m_SceneName").stringValue = sceneAsset.name;

            serializedObject.ApplyModifiedProperties();
            requiresRefresh = true;
        }
    }
}