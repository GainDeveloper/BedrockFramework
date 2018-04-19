/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

We cache all recorded components and their instance IDs before switching to play mode.
During PlayMode we assign any new instances their prefab source.
********************************************************/

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BedrockFramework.PlayModeEdit
{
    [DisallowMultipleComponent, ExecuteInEditMode, EditorOnlyComponent]
    [AddComponentMenu("BedrockFramework/PlayModeEdit")]
    public class PlayModeEdit : MonoBehaviour
    {
        public List<Component> recordedComponents;
        public bool recordDestruction = true, recordInstantiation = true;

        [SerializeField]
        int _cachedInstance = 0;
        public int CachedID { get { return _cachedInstance; } }
        [SerializeField]
        int[] _cachedComponentIds = new int[] { };
        [SerializeField]
        Object[] _cachedComponentObjects = new Object[] { };
        public PlayModeEdit_System.ObjectCacheInstance[] CachedComponents
        {
            get
            {
                PlayModeEdit_System.ObjectCacheInstance[] cachedComponents = new PlayModeEdit_System.ObjectCacheInstance[_cachedComponentIds.Length];
                for (int i = 0; i < _cachedComponentIds.Length; i++)
                {
                    cachedComponents[i] = new PlayModeEdit_System.ObjectCacheInstance { cachedObject = _cachedComponentObjects[i], cahedID = _cachedComponentIds[i] };
                }
                return cachedComponents;
            }
        }

        GameObject _prefabInstance;
        public GameObject PrefabInstance
        {
            get { return _prefabInstance; }
        }

        void Awake()
        {
            // Assign default recorded components.
            if (recordedComponents == null)
            {
                recordedComponents = new List<Component>();
                recordedComponents.Add(GetComponent<Transform>());

                MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                    recordedComponents.Add(meshRenderer);

                Light light = GetComponent<Light>();
                if (light != null)
                    recordedComponents.Add(light);
            }
        }

        void Start()
        {
            // During play mode we set the prefab.
            if (EditorApplication.isPlaying && GetInstanceID() < 0)
            {
                AssignInstancePrefab();
            }
        }

        public void CacheRecordedComponents()
        {
            SerializedObject so = new SerializedObject(this);
            so.FindProperty("_cachedInstance").intValue = GetInstanceID();

            SerializedProperty spIds = so.FindProperty("_cachedComponentIds");
            SerializedProperty spObjects = so.FindProperty("_cachedComponentObjects");

            spIds.ClearArray();
            spObjects.ClearArray();

            // Add GameObject for name/ active ect.
            spIds.InsertArrayElementAtIndex(0);
            spIds.GetArrayElementAtIndex(0).intValue = gameObject.GetInstanceID();
            spObjects.InsertArrayElementAtIndex(0);
            spObjects.GetArrayElementAtIndex(0).objectReferenceValue = gameObject;

            foreach (Object component in PlayModeEdit_System.PlayModeEditComponents(this, includePlayModeEdit: false, recordedComponentsOnly: true))
            {
                int i = spIds.arraySize;
                spIds.InsertArrayElementAtIndex(i);
                spIds.GetArrayElementAtIndex(i).intValue = component.GetInstanceID();
                spObjects.InsertArrayElementAtIndex(i);
                spObjects.GetArrayElementAtIndex(i).objectReferenceValue = component;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        void AssignInstancePrefab()
        {
            if (!recordInstantiation)
                return;

            GameObject prefabSource = PrefabUtility.GetPrefabParent(gameObject) as GameObject;
            if (prefabSource != null)
            {
                _prefabInstance = prefabSource;
            }

        }
    }
}