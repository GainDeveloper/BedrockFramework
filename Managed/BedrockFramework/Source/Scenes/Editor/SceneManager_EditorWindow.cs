/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Creates a editor window displaying the current scenes definition file.
********************************************************/

using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BedrockFramework.Utilities;

namespace BedrockFramework.Scenes
{
    [InitializeOnLoad]
    public class SceneManager_EditorWindow : EditorWindow
    {
        private class SceneManager_Scene
        {
            string sceneName;
            GUIContent logGUI;
            int index;
            SceneManager_EditorWindow sceneManager;

            public SceneManager_Scene(SceneField sceneField, SceneManager_EditorWindow sceneManager, int i)
            {
                this.sceneName = sceneField.SceneName;
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

        SceneDefinition currentDefinition;
        List<SceneManager_Scene> currentAdditionalScenes = new List<SceneManager_Scene>();

        private Rect sceneArea;
        private float menuBarHeight = 15f;

        private Texture2D boxBgOdd;
        private Texture2D boxBgEven;
        private Texture2D boxBgSelected;
        private Texture2D sceneIcon;

        Vector2 panelScroll;
        GUIStyle boxStyle, infoStyle;

        SceneManager_Scene selected;
        bool requiresRefresh = false;
        bool ignoreSceneEvents = false;

        SceneManager_EditorWindow()
        {
            EditorSceneManager.sceneOpened += OnSceneLoaded;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            EditorSceneManager.newSceneCreated += OnSceneCreated;
        }

        private void OnSceneCreated(UnityEngine.SceneManagement.Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            Debug.Log("Scene Created!");
            RefreshCurrentSceneDefinition();
        }

        private void OnSceneClosed(UnityEngine.SceneManagement.Scene scene)
        {
            // Check if scene was closed or just unloaded.
            if (EditorSceneManager.GetSceneManagerSetup().Where(x => x.path == scene.path).Count() == 0)
            {
                Debug.Log("Scene Closed!");
                RefreshCurrentSceneDefinition();
            }
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene arg0, UnityEditor.SceneManagement.OpenSceneMode arg1)
        {
            Debug.Log("Scene Loaded!");
            RefreshCurrentSceneDefinition();
        }

        void RefreshCurrentSceneDefinition()
        {
            if (ignoreSceneEvents)
                return;

            requiresRefresh = false;
            currentDefinition = FindSceneDefinition();
            currentAdditionalScenes = new List<SceneManager_Scene>();

            if (currentDefinition == null)
            {
                Repaint();
                return;
            }

            for (int i = 0; i < currentDefinition.additionalScenes.Length; i++)
            {
                currentAdditionalScenes.Add(new SceneManager_Scene(currentDefinition.additionalScenes[i], this, i));
            }

            Repaint();
            RefreshLoadedScenes();
        }

        void RefreshLoadedScenes()
        {
            UnityEngine.SceneManagement.Scene rootScene = RootScene();
            ignoreSceneEvents = true;

            IEnumerable<UnityEngine.SceneManagement.Scene> currentScenes = Enumerable.Range(0, EditorSceneManager.loadedSceneCount).Select(x => EditorSceneManager.GetSceneAt(x));
            IEnumerable<string> desiredScenes = currentDefinition.additionalScenes.Select(x => x.SceneFilePath);
            desiredScenes = desiredScenes.Concat(new[] { rootScene.path });

            // Unload any scenes no longer part of the additional scenes.
            foreach (UnityEngine.SceneManagement.Scene scene in currentScenes.Where(x => !desiredScenes.Contains(x.path)))
                EditorSceneManager.CloseScene(scene, true);

            // Load remaining.
            foreach (string scenePath in desiredScenes.Where(x => !currentScenes.Select(y => y.path).Contains(x)))
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            EditorSceneManager.SetActiveScene(rootScene);
            ignoreSceneEvents = false;
        }

        private SceneDefinition FindSceneDefinition()
        {
            for (int i = 0; i < EditorSceneManager.loadedSceneCount; i++)
            {
                SceneDefinition sceneDefintion = SceneDefinition.FromPath(EditorSceneManager.GetSceneAt(i).path);
                if (sceneDefintion != null)
                {
                    return sceneDefintion;
                }
            }

            return null;
        }

        private UnityEngine.SceneManagement.Scene RootScene()
        {
            for (int i = 0; i < EditorSceneManager.loadedSceneCount; i++)
            {
                UnityEngine.SceneManagement.Scene scene = EditorSceneManager.GetSceneAt(i);
                SceneDefinition sceneDefintion = SceneDefinition.FromPath(scene.path);
                if (sceneDefintion != null)
                {
                    return scene;
                }
            }

            return new UnityEngine.SceneManagement.Scene();
        }

        [MenuItem("Tools/Scene Manager")]
        public static void OpenGameScenes()
        {
            SceneManager_EditorWindow window = (SceneManager_EditorWindow)EditorWindow.GetWindow(typeof(SceneManager_EditorWindow), false, "Scene Manager");
        }

        void OnEnable()
        {
            boxBgOdd = EditorGUIUtility.Load("builtin skins/darkskin/images/cn entrybackodd.png") as Texture2D;
            boxBgEven = EditorGUIUtility.Load("builtin skins/darkskin/images/cnentrybackeven.png") as Texture2D;
            boxBgSelected = EditorGUIUtility.Load("builtin skins/darkskin/images/menuitemhover.png") as Texture2D;

            sceneIcon = EditorGUIUtility.FindTexture("BuildSettings.SelectedIcon");

            boxStyle = new GUIStyle();
            boxStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            boxStyle.alignment = TextAnchor.MiddleLeft;
            boxStyle.padding = new RectOffset(6, 0, 2, 2);

            infoStyle = new GUIStyle();
            infoStyle.normal.textColor = new Color(0.3f, 0.3f, 0.3f);
            infoStyle.alignment = TextAnchor.MiddleCenter;
            infoStyle.padding = new RectOffset(6, 6, 6, 6);

            RefreshCurrentSceneDefinition();
        }

        void OnGUI()
        {
            DrawHeader();

            if (currentDefinition == null)
            {
                DrawSceneDefinitionBuilder();
            }
            else
            {
                DrawScenes();
                DragArea();
            }

            if (requiresRefresh)
                RefreshCurrentSceneDefinition();
        }

        void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(EditorStyles.toolbar.fixedHeight), GUILayout.ExpandWidth(true));
            {
                if (currentDefinition != null)
                {
                    GUILayout.Label("Root Scene : " + currentDefinition.primaryScene.SceneName);

                    GUILayout.FlexibleSpace();
                }
                else
                {
                    GUILayout.Label("No Root Scene");
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawSceneDefinitionBuilder()
        {
            if (EditorSceneManager.GetActiveScene().name != "")
            {
                if (GUILayout.Button("Create Scene Definition for " + EditorSceneManager.GetActiveScene().name, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    SceneDefinition.CreateFromScene(EditorSceneManager.GetActiveScene());
                    RefreshCurrentSceneDefinition();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No Saved Scene Loaded", MessageType.Warning);
            }

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

            /*
            GUI.enabled = currentDefinition != null;

            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUI.Box(dropArea, "", EditorStyles.helpBox);

            GUILayout.BeginArea(dropArea);
            panelScroll = GUILayout.BeginScrollView(panelScroll);

            if (currentDefinition != null)
            {
                SerializedObject serializedObject = new SerializedObject(currentDefinition);
                SerializedProperty additionalScenesProperty = serializedObject.FindProperty("additionalScenes");
                float yPosition = 0;
                const float removeButtonWidth = 16;
                const float rowHeight = 16;


                for (int i = 0; i < additionalScenesProperty.arraySize; i++)
                {
                    string additionalSceneName = additionalScenesProperty.GetArrayElementAtIndex(i).FindPropertyRelative("m_SceneName").stringValue;

                    if (GUI.Button(new Rect(5, yPosition, removeButtonWidth, rowHeight), "-", EditorStyles.miniButton))
                    {

                    }

                    EditorGUI.LabelField(new Rect(20, yPosition, dropArea.width - removeButtonWidth, rowHeight), additionalSceneName);
                    //EditorGUILayout.PropertyField(additionalScenesProperty.GetArrayElementAtIndex(i));

                    yPosition += 16;
                }

                serializedObject.ApplyModifiedProperties();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();




            GUI.enabled = true;
*/
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
            if (currentDefinition == null)
                return;

            SerializedObject serializedObject = new SerializedObject(currentDefinition);
            SerializedProperty additionalScenesProperty = serializedObject.FindProperty("additionalScenes");

            additionalScenesProperty.DeleteArrayElementAtIndex(i);

            serializedObject.ApplyModifiedProperties();
            requiresRefresh = true;
        }

        void AddScene(SceneAsset sceneAsset)
        {
            if (currentDefinition == null)
                return;

            SerializedObject serializedObject = new SerializedObject(currentDefinition);
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