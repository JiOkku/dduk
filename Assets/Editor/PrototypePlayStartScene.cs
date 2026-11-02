#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PrototypePlayStartScene
{
    private const string CurrentSceneKey = "DdukPrototype.PlayCurrentScene";
    static PrototypePlayStartScene()
    {
        EditorApplication.delayCall += Configure;
        EditorApplication.projectChanged += Configure;
    }

    private static void Configure()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode && !SessionState.GetBool(CurrentSceneKey, false))
        {
            SceneAsset lobby = AssetDatabase.LoadAssetAtPath<SceneAsset>(SceneFlow2D.LobbyScene);
            if (lobby != null && EditorSceneManager.playModeStartScene != lobby)
                EditorSceneManager.playModeStartScene = lobby;
        }
    }

    [MenuItem("Tools/Prototype/Start Play From Lobby")]
    private static void FromLobby()
    {
        SessionState.SetBool(CurrentSceneKey, false);
        Configure();
    }

    [MenuItem("Tools/Prototype/Start Play From Current Scene")]
    private static void FromCurrent()
    {
        SessionState.SetBool(CurrentSceneKey, true);
        EditorSceneManager.playModeStartScene = null;
    }
}
#endif
