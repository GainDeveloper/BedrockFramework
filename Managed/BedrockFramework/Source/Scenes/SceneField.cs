/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Runtime SceneAsset.
********************************************************/

using UnityEngine;

namespace BedrockFramework.Scenes
{
    [System.Serializable]
    public class SceneField
    {

        [SerializeField]
        private Object m_SceneAsset = null;
        public Object SceneAsset
        {
            get
            {
                return m_SceneAsset;
            }
        }
        [SerializeField]
        private string m_SceneName = "";
        public string SceneName
        {
            get { return m_SceneName; }
        }


        public SceneField(Object sceneObject, string sceneName)
        {
            m_SceneAsset = sceneObject;
            m_SceneName = sceneName;
        }

        // makes it work with the existing Unity methods (LoadLevel/LoadScene)
        public static implicit operator string(SceneField sceneField)
        {
            return sceneField.SceneName;
        }
    }
}
