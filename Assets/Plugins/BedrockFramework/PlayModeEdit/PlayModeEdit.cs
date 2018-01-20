using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if (UNITY_EDITOR)
using UnityEditor;
#endif

namespace BedrockFramework.PlayModeEdit
{
    [DisallowMultipleComponent, ExecuteInEditMode]
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

        void Awake()
        {
            if (recordedComponents == null)
            {
                recordedComponents = new List<Component>();
                recordedComponents.Add(GetComponent<Transform>());
            }

            if (_cachedInstance == 0 || _cachedInstance != GetInstanceID())
            {
                _cachedInstance = GetInstanceID();
                NewObjectInstance();
            }
        }

        void NewObjectInstance()
        {
            if (!EditorApplication.isPlaying || !recordInstantiation)
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