using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Only the chosen entry stage and load guard are shared; no player/combat data survives a load.
public static class SceneFlow2D
{
    public const string LobbyScene = "Assets/Scenes/Lobby.unity";
    public const string DungeonScene = "Assets/Scenes/SampleScene.unity";
    public static bool IsLoading { get; private set; }
    public static string StartStageId { get; private set; }
    public static bool InLobby => Application.isPlaying && !IsLoading && SceneManager.GetActiveScene().path == LobbyScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { IsLoading = false; StartStageId = null; }

    public static bool EnterDungeon(string startStageId = null)
    {
        if (IsLoading) return false;
        string previous = StartStageId;
        StartStageId = startStageId;
        if (TryLoad(DungeonScene)) return true;
        StartStageId = previous;
        return false;
    }
    public static bool ReturnToLobby() => TryLoad(LobbyScene);
    public static bool RestartDungeon() => TryLoad(DungeonScene);

    private static bool TryLoad(string scenePath)
    {
        if (IsLoading)
            return false;
        if (!Application.CanStreamedLevelBeLoaded(scenePath))
        {
            Debug.LogError($"Register {scenePath} in the active Build Profile's Scene List.");
            return false;
        }
        IsLoading = true;
        try
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Single);
            if (operation == null)
            {
                IsLoading = false;
                return false;
            }
            operation.completed += _ => IsLoading = false;
            return true;
        }
        catch (Exception error)
        {
            IsLoading = false;
            Debug.LogException(error);
            return false;
        }
    }
}
