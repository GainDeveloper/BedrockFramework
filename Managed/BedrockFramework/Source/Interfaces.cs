/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework

Interfaces (Game & Editor)
********************************************************/

namespace BedrockFramework
{
    public interface IRootGameScene { };
    public interface ISubScenes
    {
        string[] GetSubScenes();
    };
}