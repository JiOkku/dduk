using UnityEngine;

[DefaultExecutionOrder(250)]
[DisallowMultipleComponent]
public sealed class DungeonSession2D : MonoBehaviour
{
    [SerializeField] private PlayerHealth2D playerHealth;
    [SerializeField] private PlayerMana2D playerMana;
    [SerializeField] private PlayerGold2D playerGold;
    [SerializeField] private WaveManager2D waves;
    [SerializeField] private DungeonRewards2D rewards;
    public DungeonRunResult2D Result { get; private set; }
    public DungeonRewards2D Rewards => rewards;

    private void Start()
    {
        if (playerHealth == null || playerMana == null || playerGold == null || waves == null)
        {
            Debug.LogError("Connect dungeon Player Health, Mana, Gold and Waves.", this);
            enabled = false;
            return;
        }
        // Other scene-owned components initialize in Awake; a new run always starts full.
        playerMana.RefillForRun();
    }

    private void LateUpdate()
    {
        if (Result != null)
            return;
        if (playerHealth.IsDead || waves.State == GameState2D.GameOver || waves.State == GameState2D.StageClear)
        {
            Result = new DungeonRunResult2D(waves.Kills, waves.CurrentWave, playerGold.EarnedGold,
                !playerHealth.IsDead && waves.State == GameState2D.StageClear, waves.CurrentStageNumber);
            if (rewards != null) rewards.Finish(Result.Cleared, Result.EarnedGold);
        }
    }
}
