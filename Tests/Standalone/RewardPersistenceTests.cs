// Actual reward/store/instance/ledger code with Unity API doubles and real temporary-file I/O.
// Does not execute Unity chest physics, UI, or scene reloads.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace UnityEngine
{
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType value) { } }
    public static class JsonUtility
    {
        private static JsonSerializerOptions Options(bool pretty) => new JsonSerializerOptions
        { IncludeFields = true, IgnoreReadOnlyProperties = true, WriteIndented = pretty };
        public static string ToJson(object value, bool pretty = false) => JsonSerializer.Serialize(value, value.GetType(), Options(pretty));
        public static T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, Options(false));
    }
}

public static class RewardPersistenceTests
{
    private static string root;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Set(object target, string name, object value) => target.GetType().GetField(name,
        BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static void Call(object target, string name) => target.GetType().GetMethod(name,
        BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
    private static string FilePath(string name) => Path.Combine(root, name, "rewards.json");
    private static WeaponPartData2D Definition()
    {
        var data = new WeaponPartData2D(); Set(data, "weaponName", "Spear"); Set(data, "weaponId", "spear");
        Set(data, "range", 2.5f); Set(data, "damage", 2); return data;
    }
    private static RewardRecord2D Tokens(int quantity, RewardSource2D source = RewardSource2D.Chest) => RewardRecord2D.Currency(quantity, source);
    private sealed class LedgerFixture
    {
        public readonly DungeonRewards2D Ledger = new DungeonRewards2D();
        public readonly PlayerHealth2D Health = new PlayerHealth2D();
        public readonly PlayerWeaponLoadout2D Loadout = new PlayerWeaponLoadout2D();
        public LedgerFixture(string name)
        {
            UnityEngine.Application.persistentDataPath = Path.Combine(root, name);
            typeof(RewardBank2D).GetMethod("ResetCache", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            Loadout.Component = Health; Set(Loadout, "club", Definition()); Call(Loadout, "Awake");
            Set(Ledger, "playerHealth", Health); Set(Ledger, "loadout", Loadout);
            Set(Ledger, "policy", new RewardSettlementPolicy2D()); Call(Ledger, "Awake"); Call(Ledger, "OnEnable");
        }
    }
    [Serializable] public sealed class TestEnvelope { public int version; public string payload; public string sha256; }

    public static int Main()
    {
        root = Path.Combine(Path.GetTempPath(), "dduk-reward-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Action[] tests = { SourcesCannotBeLaundered, SaveReloadExactWeaponStats, DuplicateRunCannotPayTwice,
            FailedWritePreservesPreviousAndRetry, CorruptionPreservesMainAndBackup, ChecksumRejectsAlteredPayload,
            DeathAndRestartDiscardPending, ClearSettlesOnceAndExcludesTemporary, BossGrantOnce,
            LegacySaveAddsMagicWithoutLosingRewards, MagicSelectionSurvivesColdReload, FailedMagicSaveRetainsSelection,
            RewardSettlementPreservesSelectedMagic, BossFirstRepeatAndColdRestore, BossDeathAndRestartDoNotRecordFirst,
            BossSaveFailureIsAtomic, BossLedgerDeduplicatesAndSupportsSharedId, ZeroBossRewardStillRecordsClear };
        try
        {
            foreach (Action test in tests) { test(); Console.WriteLine("PASS " + test.Method.Name); }
            Console.WriteLine("18 reward/magic/boss persistence tests passed with real temp-file I/O and Unity doubles.");
            Console.WriteLine("No actual persistentDataPath save, Unity chest/input/UI/scene execution.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static void SourcesCannotBeLaundered()
    {
        var data = Definition(); var health = new PlayerHealth2D();
        var loadout = new PlayerWeaponLoadout2D { Component = health };
        Set(loadout, "club", data); Call(loadout, "Awake");
        var monster = WeaponInstance2D.Create(data, 1.1f, 0.9f, 1.05f, 1.2f);
        loadout.TryEquipWeapon(monster); loadout.TryEquipWeapon(WeaponInstance2D.CreateBase(data)); loadout.TryEquipWeapon(monster);
        Check(loadout.CurrentWeapon.Source == RewardSource2D.Monster && !loadout.CurrentWeapon.Exportable, "Swap changed provenance.");
        var record = RewardRecord2D.FromWeapon(monster); record.exportable = true;
        Check(!record.CanExport && !RewardRules2D.CanExport(RewardKind2D.TemporaryGold, RewardSource2D.Chest), "Source/type policy bypassed.");
        var store = new RewardSaveStore2D(FilePath("monster"));
        Check(!store.TryCommit("run", new[] { record }) && store.Snapshot().weapons.Count == 0, "Monster weapon exported.");
    }
    private static void SaveReloadExactWeaponStats()
    {
        string path = FilePath("restore"); var definition = Definition();
        var item = WeaponInstance2D.Create(definition, 1.2f, 0.87f, 1.09f, 1.1f, RewardSource2D.Chest);
        var store = new RewardSaveStore2D(path);
        Check(store.TryCommit("run-1", new[] { Tokens(10), RewardRecord2D.FromWeapon(item) }), "Initial save failed: " + store.Error);
        var restoredProfile = new RewardSaveStore2D(path).Snapshot();
        var restored = WeaponInstance2D.Restore(restoredProfile.weapons[0], definition);
        Check(restoredProfile.currency == 10 && restored.InstanceId == item.InstanceId && restored.Source == RewardSource2D.Chest &&
            restored.Damage == item.Damage && restored.AttackInterval == item.AttackInterval && restored.Range == item.Range &&
            restored.ManaOnHit == item.ManaOnHit, "Cold restore changed identity/stats/source.");
    }
    private static void DuplicateRunCannotPayTwice()
    {
        string path = FilePath("dedupe"); var rewards = new[] { Tokens(10) };
        var store = new RewardSaveStore2D(path); Check(store.TryCommit("same-run", rewards), "First grant failed.");
        string before = File.ReadAllText(path);
        var cold = new RewardSaveStore2D(path);
        Check(cold.TryCommit("same-run", rewards) && cold.Snapshot().currency == 10 && File.ReadAllText(path) == before,
            "Repeated/cold settlement paid twice or rewrote file.");
    }
    private static void FailedWritePreservesPreviousAndRetry()
    {
        string path = FilePath("failure"); var store = new RewardSaveStore2D(path);
        Check(store.TryCommit("first", new[] { Tokens(10) }), "Setup failed.");
        string before = File.ReadAllText(path);
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Check(!store.TryCommit("second", new[] { Tokens(20) }) && store.Snapshot().currency == 10,
                "Locked save must fail without granting in memory.");
        Check(File.ReadAllText(path) == before, "Failure changed previous save.");
        Check(store.TryCommit("second", new[] { Tokens(20) }) && store.Snapshot().currency == 30 && File.Exists(path + ".bak"),
            "Retry or backup failed.");
    }
    private static void CorruptionPreservesMainAndBackup()
    {
        string path = FilePath("corrupt"); var store = new RewardSaveStore2D(path);
        Check(store.TryCommit("one", new[] { Tokens(10) }) && store.TryCommit("two", new[] { Tokens(20) }), "Setup failed.");
        string backup = File.ReadAllText(path + ".bak"); File.WriteAllText(path, "broken");
        var damaged = new RewardSaveStore2D(path);
        Check(damaged.ReadOnly && damaged.Snapshot().currency == 10 && !damaged.TryCommit("three", new[] { Tokens(5) }) &&
            File.ReadAllText(path) == "broken" && File.ReadAllText(path + ".bak") == backup, "Corruption recovery overwrote originals.");
    }
    private static void ChecksumRejectsAlteredPayload()
    {
        string path = FilePath("checksum"); var store = new RewardSaveStore2D(path);
        Check(store.TryCommit("one", new[] { Tokens(10) }), "Setup failed.");
        var envelope = UnityEngine.JsonUtility.FromJson<TestEnvelope>(File.ReadAllText(path));
        envelope.payload += " "; File.WriteAllText(path, UnityEngine.JsonUtility.ToJson(envelope));
        Check(new RewardSaveStore2D(path).ReadOnly, "Changed payload/checksum accepted.");
    }
    private static void DeathAndRestartDiscardPending()
    {
        var f = new LedgerFixture("death");
        Check(f.Ledger.TryAddBatch(new[] { Tokens(10) }, "chest"), "Acquisition failed.");
        f.Health.CombatStopped = true; f.Ledger.Finish(false, 5);
        Check(!f.Ledger.ExportSucceeded && !f.Ledger.NeedsSaveRetry && RewardBank2D.Snapshot().currency == 0 &&
            !File.Exists(RewardBank2D.SavePath) && f.Ledger.Summary().Contains("Tokens 10"), "Death exported pending rewards.");
        var restarted = new LedgerFixture("restart");
        Check(!restarted.Ledger.Finished && RewardBank2D.Snapshot().currency == 0, "New session retained pending data.");
    }
    private static void ClearSettlesOnceAndExcludesTemporary()
    {
        var f = new LedgerFixture("clear"); var definition = Definition();
        var monster = WeaponInstance2D.Create(definition, 1f, 1f, 1f, 1f);
        f.Loadout.TryEquipWeapon(monster); f.Loadout.TryEquipWeapon(monster);
        var chest = WeaponInstance2D.Create(definition, 1.2f, 0.9f, 1.1f, 1f, RewardSource2D.Chest);
        var batch = new[] { Tokens(10), RewardRecord2D.FromWeapon(chest) };
        Check(f.Ledger.TryAddBatch(batch, "chest") && !f.Ledger.TryAddBatch(batch, "again") &&
            ReferenceEquals(f.Loadout.CurrentWeapon, monster), "Duplicate acquisition or forced equip.");
        f.Health.CombatStopped = true; f.Ledger.Finish(true, 5); f.Ledger.Finish(true, 5); f.Ledger.RetrySave();
        var bank = RewardBank2D.Snapshot();
        Check(f.Ledger.ExportSucceeded && bank.currency == 10 && bank.weapons.Count == 1 && bank.weapons[0].instanceId == chest.InstanceId &&
            f.Ledger.Summary().Contains("Monster weapons 1"), "Clear duplicated or exported temporary rewards.");
    }
    private static void BossGrantOnce()
    {
        var f = new LedgerFixture("boss"); Call(f.Ledger, "GrantBossReward"); Call(f.Ledger, "GrantBossReward");
        f.Ledger.Finish(true, 0);
        Check(RewardBank2D.Snapshot().currency == 20, "Development boss reward duplicated.");
    }
    [Serializable] public sealed class LegacyProfile
    {
        public int version; public long currency; public List<WeaponSaveData2D> weapons; public List<string> settledRunIds;
    }
    private static void LegacySaveAddsMagicWithoutLosingRewards()
    {
        string path = FilePath("legacy-magic"); Directory.CreateDirectory(Path.GetDirectoryName(path));
        var item = WeaponInstance2D.Create(Definition(), 1.1f, 0.9f, 1.05f, 1f, RewardSource2D.Chest).ToSaveData();
        string payload = UnityEngine.JsonUtility.ToJson(new LegacyProfile
        { version = 1, currency = 123, weapons = new List<WeaponSaveData2D> { item }, settledRunIds = new List<string> { "old-run" } });
        string hash; using (var sha = System.Security.Cryptography.SHA256.Create())
            hash = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payload))).Replace("-", "");
        string original = UnityEngine.JsonUtility.ToJson(new TestEnvelope { version = 1, payload = payload, sha256 = hash });
        File.WriteAllText(path, original);
        var store = new RewardSaveStore2D(path); var profile = store.Snapshot();
        Check(!store.ReadOnly && profile.currency == 123 && profile.weapons[0].instanceId == item.instanceId &&
            profile.settledRunIds.Contains("old-run") && profile.ownedMagicIds.Count == 3 && profile.equippedMagicId == "fire_bolt" &&
            profile.magicStones == 0 && profile.defeatedBossIds.Count == 0 &&
            File.ReadAllText(path) == original, "Legacy load lost rewards or rewrote original.");
        Check(store.TrySelectMagic("water_bolt") && new RewardSaveStore2D(path).Snapshot().currency == 123,
            "Legacy upgrade failed to preserve rewards.");
    }
    private static void MagicSelectionSurvivesColdReload()
    {
        string path = FilePath("magic-selection"); var store = new RewardSaveStore2D(path);
        Check(!store.TrySelectMagic("flame_blast"), "Locked magic selected.");
        Check(store.TryUnlockMagics(new[] { "flame_blast", "flame_blast" }) && store.TrySelectMagic("flame_blast"), "Unlock/equip failed.");
        var profile = new RewardSaveStore2D(path).Snapshot();
        Check(profile.equippedMagicId == "flame_blast" && profile.ownedMagicIds.Count == 4, "Cold reload lost equip or duplicated ownership.");
    }
    private static void FailedMagicSaveRetainsSelection()
    {
        string path = FilePath("magic-failure"); var store = new RewardSaveStore2D(path);
        Check(store.TrySelectMagic("water_bolt"), "Setup selection failed."); string before = File.ReadAllText(path);
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Check(!store.TrySelectMagic("thorn_bolt") && store.Snapshot().equippedMagicId == "water_bolt", "Failed save changed selection.");
        Check(File.ReadAllText(path) == before, "Failed selection overwrote previous save.");
    }
    private static void RewardSettlementPreservesSelectedMagic()
    {
        string path = FilePath("magic-and-rewards"); var store = new RewardSaveStore2D(path);
        Check(store.TrySelectMagic("thorn_bolt") && store.TryCommit("clear", new[] { Tokens(10) }), "Setup failed.");
        var profile = new RewardSaveStore2D(path).Snapshot();
        Check(profile.currency == 10 && profile.equippedMagicId == "thorn_bolt" && profile.ownedMagicIds.Contains("water_bolt"),
            "Reward commit erased magic progress.");
    }
    private static void BossFirstRepeatAndColdRestore()
    {
        string path = FilePath("boss-first-repeat"); var store = new RewardSaveStore2D(path);
        Check(store.TrySelectMagic("water_bolt"), "Setup magic failed.");
        var first = RewardRecord2D.BossStones("stage1", "flame_boss", 5, 2, false);
        var again = RewardRecord2D.BossStones("stage4", "flame_boss", 5, 2, false);
        Check(store.TryCommit("first-run", new[] { first, again, Tokens(10) }), "Boss export failed.");
        var cold = new RewardSaveStore2D(path); var profile = cold.Snapshot();
        Check(profile.magicStones == 7 && profile.lastMagicStones == 7 && profile.defeatedBossIds.Count == 1 &&
            profile.defeatedBossIds.Contains("flame_boss") && profile.currency == 10 && profile.equippedMagicId == "water_bolt",
            "Shared boss first reward duplicated or previous data lost.");
        Check(cold.TryCommit("second-run", new[] { RewardRecord2D.BossStones("new-stage", "flame_boss", 5, 2, false) }), "Repeat export failed.");
        profile = new RewardSaveStore2D(path).Snapshot();
        Check(profile.magicStones == 9 && profile.lastMagicStones == 2 && profile.defeatedBossIds.Count == 1,
            "Cold repeat must use saved boss ID, not stale preview quantity.");
        string saved = File.ReadAllText(path);
        Check(cold.TryCommit("second-run", new[] { first }) && cold.Snapshot().magicStones == 9 && File.ReadAllText(path) == saved,
            "Duplicate run paid stones again.");
    }
    private static void BossDeathAndRestartDoNotRecordFirst()
    {
        var dead = new LedgerFixture("boss-death");
        Check(dead.Ledger.TryRegisterBossKill("stage3", "water_boss", 5, 2) && dead.Ledger.PendingMagicStones == 5, "Pending first reward missing.");
        dead.Health.CombatStopped = true; dead.Ledger.Finish(false, 0);
        Check(RewardBank2D.Snapshot().magicStones == 0 && !RewardBank2D.Snapshot().defeatedBossIds.Contains("water_boss") &&
            dead.Ledger.Summary().Contains("lost"), "Death committed a first-clear flag or stones.");
        var pending = new LedgerFixture("boss-restart"); pending.Ledger.TryRegisterBossKill("stage1", "flame_boss", 5, 2);
        typeof(RewardBank2D).GetMethod("ResetCache", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        Check(RewardBank2D.Snapshot().magicStones == 0 && RewardBank2D.Snapshot().defeatedBossIds.Count == 0,
            "Unsettled restart leaked boss progression.");
    }
    private static void BossSaveFailureIsAtomic()
    {
        string path = FilePath("boss-failed-write"); var store = new RewardSaveStore2D(path);
        Check(store.TryCommit("setup", new[] { Tokens(3) }), "Setup failed."); string original = File.ReadAllText(path);
        var batch = new[] { RewardRecord2D.BossStones("stage1", "flame_boss", 5, 2, false) };
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Check(!store.TryCommit("boss-clear", batch) && store.Snapshot().magicStones == 0 && store.Snapshot().defeatedBossIds.Count == 0,
                "Failed write partially recorded stones or first kill.");
        Check(File.ReadAllText(path) == original && store.TryCommit("boss-clear", batch), "Retry did not preserve original/award.");
        var profile = new RewardSaveStore2D(path).Snapshot();
        Check(profile.magicStones == 5 && profile.defeatedBossIds.Contains("flame_boss") && profile.currency == 3, "Retry must record both atomically.");
    }
    private static void BossLedgerDeduplicatesAndSupportsSharedId()
    {
        var f = new LedgerFixture("boss-ledger");
        Check(f.Ledger.TryRegisterBossKill("stage1", "flame_boss", 5, 2) &&
            !f.Ledger.TryRegisterBossKill("stage1", "flame_boss", 5, 2), "Same death event registered twice.");
        Check(f.Ledger.TryRegisterBossKill("stage4", "flame_boss", 5, 2) && f.Ledger.PendingMagicStones == 7,
            "Same boss ID must grant repeat reward within the current run.");
        Check(f.Ledger.TryRegisterBossKill("stage5", "flame_elite", 5, 2) && f.Ledger.PendingMagicStones == 12,
            "A separately configured boss ID must have its own first reward.");
        f.Ledger.Finish(true, 0); f.Ledger.Finish(true, 0); f.Ledger.RetrySave();
        Check(RewardBank2D.Snapshot().magicStones == 12 && RewardBank2D.Snapshot().defeatedBossIds.Count == 2,
            "Repeated finalization duplicated stones/first records.");
        var fake = RewardRecord2D.BossStones("fake", "fake_boss", 5, 2, false); fake.source = RewardSource2D.Monster;
        Check(!fake.CanExport && !RewardRules2D.CanExport(RewardKind2D.MagicStone, RewardSource2D.Chest), "Non-boss stones exported.");
    }
    private static void ZeroBossRewardStillRecordsClear()
    {
        var f = new LedgerFixture("boss-zero");
        Check(f.Ledger.TryRegisterBossKill("stage", "zero_boss", 0, 0), "Zero reward should still track a boss clear.");
        f.Ledger.Finish(true, 0);
        var profile = RewardBank2D.Snapshot();
        Check(profile.magicStones == 0 && profile.defeatedBossIds.Contains("zero_boss"), "Zero reward clear was not committed.");
    }
}
