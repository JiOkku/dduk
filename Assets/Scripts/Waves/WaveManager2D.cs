using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// StageClear retains its legacy meaning: final dungeon clear. StageCompleted is intermediate.
public enum GameState2D { Playing, WaveTransition, GameOver, StageClear, StageCompleted, StageTransition, BossIntro, BossFight }

// Resolve the result after physics contacts and both player attacks (order 100).
[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
public sealed class WaveManager2D : MonoBehaviour
{
    [SerializeField] private EnemySpawner2D spawner;
    [SerializeField] private PlayerHealth2D playerHealth;
    [SerializeField] private PlayerGold2D playerGold;
    [SerializeField] private int[] enemiesPerWave = { 3, 5, 7 };
    [SerializeField, Min(0f)] private float transitionDuration = 3f;
    [SerializeField, Min(0)] private int goldPerKill = 5;
    [SerializeField, Min(0.1f)] private float announcementDuration = 2f;
    [SerializeField] private DungeonStageCatalog2D stageCatalog;
    [SerializeField, HideInInspector] private DungeonStageData2D[] stages; // Legacy scene fallback.
    private DungeonStageData2D[] StageList => stageCatalog != null ? stageCatalog.Stages : stages;
    [SerializeField] private SpriteRenderer environmentRenderer;
    private DungeonStageProgress2D progress;
    private int nextStageFrame;
    private float bossSpawnTime;
    private bool bossDefeated;
    private EnemyChase2D bossEnemy;
    private DungeonRewards2D rewards;
    public BossController2D CurrentBoss { get; private set; }
    public int StageStoneReward { get; private set; }
    public DungeonStageData2D CurrentStage => StageList != null && StageList.Length > 0 ? StageList[progress != null ? progress.Index : 0] : null;
    public int CurrentStageNumber => progress != null ? progress.Index + 1 : 1;
    public int TotalStages => StageList != null && StageList.Length > 0 ? StageList.Length : 1;
    public DungeonStageState2D StageState => progress != null ? progress.State : DungeonStageState2D.Preparing;
    public bool CanAdvance => State == GameState2D.StageCompleted && !playerHealth.IsDead && !SceneFlow2D.IsLoading && Time.frameCount >= nextStageFrame;
    private int WaveEnemyCount => CurrentStage != null ? CurrentStage.Waves[waveIndex].Count : enemiesPerWave[waveIndex];

    private readonly HashSet<EnemyChase2D> alive = new HashSet<EnemyChase2D>();
    private int waveIndex;
    private int spawnedThisWave;
    private float nextSpawnTime;
    private float nextWaveTime;
    private float announcementUntil;
    private string announcement;
    private bool initialized;

    public GameState2D State { get; private set; } = GameState2D.Playing;
    public int CurrentWave => initialized ? waveIndex + 1 : 0;
    public int TotalWaves => CurrentStage != null ? (CurrentStage.Waves != null ? CurrentStage.Waves.Length : 0) : enemiesPerWave != null ? enemiesPerWave.Length : 0;
    public int AliveEnemies => alive.Count;
    public int Kills { get; private set; }
    public int PendingEnemies => initialized && State != GameState2D.StageTransition ? Mathf.Max(0, WaveEnemyCount - spawnedThisWave) : 0;
    public int RemainingEnemies => PendingEnemies + AliveEnemies;
    public bool SpawnBlocked { get; private set; }
    public string Announcement => Time.time < announcementUntil ? announcement : "";

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.Died += HandlePlayerDeath;
    }

    private void Start()
    {
        rewards = FindAnyObjectByType<DungeonRewards2D>();
        bool valid = spawner != null && spawner.IsConfigured && playerHealth != null && playerGold != null && TotalWaves > 0;
        if (!valid)
            Debug.LogError($"Wave setup: spawner={spawner != null}, spawn settings={spawner != null && spawner.IsConfigured}, " +
                $"health={playerHealth != null}, gold={playerGold != null}, waves={TotalWaves}.", this);
        if (valid)
        {
            if (StageList != null && StageList.Length > 0)
            {
                var ids = new HashSet<string>();
                var bossIds = new Dictionary<string, BossData2D>();
                foreach (var stage in StageList)
                {
                    bool stageValid = stage != null && stage.Validate();
                    bool unique = stage != null && ids.Add(stage.Id);
                    if (!stageValid || !unique)
                        Debug.LogError($"Invalid stage: {(stage != null ? stage.name : "missing reference")}. Check wave groups, boss prefab, patterns and projectile references; unique ID={unique}.", this);
                    valid &= stageValid && unique;
                    if (stage != null && stage.Boss != null)
                    {
                        if (rewards == null) Debug.LogError("DungeonRewards2D is missing from the active dungeon scene.", this);
                        valid &= rewards != null;
                        if (bossIds.TryGetValue(stage.Boss.Id, out var existing) && existing != stage.Boss)
                        {
                            Debug.LogError("Different boss assets have the same ID: " + stage.Boss.Id, this);
                            valid = false;
                        }
                        else bossIds[stage.Boss.Id] = stage.Boss;
                    }
                }
            }
            else foreach (int count in enemiesPerWave) valid &= count > 0;
        }
        if (!valid)
        {
            Debug.LogError("Configure WaveManager2D references, active enemy prefab and positive wave counts.", this);
            enabled = false;
            return;
        }
        initialized = true;
        int startIndex = stageCatalog != null ? stageCatalog.FindIndex(SceneFlow2D.StartStageId) : 0;
        progress = new DungeonStageProgress2D(TotalStages, startIndex);
        progress.BeginFighting();
        ApplyStageEnvironment();
        BeginWave(0);
    }

    private void LateUpdate()
    {
        // Death wins if the final enemy and player die in the same frame.
        if (playerHealth.IsDead)
        {
            HandlePlayerDeath();
            return;
        }
        if (State == GameState2D.GameOver || State == GameState2D.StageClear ||
            State == GameState2D.StageCompleted || State == GameState2D.StageTransition)
            return;

        if (State == GameState2D.BossIntro)
        {
            if (Time.time >= bossSpawnTime) TrySpawnBoss();
            return;
        }
        if (State == GameState2D.BossFight)
        {
            if (bossDefeated) CompleteStage();
            return;
        }

        if (State == GameState2D.WaveTransition)
        {
            if (Time.time >= nextWaveTime)
                BeginWave(waveIndex + 1);
            return;
        }

        if (PendingEnemies > 0 && Time.time >= nextSpawnTime)
        {
            nextSpawnTime = Time.time + spawner.SpawnInterval;
            StageEnemyGroup2D group = CurrentStage != null ? CurrentStage.Waves[waveIndex].GroupAt(spawnedThisWave) : null;
            EnemyChase2D enemy;
            bool spawned = CurrentStage != null ? spawner.TrySpawn(group.prefab, CurrentStage, group.enemyData, out enemy) : spawner.TrySpawn(out enemy);
            if (spawned)
            {
                spawnedThisWave++;
                alive.Add(enemy);
                enemy.Died += HandleEnemyDeath;
                enemy.Removed += HandleEnemyRemoved;
                SpawnBlocked = false;
            }
            else
            {
                if (!SpawnBlocked)
                    Debug.LogWarning("No valid spawn position. Wave waits and retries at the spawn interval; check room bounds and clearance.", this);
                SpawnBlocked = true;
            }
        }

        if (RemainingEnemies == 0)
            CompleteWave();
    }

    private void BeginWave(int index)
    {
        waveIndex = index;
        spawnedThisWave = 0;
        SpawnBlocked = false;
        State = GameState2D.Playing;
        nextSpawnTime = Time.time;
        Announce($"Wave {CurrentWave} Start");
    }

    private void CompleteWave()
    {
        if (waveIndex + 1 == TotalWaves)
        {
            if (CurrentStage != null && CurrentStage.Boss != null)
            {
                if (!progress.BeginBoss()) return;
                bossDefeated = false;
                State = GameState2D.BossIntro;
                bossSpawnTime = Time.time + CurrentStage.BossIntroDuration;
                ClearProjectiles();
                Announce($"Boss Approaching: {CurrentStage.Boss.Name}");
            }
            else CompleteStage();
            return;
        }
        State = GameState2D.WaveTransition;
        nextWaveTime = Time.time + Mathf.Max(0f, transitionDuration);
        Announce($"Wave {CurrentWave} Complete");
    }

    private void TrySpawnBoss()
    {
        bossSpawnTime = Time.time + spawner.SpawnInterval;
        if (!spawner.TrySpawn(CurrentStage.Boss.Prefab, null, null, out var enemy))
        {
            if (!SpawnBlocked) Debug.LogWarning("Boss spawn blocked; retrying at the spawn interval.", this);
            SpawnBlocked = true;
            return;
        }
        SpawnBlocked = false;
        bossEnemy = enemy;
        CurrentBoss = enemy.GetComponent<BossController2D>();
        CurrentBoss.Initialize(CurrentStage.Boss, playerHealth, CurrentStage.BossHealthMultiplier, CurrentStage.BossDamageMultiplier);
        alive.Add(enemy);
        enemy.Died += HandleBossDeath;
        enemy.Removed += HandleEnemyRemoved;
        State = GameState2D.BossFight;
        Announce($"Boss Fight: {CurrentStage.Boss.Name}");
    }
    private void HandleBossDeath(EnemyChase2D enemy)
    {
        if (bossDefeated || !alive.Remove(enemy)) return;
        Unsubscribe(enemy);
        bossDefeated = true;
        if (!playerHealth.IsDead) Kills++;
    }
    private void CompleteStage()
    {
        if (!progress.Complete(playerHealth.IsDead, bossDefeated)) return;
        StageStoneReward = 0;
        if (CurrentStage != null)
        {
            playerGold.Award(CurrentStage.ClearGold);
            if (CurrentStage.Boss != null)
            {
                int previous = rewards.PendingMagicStones;
                var boss = CurrentStage.Boss;
                rewards.TryRegisterBossKill(CurrentStage.Id, boss.Id, boss.FirstKillStones, boss.RepeatKillStones);
                StageStoneReward = rewards.PendingMagicStones - previous;
            }
        }
        State = progress.State == DungeonStageState2D.DungeonCleared ? GameState2D.StageClear : GameState2D.StageCompleted;
        playerHealth.StopForStageClear();
        ClearProjectiles();
        nextStageFrame = Time.frameCount + 1;
        Announce(bossDefeated ? "Boss Defeated" : $"Wave {CurrentWave} Complete");
    }

    private void HandleEnemyDeath(EnemyChase2D enemy)
    {
        if (!alive.Remove(enemy))
            return;
        Unsubscribe(enemy);
        if (State == GameState2D.Playing && !playerHealth.IsDead)
        {
            Kills++;
            playerGold.Award(Mathf.Max(0, goldPerKill));
        }
    }

    private void HandleEnemyRemoved(EnemyChase2D enemy)
    {
        if (!alive.Remove(enemy))
            return;
        Unsubscribe(enemy);
        if (object.ReferenceEquals(enemy, bossEnemy) && !bossDefeated && State == GameState2D.BossFight)
        {
            CurrentBoss = null; bossEnemy = null;
            State = GameState2D.BossIntro;
            bossSpawnTime = Time.time + spawner.SpawnInterval;
            return;
        }
        // An external Destroy is not a kill: replace it without awarding gold.
        if (State == GameState2D.Playing)
            spawnedThisWave = Mathf.Max(0, spawnedThisWave - 1);
    }

    private void HandlePlayerDeath()
    {
        if (State == GameState2D.StageClear || State == GameState2D.GameOver)
            return;
        State = GameState2D.GameOver;
        progress?.Die();
        SpawnBlocked = false;
        announcement = "";
        playerGold?.ResetGold();
    }

    public bool TryNextStage()
    {
        if (!CanAdvance || progress == null || !progress.Next()) return false;
        State = GameState2D.StageTransition;
        StartCoroutine(AdvanceStage());
        return true;
    }

    private IEnumerator AdvanceStage()
    {
        // Deactivate immediately, then wait for deferred Destroy before accepting spawns.
        ClearProjectiles();
        foreach (var item in new List<WeaponPickup2D>(WeaponPickup2D.Active)) if (item != null) item.Consume();
        foreach (var enemy in FindObjectsByType<EnemyChase2D>())
        {
            Unsubscribe(enemy);
            enemy.gameObject.SetActive(false);
            Destroy(enemy.gameObject); // Removal is not death: no drop, gold or mana.
        }
        alive.Clear();
        CurrentBoss = null; bossEnemy = null; bossDefeated = false; StageStoneReward = 0;
        foreach (var chest in FindObjectsByType<TreasureChest2D>()) chest.ResetForStage();
        yield return null;
        if (playerHealth.IsDead || !isActiveAndEnabled) yield break;
        ApplyStageEnvironment();
        progress.BeginFighting();
        BeginWave(0);
        playerHealth.ResumeAfterStage();
    }

    private void ApplyStageEnvironment()
    {
        if (CurrentStage != null && environmentRenderer != null) environmentRenderer.color = CurrentStage.EnvironmentColor;
    }

    private static void ClearProjectiles()
    {
        foreach (var projectile in FindObjectsByType<Projectile2D>())
        { projectile.gameObject.SetActive(false); Destroy(projectile.gameObject); }
    }

    private void Announce(string message)
    {
        announcement = message;
        announcementUntil = Time.time + Mathf.Max(0.1f, announcementDuration);
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.Died -= HandlePlayerDeath;
    }

    private void OnDestroy()
    {
        foreach (EnemyChase2D enemy in alive)
            if (enemy != null)
                Unsubscribe(enemy);
        alive.Clear();
    }

    private void Unsubscribe(EnemyChase2D enemy)
    {
        enemy.Died -= HandleEnemyDeath;
        enemy.Died -= HandleBossDeath;
        enemy.Removed -= HandleEnemyRemoved;
    }
}
