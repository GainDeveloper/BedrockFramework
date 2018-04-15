using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace BedrockFramework.Prototype
{
    //[CustomEditor(typeof(PrototypeObject), true)]
    public class PrototypeEditor : Editor
    {
        SerializedPrototypeEditor serializedPrototypeObject;

        void OnEnable()
        {
            serializedPrototypeObject = new SerializedPrototypeEditor(serializedObject);
            serializedPrototypeObject.UpdatePrototypeHierachy();
        }

        void OnDisable()
        {
            SerializedPrototypeEditor.UpdateAllPrototypes();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            serializedPrototypeObject.PrototypeObject = EditorGUILayout.ObjectField(serializedPrototypeObject.PrototypeObject, typeof(PrototypeObject), false);

            SerializedProperty prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
            {
                do
                {
                    if (prop.name != "m_Script" && prop.name != "prototype" && prop.name != "modifiedValues")
                    {
                        bool isPropertyModified = serializedPrototypeObject.IsPropertyModified(prop.name);
                        if (!serializedPrototypeObject.PropertyExistsInPrototype(prop.name)) isPropertyModified = true;

                        EditorGUILayout.BeginHorizontal();

                        isPropertyModified = EditorGUILayout.Toggle(isPropertyModified, GUILayout.Width(16));
                        serializedPrototypeObject.SetPropertyModified(prop.name, isPropertyModified);

                        GUI.enabled = isPropertyModified;
                        if (prop.hasVisibleChildren)
                            EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(prop, true);
                        if (prop.hasVisibleChildren)
                            EditorGUI.indentLevel--;
                        EditorGUILayout.EndHorizontal();
                        GUI.enabled = true;
                    }
                }
                while (prop.NextVisible(false));
            }

            serializedObject.ApplyModifiedProperties();
            serializedPrototypeObject.UpdateNonModifiedProperties();
        }

        public class SerializedPrototypeEditor
        {
            SerializedProperty prototypeProperty;
            SerializedObject prototypeSerialized;

            SerializedObject serializedObject;

            public Object PrototypeObject
            {
                get
                {
                    return prototypeProperty.objectReferenceValue;
                }
                set
                {
                    if (value != prototypeProperty.objectReferenceValue)
                    {
                        prototypeProperty.objectReferenceValue = value;
                        UpdateSerializedPrototype();
                    }
                }
            }

            /// <summary>
            /// Updates all prototypes in Assets/.
            /// </summary>
            public static void UpdateAllPrototypes()
            {
                foreach (string assetGUID in AssetDatabase.FindAssets("t:Prototype"))
                {
                    SerializedPrototypeEditor serializedPrototypeEditor = new SerializedPrototypeEditor(new SerializedObject(AssetDatabase.LoadAssetAtPath<PrototypeObject>(AssetDatabase.GUIDToAssetPath(assetGUID))));
                    serializedPrototypeEditor.UpdatePrototypeHierachy();
                    serializedPrototypeEditor.serializedObject.ApplyModifiedProperties();
                }
            }

            public SerializedPrototypeEditor(SerializedObject serializedObject)
            {
                this.serializedObject = serializedObject;
                prototypeProperty = serializedObject.FindProperty("prototype").Copy();
                UpdateSerializedPrototype();
            }

            void UpdateSerializedPrototype()
            {
                if (prototypeProperty.objectReferenceValue != null && prototypeProperty.objectReferenceValue != serializedObject.context)
                    prototypeSerialized = new SerializedObject(prototypeProperty.objectReferenceValue);
                else
                    prototypeSerialized = null;
            }

            #region ModifiedProperties

            int PropertyModifiedIndex(string name)
            {
                SerializedProperty modifiedValues = serializedObject.FindProperty("modifiedValues");
                for (int i = 0; i < modifiedValues.arraySize; i++)
                {
                    if (modifiedValues.GetArrayElementAtIndex(i).stringValue == name)
                        return i;
                }
                return -1;
            }

            public bool PropertyExistsInPrototype(string property)
            {
                if (prototypeSerialized == null)
                    return false;

                if (prototypeSerialized.FindProperty(property) == null)
                    return false;

                return true;
            }

            public bool IsPropertyModified(string name)
            {
                return PropertyModifiedIndex(name) >= 0;
            }

            public void SetPropertyModified(string property, bool modified)
            {
                SerializedProperty modifiedValues = serializedObject.FindProperty("modifiedValues");
                int i = PropertyModifiedIndex(property);
                if (modified)
                {
                    if (i == -1)
                    {
                        modifiedValues.InsertArrayElementAtIndex(0);
                        modifiedValues.GetArrayElementAtIndex(0).stringValue = property;
                    }
                }
                else if (i >= 0)
                {
                    modifiedValues.DeleteArrayElementAtIndex(i);
                }
            }

            #endregion


            /// <summary>
            /// Updates all non modified properties in the prototype hierarchy. Works in reverse.
            /// </summary>
            public void UpdatePrototypeHierachy()
            {
                Stack<PrototypeObject> stack = new Stack<PrototypeObject>();
                PrototypeObject prototype = serializedObject.FindProperty("prototype").objectReferenceValue as PrototypeObject;
                while (prototype != null)
                {
                    stack.Push(prototype);

                    if (prototype == prototype.prototype)
                        break;

                    prototype = prototype.prototype;
                }

                if (stack.Count == 0)
                    return;

                while (stack.Count > 0)
                {
                    SerializedPrototypeEditor serializedObjectEditor = new SerializedPrototypeEditor(new SerializedObject(stack.Pop()));
                    serializedObjectEditor.UpdateNonModifiedProperties();
                }

                prototypeSerialized.Update();
            }

            /// <summary>
            /// Updates all non modified properties in this serialziedObject from the prototype.
            /// </summary>
            public void UpdateNonModifiedProperties()
            {
                SerializedProperty prop = serializedObject.GetIterator();
                if (prop.NextVisible(true))
                {
                    do
                    {
                        if (prop.name != "m_Script" && prop.name != "prototype" && prop.name != "modifiedValues")
                        {
                            bool isPropertyModified = IsPropertyModified(prop.name);
                            if (!PropertyExistsInPrototype(prop.name)) isPropertyModified = true;

                            if (!isPropertyModified && prototypeSerialized != null)
                            {
                                serializedObject.CopyFromSerializedProperty(prototypeSerialized.FindProperty(prop.name));
                            }
                        }
                    }
                    while (prop.NextVisible(false));
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}