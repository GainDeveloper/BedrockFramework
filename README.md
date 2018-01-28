# BedrockFramework
Features,

- Implementation of prototype data model for Unity's ScriptableObjects. Entirely in editor, no runtime overhead.
- PlayMode editing. Allows for editing, instancing (prefabs only), deleting and parenting of gameobjects during playmode. Control over what objects and components are updated through the PlayModeEdit component.
- Folder Import Settings Override. Controls how assets are imported on a directory level. Allows for pre and post import actions to be invoked.
- DebugLog. Supports categories, string formatting (pythonesque), zero GC in release builds.

Planned Features,
- BuildSteps: Configurable builds with different steps to perform on a per asset and scene.
- DebugLog: Add HTML LogFile for debug builds.
- InGame Console. Supports calling predefined methods with arguments. Should have auto-complete & history. 
- PlayMode Editing: Merge Window, shows all changes made to the scene and lets user choose which ones to bring across to the editor scene.
