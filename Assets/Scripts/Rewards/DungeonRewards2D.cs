using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DungeonRewards2D : MonoBehaviour
{
    [SerializeField] private PlayerHealth2D playerHealth;
    [SerializeField] private PlayerWeaponLoadout2D loadout;
    [SerializeField] private RewardSettlementPolicy2D policy;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [SerializeField, Min(0)] private int developmentBossCurrency = 20;
#endif
    private readonly List<RewardRecord2D> records = new List<RewardRecord2D>();
    private readonly List<RewardRecord2D> exportBatch = new List<RewardRecord2D>();
    private readonly HashSet<string> rewardIds = new HashSet<string>();
    private string runId;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private bool bossGranted;
#endif
    private float noticeUntil;
    private string notice;
    public bool Finished { get; private set; }
    public bool NeedsSaveRetry { get; private set; }
    public bool ExportSucceeded { get; private set; }
    public bool CanAcquire => isActiveAndEnabled && !Finished && playerHealth != null && !playerHealth.CombatStopped;
    public string Notice => Time.time < noticeUntil ? notice : "";
    public int PendingMagicStones
    {
        get { int total = 0; foreach (var record in records) if (record.kind == RewardKind2D.MagicStone) total += record.quantity; return total; }
    }

    public bool TryRegisterBossKill(string eventId, string bossId, int first, int repeat)
    {
        if (string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(bossId)) return false;
        bool defeated = RewardBank2D.Snapshot().defeatedBossIds.Contains(bossId);
        foreach (var record in records)
            if (record.kind == RewardKind2D.MagicStone && record.bossId == bossId) defeated = true;
        var reward = RewardRecord2D.BossStones(eventId, bossId, first, repeat, defeated);
        return TryAddBatch(new[] { reward }, $"Boss defeated! Magic Stones +{reward.quantity} ({(defeated ? "repeat" : "first")}, export on dungeon clear)");
    }

    private void Awake() => runId = Guid.NewGuid().ToString("N");
    private void OnEnable()
    {
        if (loadout != null) loadout.WeaponEquipped += RegisterTemporaryWeapon;
    }
    private void OnDisable()
    {
        if (loadout != null) loadout.WeaponEquipped -= RegisterTemporaryWeapon;
    }
    private void RegisterTemporaryWeapon(WeaponInstance2D instance)
    {
        if (!Finished && instance != null && instance.IsValid && instance.Source == RewardSource2D.Monster &&
            rewardIds.Add(instance.InstanceId)) records.Add(RewardRecord2D.FromWeapon(instance));
    }

    public bool TryAddBatch(IList<RewardRecord2D> batch, string message)
    {
        if (!CanAcquire || batch == null) return false;
        var incomingIds = new HashSet<string>();
        foreach (RewardRecord2D record in batch)
            if (record == null || string.IsNullOrEmpty(record.rewardId) || record.quantity < 0 ||
                (record.quantity == 0 && record.kind != RewardKind2D.MagicStone) ||
                rewardIds.Contains(record.rewardId) || !incomingIds.Add(record.rewardId)) return false;
        foreach (RewardRecord2D record in batch) { rewardIds.Add(record.rewardId); records.Add(record); }
        notice = message;
        noticeUntil = Time.time + 3f;
        return true;
    }

    public void Finish(bool cleared, int temporaryGold)
    {
        if (Finished) return;
        if (loadout != null) RegisterTemporaryWeapon(loadout.CurrentWeapon);
        if (temporaryGold > 0) records.Add(RewardRecord2D.TemporaryGold(temporaryGold));
        Finished = true;
        bool allowed = policy != null ? policy.AllowsExport(cleared) : cleared;
        if (!allowed) return;
        foreach (RewardRecord2D record in records) if (record.CanExport) exportBatch.Add(record);
        NeedsSaveRetry = true;
        RetrySave();
    }

    public void RetrySave()
    {
        if (!Finished || !NeedsSaveRetry) return;
        ExportSucceeded = RewardBank2D.TryCommit(runId, exportBatch);
        NeedsSaveRetry = !ExportSucceeded;
        if (NeedsSaveRetry) Debug.LogWarning(RewardBank2D.Error, this);
    }

    public string Summary()
    {
        int tempWeapons = 0, tempGold = 0, lostTokens = 0, lostWeapons = 0, tokens = 0, weapons = 0;
        int stones = 0;
        foreach (RewardRecord2D record in records)
        {
            if (!record.CanExport)
            {
                if (record.kind == RewardKind2D.Weapon) tempWeapons += record.quantity;
                if (record.kind == RewardKind2D.TemporaryGold) tempGold += record.quantity;
                continue;
            }
            if (record.kind == RewardKind2D.MagicStone) { stones += record.quantity; continue; }
            if (ExportSucceeded || NeedsSaveRetry)
            { if (record.kind == RewardKind2D.Currency) tokens += record.quantity; else weapons += record.quantity; }
            else
            { if (record.kind == RewardKind2D.Currency) lostTokens += record.quantity; else lostWeapons += record.quantity; }
        }
        var text = new StringBuilder();
        text.AppendLine($"Magic Stones: {stones} ({(NeedsSaveRetry ? "pending save" : ExportSucceeded ? "exported" : "lost")})");
        if (NeedsSaveRetry)
            text.AppendLine($"NOT SAVED: Tokens {tokens}, Weapons {weapons}. Retry Save.\nRestart discards these pending rewards.");
        else
            text.AppendLine($"Exported: Tokens {tokens}, Weapons {weapons}\nLost export rewards: Tokens {lostTokens}, Weapons {lostWeapons}");
        text.Append($"Discarded temporary: Gold {tempGold}, Monster weapons {tempWeapons}");
        var names = new List<string>();
        int weaponRewards = 0;
        foreach (RewardRecord2D record in records)
            if (record.CanExport && record.kind == RewardKind2D.Weapon)
            {
                weaponRewards++;
                if (names.Count < 3) names.Add($"{record.name} [{record.source}]");
            }
        if (names.Count > 0)
            text.Append("\nReward weapons: " + string.Join(", ", names.ToArray()) + (weaponRewards > 3 ? $" (+{weaponRewards - 3} more)" : ""));
        if (NeedsSaveRetry) text.Append("\n" + RewardBank2D.Error);
        return text.ToString();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [ContextMenu("Development/Grant Boss Reward Once")]
    private void GrantBossReward()
    {
        if (!Application.isPlaying || bossGranted || developmentBossCurrency <= 0) return;
        if (TryAddBatch(new[] { RewardRecord2D.Currency(developmentBossCurrency, RewardSource2D.Boss) },
            $"Boss test reward: Export Tokens +{developmentBossCurrency}")) bossGranted = true;
    }
#endif
}
