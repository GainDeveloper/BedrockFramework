# BedrockFramework
PlayMode Editing, GC Free DebugLog with Categories, Prototype ScriptableObjects, Folder Import Settings, Build Steps. An essential suite of editor tools I developed for my Unity projects.
<p align="center">
  <img src="https://i.imgur.com/6xEnft9.jpg" alt="Build Steps"/>
  <img src="https://i.imgur.com/kP4fEzq.jpg" alt="Folder Settings"/>
</p>
<p align="center">
  <img src="https://i.imgur.com/kk2pysp.jpg" alt="Logger"/>
  <img src="https://i.imgur.com/vd2dKdq.jpg" alt="ScriptableObject Prototype"/>
</p>

## Getting Started
Installing should be rather painless, you can just grab the DLLs in Assets/Plugins/BedrockFramework and drop them into your project.
All the source is kept in Managed/BedrockFramework/Source.

### Prerequisites
Built for Unity 2017.3.0f3. Makes use of some functionality not exposed in previous versions.
Odin Inspector is required for the BuildSteps & FolderSettings editors. 

### Feature Breakdown
- Implementation of prototype data model for Unity's ScriptableObjects. Entirely in editor, no runtime overhead.
- PlayMode editing. Allows for editing, instancing (prefabs only), deleting and parenting of gameobjects during playmode. Control over what objects and components are updated through the PlayModeEdit component.
- Folder Import Settings Override. Controls how assets are imported on a directory level. Allows for pre and post import actions to be invoked.
- DebugLog. Supports categories, string formatting (pythonesque), zero GC in release builds.
- BuildSteps. Configurable builds with different steps to perform on a per asset and scene.

### Future
- DebugLog: Add HTML LogFile for debug builds.
- InGame Console. Supports calling predefined methods with arguments. Should have auto-complete & history. 
- PlayMode Editing: Merge Window, shows all changes made to the scene and lets user choose which ones to bring across to the editor scene.
