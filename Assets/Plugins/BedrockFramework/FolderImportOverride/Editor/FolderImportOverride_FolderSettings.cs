using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;

namespace BedrockFramework.FolderImportOverride
{
    [CreateAssetMenu(fileName = "FolderSettings",menuName = "BedrockFramework/FolderSettings", order = 0)]
    class FolderImportOverride_FolderSettings : Prototype.PrototypeObject //TODO: Experiment if we can use Odin drawerers with the PrototypeObject
    {
        [BoxGroup("Model Import Settings Override"), SerializeField]
        private ModelImporterOverrideBool importVisibility = new ModelImporterOverrideBool(ToModelImport.ImportVisibility);
        [BoxGroup("Model Import Settings Override"), SerializeField]
        private ModelImporterOverrideBool importCameras = new ModelImporterOverrideBool(ToModelImport.ImportCameras);
        [BoxGroup("Model Import Settings Override"), SerializeField]
        private ModelImporterOverrideBool importLights = new ModelImporterOverrideBool(ToModelImport.ImportLights);

        [BoxGroup("Model Import Settings Override"), SerializeField]
        private ModelImporterOverrideAnimationType animationType = new ModelImporterOverrideAnimationType(ToModelImport.AnimationType);
        [BoxGroup("Model Import Settings Override"), SerializeField]
        private ModelImporterOverrideBool importAnimation = new ModelImporterOverrideBool(ToModelImport.ImportAnimation);

        [BoxGroup("Model Import Settings Override"), SerializeField]
        private ModelImporterOverrideBool importMaterials = new ModelImporterOverrideBool(ToModelImport.ImportMaterials);
        [BoxGroup("Model Import Settings Override"), SerializeField]
        private ModelImporterOverrideMaterialSearch materialSearch = new ModelImporterOverrideMaterialSearch(ToModelImport.MaterialSearch);
        [BoxGroup("Model Import Settings Override"), SerializeField]
        private ModelImporterOverrideMaterialName materialName = new ModelImporterOverrideMaterialName(ToModelImport.ModelImporterMaterialName);
        [BoxGroup("Model Import Settings Override"), SerializeField]
        private List<FolderImportOverride_Actions> modelPreActions = new List<FolderImportOverride_Actions>();
        [BoxGroup("Model Import Settings Override"), SerializeField]
        private List<FolderImportOverride_Actions> modelPostActions = new List<FolderImportOverride_Actions>();

        [InfoBox("The are invoked after all assets have finished importing.")]
        [SerializeField]
        private List<FolderImportOverride_Actions> assetDeletedActions = new List<FolderImportOverride_Actions>();
        [SerializeField]
        private List<FolderImportOverride_Actions> assetImportedActions = new List<FolderImportOverride_Actions>();

        public void OverrideModelImporter(ModelImporter importer)
        {
            importVisibility.OverrideModelImporter(importer);
            importCameras.OverrideModelImporter(importer);
            importLights.OverrideModelImporter(importer);

            animationType.OverrideModelImporter(importer);
            importAnimation.OverrideModelImporter(importer);

            importMaterials.OverrideModelImporter(importer);
            materialSearch.OverrideModelImporter(importer);
            materialName.OverrideModelImporter(importer);

            foreach (FolderImportOverride_Actions action in modelPreActions)
                action.InvokePreAction(importer);
        }

        public void PostModelImport(GameObject gameObject)
        {
            foreach (FolderImportOverride_Actions action in modelPostActions)
                action.InvokePostAction(gameObject);
        }

        public void AssetDeleted(string assetPath)
        {
            foreach (FolderImportOverride_Actions action in assetDeletedActions)
                action.InvokeDeleteAction(assetPath);
        }

        public void AssetImported(string assetPath)
        {
            foreach (FolderImportOverride_Actions action in assetImportedActions)
                action.InvokeImportAction(assetPath);
        }

        // Specific Override (For Serialization)

        [System.Serializable]
        public class ModelImporterOverrideBool : ModelImporterOverrideGeneric<bool>
        {
            public ModelImporterOverrideBool(ToModelImport newOverride): base(newOverride) { }
        }

        [System.Serializable]
        public class ModelImporterOverrideMaterialSearch : ModelImporterOverrideGeneric<ModelImporterMaterialSearch>
        {
            public ModelImporterOverrideMaterialSearch(ToModelImport newOverride) : base(newOverride) { }
        }

        [System.Serializable]
        public class ModelImporterOverrideMaterialName : ModelImporterOverrideGeneric<ModelImporterMaterialName>
        {
            public ModelImporterOverrideMaterialName(ToModelImport newOverride) : base(newOverride) { }
        }

        [System.Serializable]
        public class ModelImporterOverrideAnimationType : ModelImporterOverrideGeneric<ModelImporterAnimationType>
        {
            public ModelImporterOverrideAnimationType(ToModelImport newOverride) : base(newOverride) { }
        }

        // Generic Override

        [System.Serializable]
        public class ModelImporterOverrideGeneric<T>
        {
#pragma warning disable
            public bool overrideEnabled;
            public T someField;
#pragma warning restore
            protected ToModelImport toOverride;

            public ModelImporterOverrideGeneric(ToModelImport newOverride)
            {
                toOverride = newOverride;
            }

            public void OverrideModelImporter(ModelImporter importer)
            {
                if (!overrideEnabled)
                    return;

                switch (toOverride)
                {
                    case ToModelImport.ImportMaterials:
                        importer.importMaterials = (bool)(object)someField;
                        break;
                    case ToModelImport.ImportAnimation:
                        importer.importAnimation = (bool)(object)someField;
                        break;
                    case ToModelImport.MaterialSearch:
                        importer.materialSearch = (ModelImporterMaterialSearch)(object)someField;
                        break;
                    case ToModelImport.ModelImporterMaterialName:
                        importer.materialName = (ModelImporterMaterialName)(object)someField;
                        break;
                    case ToModelImport.AnimationType:
                        importer.animationType = (ModelImporterAnimationType)(object)someField;
                        break;
                    case ToModelImport.ImportVisibility:
                        importer.importVisibility = (bool)(object)someField;
                        break;
                    case ToModelImport.ImportCameras:
                        importer.importCameras = (bool)(object)someField;
                        break;
                    case ToModelImport.ImportLights:
                        importer.importLights = (bool)(object)someField;
                        break;
                }
            }
        }

        public enum ToModelImport
        {
            ImportMaterials,
            ImportAnimation,
            MaterialSearch,
            ModelImporterMaterialName,
            AnimationType,
            ImportVisibility,
            ImportCameras,
            ImportLights
        }
    }

    [CustomPropertyDrawer(typeof(FolderImportOverride_FolderSettings.ModelImporterOverrideBool), true)]
    [CustomPropertyDrawer(typeof(FolderImportOverride_FolderSettings.ModelImporterOverrideMaterialSearch), true)]
    [CustomPropertyDrawer(typeof(FolderImportOverride_FolderSettings.ModelImporterOverrideMaterialName), true)]
    [CustomPropertyDrawer(typeof(FolderImportOverride_FolderSettings.ModelImporterOverrideAnimationType), true)]
    public class ModelImporterOverrideGeneric_Drawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw label
            var toggleRect = new Rect(position.x, position.y, 10, position.height);
            var labelRect = new Rect(position.x + 15, position.y, 100, position.height);
            var valueRect = new Rect(175, position.y, position.width - 175, position.height);

            SerializedProperty enabledProperty = property.FindPropertyRelative("overrideEnabled");
            EditorGUI.PropertyField(toggleRect, enabledProperty, GUIContent.none);

            bool enabledDefault = GUI.enabled;
            GUI.enabled = enabledProperty.boolValue;
            EditorGUI.PrefixLabel(labelRect, GUIUtility.GetControlID(FocusType.Passive), label);
            EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("someField"), GUIContent.none);

            GUI.enabled = enabledDefault;
            EditorGUI.EndProperty();
        }
    }

}
