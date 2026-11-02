// Compile ONLY with WaveManager2D.cs and PlayerGold2D.cs outside Unity.
// These minimal doubles test production progression/reward code, not Unity physics or lifecycle.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine
{
    public class MonoBehaviour
    {
        public bool enabled = true;
        public bool isActiveAndEnabled => enabled;
        public object Component;
        public T GetComponent<T>() => (T)Component;
    }
    public sealed class SerializeField : Attribute { }
    public sealed class MinAttribute : Attribute { public MinAttribute(float value) { } }
    public sealed class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order) { } }
    public sealed class DisallowMultipleComponent : Attribute { }
    public sealed class RequireComponent : Attribute { public RequireComponent(Type type) { } }
    public static class Time { public static float time; }
    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Min(int a, int b) => Math.Min(a, b);
    }
    public static class Debug
    {
        public static void LogError(string message, object context) { }
        public static void LogWarning(string message, object context) { }
    }
}

public sealed class PlayerHealth2D
{
    public bool IsDead;
    public bool Cleared;
    public bool CombatStopped => IsDead || Cleared;
    public event Action Died;
    public void StopForStageClear() => Cleared = true;
    public void Die() { IsDead = true; Died?.Invoke(); }
}
public sealed class EnemyChase2D
{
    public bool Killed;
    public event Action<EnemyChase2D> Died;
    public event Action<EnemyChase2D> Removed;
    public void Kill() { Killed = true; Died?.Invoke(this); }
    public void Remove() => Removed?.Invoke(this);
}
public sealed class EnemySpawner2D
{
    public bool IsConfigured = true;
    public bool CanSpawn = true;
    public float SpawnInterval = 1f;
    public int Calls;
    public readonly List<EnemyChase2D> Created = new List<EnemyChase2D>();
    public bool TrySpawn(out EnemyChase2D enemy)
    {
        Calls++;
        enemy = CanSpawn ? new EnemyChase2D() : null;
        if (enemy != null) Created.Add(enemy);
        return enemy != null;
    }
}

public static class WaveProgressionTests
{
    private sealed class Fixture
    {
        public readonly WaveManager2D Waves = new WaveManager2D();
        public readonly PlayerHealth2D Health = new PlayerHealth2D();
        public readonly PlayerGold2D Gold = new PlayerGold2D();
        public readonly EnemySpawner2D Spawner = new EnemySpawner2D();
        public EnemyChase2D Last => Spawner.Created[Spawner.Created.Count - 1];
        public Fixture(params int[] counts)
        {
            UnityEngine.Time.time = 0f;
            Gold.Component = Health;
            Call(Gold, "Awake");
            Call(Gold, "OnEnable");
            Set(Waves, "spawner", Spawner);
            Set(Waves, "playerHealth", Health);
            Set(Waves, "playerGold", Gold);
            Set(Waves, "enemiesPerWave", counts);
            Call(Waves, "OnEnable");
            Call(Waves, "Start");
        }
        public void Tick(float time) { UnityEngine.Time.time = time; Call(Waves, "LateUpdate"); }
    }

    private static void Set(object owner, string field, object value) =>
        owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
    private static void Call(object owner, string method) =>
        owner.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(owner, null);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    public static int Main()
    {
        var tests = new Action[] { PendingPreventsEarlyCompletion, TransitionWaitsThreeSeconds,
            ThreeWavesYield75Gold, DuplicateDeathGivesOneReward, DeathStopsAndResets,
            SameFrameDeathWins, SpawnFailureRetriesSafely, ExternalRemovalIsReplaced };
        try
        {
            foreach (Action test in tests) { test(); Console.WriteLine("PASS " + test.Method.Name); }
            Console.WriteLine("8 progression/reward tests passed with simulated dependencies. No Unity Play/physics/UI execution.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void PendingPreventsEarlyCompletion()
    {
        var f = new Fixture(3, 5, 7);
        f.Tick(0f); f.Last.Kill(); f.Tick(0f);
        Check(f.Waves.State == GameState2D.Playing && f.Waves.RemainingEnemies == 2 && f.Waves.PendingEnemies == 2,
            "Killing the first enemy must not complete a wave with unspawned enemies.");
    }

    private static void TransitionWaitsThreeSeconds()
    {
        var f = new Fixture(1, 1);
        f.Tick(0f); f.Last.Kill(); f.Tick(0f);
        Check(f.Waves.State == GameState2D.WaveTransition, "Expected transition.");
        f.Tick(2.99f);
        Check(f.Waves.CurrentWave == 1 && f.Spawner.Calls == 1, "Must wait full transition.");
        f.Tick(3f);
        Check(f.Waves.CurrentWave == 2 && f.Waves.State == GameState2D.Playing, "Next wave did not start.");
    }

    private static void ThreeWavesYield75Gold()
    {
        var f = new Fixture(3, 5, 7);
        for (int t = 0; t < 100 && f.Waves.State != GameState2D.StageClear; t++)
        {
            f.Tick(t);
            foreach (EnemyChase2D enemy in f.Spawner.Created)
                if (!enemy.Killed) enemy.Kill();
            f.Tick(t);
        }
        Check(f.Waves.State == GameState2D.StageClear && f.Spawner.Created.Count == 15 && f.Gold.CurrentGold == 75 &&
            f.Gold.EarnedGold == 75 && f.Waves.Kills == 15,
            "Three-wave totals or clear state incorrect.");
        Check(f.Health.Cleared && !f.Health.IsDead, "Clear must stop combat without death.");
        f.Tick(100f);
        Check(f.Spawner.Created.Count == 15, "Cannot spawn after clear.");
    }

    private static void DuplicateDeathGivesOneReward()
    {
        var f = new Fixture(3);
        f.Tick(0f); f.Last.Kill(); f.Last.Kill(); f.Last.Remove();
        Check(f.Gold.CurrentGold == 5 && f.Gold.EarnedGold == 5 && f.Waves.Kills == 1 && f.Waves.RemainingEnemies == 2,
            "Duplicate death/removal changed reward or count.");
    }

    private static void DeathStopsAndResets()
    {
        var f = new Fixture(3);
        f.Tick(0f); f.Last.Kill();
        Check(f.Gold.CurrentGold == 5, "Expected reward before death.");
        f.Health.Die(); f.Tick(100f);
        Check(f.Waves.State == GameState2D.GameOver && f.Gold.CurrentGold == 0 && f.Gold.EarnedGold == 5 && f.Spawner.Calls == 1,
            "Death must reset gold and stop spawning.");
        f.Gold.Award(5);
        Check(f.Gold.CurrentGold == 0, "Cannot award after death.");
        var restarted = new Fixture(3, 5, 7);
        Check(restarted.Gold.CurrentGold == 0 && restarted.Waves.CurrentWave == 1, "Fresh run did not initialize.");
    }

    private static void SameFrameDeathWins()
    {
        var f = new Fixture(1);
        f.Tick(0f); f.Last.Kill(); f.Health.Die(); f.Tick(0f);
        Check(f.Waves.State == GameState2D.GameOver && !f.Health.Cleared && f.Gold.CurrentGold == 0,
            "Death must win over final kill before result resolution.");
    }

    private static void SpawnFailureRetriesSafely()
    {
        var f = new Fixture(3);
        f.Spawner.CanSpawn = false;
        f.Tick(0f); f.Tick(0f); f.Tick(0.5f);
        Check(f.Spawner.Calls == 1 && f.Waves.PendingEnemies == 3 && f.Waves.SpawnBlocked, "Failed spawn must wait, not count or loop.");
        f.Tick(1f);
        Check(f.Spawner.Calls == 2 && f.Waves.State == GameState2D.Playing, "Retry timing incorrect.");
        f.Spawner.CanSpawn = true; f.Tick(2f);
        Check(f.Waves.AliveEnemies == 1 && f.Waves.PendingEnemies == 2 && !f.Waves.SpawnBlocked, "Spawn did not recover.");
    }

    private static void ExternalRemovalIsReplaced()
    {
        var f = new Fixture(3);
        f.Tick(0f); f.Last.Remove();
        Check(f.Waves.PendingEnemies == 3 && f.Gold.CurrentGold == 0, "External removal must not count as a kill.");
        f.Tick(1f);
        Check(f.Spawner.Created.Count == 2 && f.Waves.RemainingEnemies == 3, "Removed enemy was not replaced.");
    }
}
