using System.Linq;
using System.Collections.Generic;
using UnityEngine;
#if (UNITY_EDITOR)
using UnityEditor;
#endif

namespace BedrockFramework.PlayModeEdit
{
    [DisallowMultipleComponent, ExecuteInEditMode, EditorOnlyComponent]
    [AddComponentMenu("BedrockFramework/PlayModeEdit")]
    public class PlayModeEdit : MonoBehaviour
    {
#if (UNITY_EDITOR)
        public List<Component> recordedComponents;
        public bool recordDestruction = true, recordInstantiation = true;

        [SerializeField]
        int _cachedInstance = 0;
        GameObject _prefabInstance;

        public GameObject PrefabInstance
        {
            get { return _prefabInstance; }
        }

        public List<string> RecordedComponentTypes
        {
            get { return recordedComponents.Select(w => w.GetType().Name).ToList(); }
        }

        void Awake()
        {
            if (recordedComponents == null)
            {
                recordedComponents = new List<Component>();
                recordedComponents.Add(GetComponent<Transform>());
            }
        }

        void Start()
        {
            if (_cachedInstance == 0 || _cachedInstance != GetInstanceID())
            {
                if (!EditorApplication.isPlaying)
                {
                    SerializedObject so = new SerializedObject(this);
                    so.FindProperty("_cachedInstance").intValue = GetInstanceID();
                    so.ApplyModifiedPropertiesWithoutUndo();
                } else
                {
                    NewObjectInstance();
                }
            }
        }

        void NewObjectInstance()
        {
            if (!recordInstantiation)
                return;

            GameObject prefabSource = PrefabUtility.GetPrefabParent(gameObject) as GameObject;
            if (prefabSource != null)
            {
                _prefabInstance = prefabSource;
            }

        }
#endif
    }
}