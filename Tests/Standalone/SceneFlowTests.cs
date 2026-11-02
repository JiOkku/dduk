// Compile with production SceneFlow2D, DungeonSession2D and DungeonRunResult2D only.
// Doubles verify flow guards and result snapshots, not real Unity loads/UI/physics.
using System;
using System.Reflection;

namespace UnityEngine
{
    public class MonoBehaviour { public bool enabled = true; }
    public sealed class SerializeField : Attribute { }
    public sealed class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order) { } }
    public sealed class DisallowMultipleComponent : Attribute { }
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { } }
    public class AsyncOperation
    {
        public event Action<AsyncOperation> completed;
        public void Complete() => completed?.Invoke(this);
    }
    public static class Application
    {
        public static bool Available = true;
        public static bool CanStreamedLevelBeLoaded(string path) => Available;
    }
    public static class Debug
    {
        public static void LogError(string value, object context = null) { }
        public static void LogException(Exception value) { }
    }
}
namespace UnityEngine.SceneManagement
{
    public enum LoadSceneMode { Single, Additive }
    public static class SceneManager
    {
        public static int Loads;
        public static string Path;
        public static LoadSceneMode Mode;
        public static bool Throw;
        public static bool ReturnNull;
        public static UnityEngine.AsyncOperation Pending;
        public static UnityEngine.AsyncOperation LoadSceneAsync(string path, LoadSceneMode mode)
        {
            Loads++; Path = path; Mode = mode;
            if (Throw) throw new InvalidOperationException("simulated load failure");
            Pending = ReturnNull ? null : new UnityEngine.AsyncOperation();
            return Pending;
        }
    }
}
public enum GameState2D { Playing, WaveTransition, GameOver, StageClear }
public sealed class PlayerHealth2D { public bool IsDead; }
public sealed class PlayerMana2D
{
    public int CurrentMana;
    public int MaxMana = 100;
    public void RefillForRun() => CurrentMana = MaxMana;
}
public sealed class PlayerGold2D { public int CurrentGold; public int EarnedGold; }
public sealed class WaveManager2D { public int Kills; public int CurrentWave = 1; public GameState2D State; }
public sealed class DungeonRewards2D
{
    public int FinishCalls;
    public void Finish(bool cleared, int gold) => FinishCalls++;
}

public static class SceneFlowTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Call(object target, string name) =>
        target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static void ResetFlow()
    {
        typeof(SceneFlow2D).GetMethod("Reset", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        UnityEngine.Application.Available = true;
        UnityEngine.SceneManagement.SceneManager.Loads = 0;
        UnityEngine.SceneManagement.SceneManager.Throw = false;
        UnityEngine.SceneManagement.SceneManager.ReturnNull = false;
    }
    private sealed class SessionFixture
    {
        public readonly DungeonSession2D Session = new DungeonSession2D();
        public readonly PlayerHealth2D Health = new PlayerHealth2D();
        public readonly PlayerMana2D Mana = new PlayerMana2D();
        public readonly PlayerGold2D Gold = new PlayerGold2D();
        public readonly WaveManager2D Waves = new WaveManager2D();
        public readonly DungeonRewards2D Rewards = new DungeonRewards2D();
        public SessionFixture()
        {
            Set(Session, "playerHealth", Health); Set(Session, "playerMana", Mana);
            Set(Session, "playerGold", Gold); Set(Session, "waves", Waves);
            Set(Session, "rewards", Rewards);
        }
    }
    public static int Main()
    {
        Action[] tests = { DuplicateLoadsRejected, FailedLoadsReleaseGuard, ScenePathsUseSingleLoad,
            EntryRefillsMana, DeathResultFreezesEarnedGold, ClearResultCaptured };
        try
        {
            foreach (Action test in tests) { test(); Console.WriteLine("PASS " + test.Method.Name); }
            Console.WriteLine("6 flow/result tests passed with simulated dependencies. No real Unity scene/UI execution.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static void DuplicateLoadsRejected()
    {
        ResetFlow();
        Check(SceneFlow2D.EnterDungeon() && SceneFlow2D.IsLoading, "First load rejected.");
        Check(!SceneFlow2D.ReturnToLobby() && UnityEngine.SceneManagement.SceneManager.Loads == 1, "Duplicate load accepted.");
        UnityEngine.SceneManagement.SceneManager.Pending.Complete();
        Check(!SceneFlow2D.IsLoading && SceneFlow2D.ReturnToLobby(), "Completed load did not restore input gate.");
    }
    private static void FailedLoadsReleaseGuard()
    {
        ResetFlow(); UnityEngine.Application.Available = false;
        Check(!SceneFlow2D.EnterDungeon() && !SceneFlow2D.IsLoading, "Missing scene left gate locked.");
        UnityEngine.Application.Available = true; UnityEngine.SceneManagement.SceneManager.Throw = true;
        Check(!SceneFlow2D.EnterDungeon() && !SceneFlow2D.IsLoading, "Exception left gate locked.");
        UnityEngine.SceneManagement.SceneManager.Throw = false; UnityEngine.SceneManagement.SceneManager.ReturnNull = true;
        Check(!SceneFlow2D.EnterDungeon() && !SceneFlow2D.IsLoading, "Null operation left gate locked.");
    }
    private static void ScenePathsUseSingleLoad()
    {
        ResetFlow(); SceneFlow2D.RestartDungeon();
        Check(UnityEngine.SceneManagement.SceneManager.Path == SceneFlow2D.DungeonScene &&
            UnityEngine.SceneManagement.SceneManager.Mode == UnityEngine.SceneManagement.LoadSceneMode.Single, "Dungeon restart must replace scene.");
        UnityEngine.SceneManagement.SceneManager.Pending.Complete(); SceneFlow2D.ReturnToLobby();
        Check(UnityEngine.SceneManagement.SceneManager.Path == SceneFlow2D.LobbyScene, "Lobby path incorrect.");
    }
    private static void EntryRefillsMana()
    {
        var f = new SessionFixture(); f.Mana.CurrentMana = 10; Call(f.Session, "Start");
        Check(f.Mana.CurrentMana == 100 && f.Session.Result == null, "New run did not refill mana or clear result.");
    }
    private static void DeathResultFreezesEarnedGold()
    {
        var f = new SessionFixture(); f.Health.IsDead = true; f.Waves.State = GameState2D.StageClear;
        f.Waves.Kills = 2; f.Waves.CurrentWave = 2; f.Gold.CurrentGold = 0; f.Gold.EarnedGold = 10;
        Call(f.Session, "LateUpdate"); var result = f.Session.Result;
        Check(!result.Cleared && result.Kills == 2 && result.ReachedWave == 2 && result.EarnedGold == 10,
            "Death result must use earned gold and death priority.");
        f.Gold.EarnedGold = 99; f.Waves.Kills = 99; Call(f.Session, "LateUpdate");
        Check(ReferenceEquals(result, f.Session.Result) && result.EarnedGold == 10 && f.Rewards.FinishCalls == 1,
            "Result changed or rewards finalized twice.");
    }
    private static void ClearResultCaptured()
    {
        var f = new SessionFixture(); f.Waves.State = GameState2D.StageClear; f.Waves.Kills = 15;
        f.Waves.CurrentWave = 3; f.Gold.EarnedGold = 75; Call(f.Session, "LateUpdate");
        Check(f.Session.Result.Cleared && f.Session.Result.Kills == 15 && f.Session.Result.EarnedGold == 75,
            "Clear summary incorrect.");
    }
}
