using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

// Production growth rules + actual atomic save store; Unity APIs are shared test doubles.
public static class PermanentGrowthTests
{
    private static string root;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static string PathFor(string name) => Path.Combine(root, name, "rewards.json");
    private static PermanentGrowthData2D Data()
    {
        var magic = new List<MagicData2D>(); var nodes = new List<MagicTechNode2D>();
        string[][] ids = { new[] { "fire_bolt", "explosive_fireball", "flame_blast" },
            new[] { "water_bolt", "piercing_stream", "water_wave" }, new[] { "thorn_bolt", "thorn_burst", "thorn_nova" } };
        string[] elements = { "fire", "water", "grass" };
        for (int element = 0; element < 3; element++) for (int tier = 1; tier <= 3; tier++)
        {
            var spell = new MagicData2D(); Set(spell, "magicId", ids[element][tier - 1]); Set(spell, "magicName", ids[element][tier - 1]);
            Set(spell, "tier", tier); magic.Add(spell);
            nodes.Add(new MagicTechNode2D { nodeId = "node_" + elements[element] + "_" + tier, magicId = ids[element][tier - 1],
                cost = tier == 1 ? 0 : tier == 2 ? 5 : 10,
                prerequisites = tier == 1 ? new string[0] : new[] { "node_" + elements[element] + "_" + (tier - 1) } });
        }
        var catalog = new MagicCatalog2D(); Set(catalog, "magics", magic.ToArray());
        var data = new PermanentGrowthData2D(); Set(data, "magicCatalog", catalog); Set(data, "nodes", nodes.ToArray());
        Set(data, "upgrades", new[] {
            new PermanentUpgradeData2D { kind = PermanentUpgradeKind2D.Health, bonusPerLevel = 1, costs = new[] {2,4,6,8,10} },
            new PermanentUpgradeData2D { kind = PermanentUpgradeKind2D.Mana, bonusPerLevel = 10, costs = new[] {2,4,6,8,10} },
            new PermanentUpgradeData2D { kind = PermanentUpgradeKind2D.MeleeDamage, bonusPerLevel = 1, costs = new[] {5,10,15} } });
        Check(data.Validate(out _), "Fixture data invalid."); return data;
    }
    private static RewardSaveStore2D Funded(string name, int stones = 100)
    {
        var store = new RewardSaveStore2D(PathFor(name));
        Check(store.TryCommit("fund", new[] { RewardRecord2D.BossStones("boss", "test_boss", stones, 2, false), RewardRecord2D.Currency(10, RewardSource2D.Chest) }), "Funding failed.");
        return store;
    }
    public static int Main()
    {
        root = Path.Combine(Path.GetTempPath(), "dduk-growth-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Action[] tests = { UnlockRequiresPrerequisiteAndFunds, InvalidTreeBlocked, UpgradeCapsAndStaleRequests,
            FailedPurchasesPreserveEverything, LegacyOwnedScrollsReconcileWithoutReset, NodeLedgerRestoresOwnership,
            BonusesStaySeparateFromWeapons, PurchasesAreLobbyOnlyAndNotifyOnce };
        try
        {
            foreach (var test in tests) { test(); Console.WriteLine("PASS " + test.Method.Name); }
            Console.WriteLine("8 focused growth scenarios passed (real temporary-file I/O; Unity doubles, no Play-mode UI/physics)."); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static void UnlockRequiresPrerequisiteAndFunds()
    {
        var data = Data(); var store = Funded("unlock", 15);
        Check(!store.TryPurchaseNode(data, "node_fire_3") && store.Snapshot().magicStones == 15, "T3 bypassed prerequisite.");
        Check(store.TryPurchaseNode(data, "node_fire_2"), "T2 purchase failed.");
        Check(!store.TryPurchaseNode(data, "node_fire_2") && store.Snapshot().magicStones == 10, "Duplicate T2 charged twice.");
        Check(store.TryPurchaseNode(data, "node_fire_3") && store.Snapshot().magicStones == 0, "T3 purchase/cost failed.");
        Check(!store.TryPurchaseNode(data, "node_water_2") && !store.TryPurchaseNode(data, "unknown"), "Insufficient funds/unknown ID accepted.");
        Check(!store.TryPurchaseUpgrade(data, PermanentUpgradeKind2D.Health, 0) && store.Snapshot().healthUpgradeLevel == 0,
            "Upgrade accepted insufficient funds.");
        Check(store.TrySelectMagic("flame_blast", data), "Unlocked scroll cannot be equipped.");
        var cold = new RewardSaveStore2D(PathFor("unlock")).Snapshot();
        Check(cold.unlockedTechNodeIds.Contains("node_fire_3") && cold.ownedMagicIds.Contains("flame_blast") &&
            cold.equippedMagicId == "flame_blast" && cold.magicStones == 0 && cold.currency == 10 && cold.defeatedBossIds.Contains("test_boss"),
            "Cold reload lost growth/equipment/existing rewards.");
    }
    private static void InvalidTreeBlocked()
    {
        var store = Funded("invalid"); var data = Data(); data.FindNode("node_fire_2").prerequisites = new[] { "missing" };
        Check(!data.Validate(out _) && !store.TryPurchaseNode(data, "node_fire_2"), "Unknown prerequisite accepted.");
        data = Data(); data.FindNode("node_fire_2").prerequisites = new[] { "node_fire_3" };
        Check(!data.Validate(out _) && !store.TryPurchaseNode(data, "node_fire_2"), "Cycle accepted.");
        data = Data(); data.FindNode("node_fire_2").nodeId = "node_fire_1";
        Check(!data.Validate(out _), "Duplicate node ID accepted.");
        data = Data(); data.FindNode("node_fire_2").magicId = "missing_magic";
        Check(!data.Validate(out _) && !store.TryPurchaseNode(data, "node_fire_2") && store.Snapshot().magicStones == 100, "Invalid magic consumed currency.");
        string path = PathFor("invalid-save"); var badProfile = RewardProfile2D.Empty(); badProfile.healthUpgradeLevel = -1;
        SaveEnvelope(path, badProfile); string original = File.ReadAllText(path);
        var invalidStore = new RewardSaveStore2D(path);
        Check(invalidStore.ReadOnly && !invalidStore.TryPurchaseUpgrade(Data(), PermanentUpgradeKind2D.Health, 0) &&
            File.ReadAllText(path) == original, "Invalid stored growth level was overwritten.");
    }
    private static void UpgradeCapsAndStaleRequests()
    {
        var data = Data(); var store = Funded("upgrades", 200);
        Check(store.TryPurchaseUpgrade(data, PermanentUpgradeKind2D.Health, 0), "HP upgrade failed.");
        Check(!store.TryPurchaseUpgrade(data, PermanentUpgradeKind2D.Health, 0) && store.Snapshot().healthUpgradeLevel == 1,
            "Repeated/stale request bought a second level.");
        for (int i = 1; i < 5; i++) Check(store.TryPurchaseUpgrade(data, PermanentUpgradeKind2D.Health, i), "HP tier failed.");
        Check(!store.TryPurchaseUpgrade(data, PermanentUpgradeKind2D.Health, 5), "HP exceeded MAX.");
        Check(store.TryPurchaseUpgrade(data, PermanentUpgradeKind2D.Mana, 0), "Mana purchase failed.");
        for (int i = 0; i < 3; i++) Check(store.TryPurchaseUpgrade(data, PermanentUpgradeKind2D.MeleeDamage, i), "Melee tier failed.");
        Check(!store.TryPurchaseUpgrade(data, PermanentUpgradeKind2D.MeleeDamage, 3), "Melee exceeded MAX.");
        var cold = new RewardSaveStore2D(PathFor("upgrades")).Snapshot();
        Check(cold.healthUpgradeLevel == 5 && cold.manaUpgradeLevel == 1 && cold.meleeUpgradeLevel == 3 && cold.magicStones == 138,
            "Levels/costs failed to persist.");
        var bonuses = PermanentBonuses2D.From(cold, data);
        Check(bonuses.Health == 5 && bonuses.Mana == 10 && bonuses.MeleeDamage == 3, "Persisted bonuses incorrect.");
    }
    private static void FailedPurchasesPreserveEverything()
    {
        var data = Data(); var store = Funded("failure"); string path = PathFor("failure"); string before = File.ReadAllText(path);
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Check(!store.TryPurchaseNode(data, "node_fire_2") && store.Snapshot().magicStones == 100 &&
                !store.Snapshot().ownedMagicIds.Contains("explosive_fireball"), "Failed unlock partially applied.");
        Check(File.ReadAllText(path) == before && store.TryPurchaseNode(data, "node_fire_2"), "Retry did not preserve original then unlock.");
        before = File.ReadAllText(path);
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Check(!store.TryPurchaseUpgrade(data, PermanentUpgradeKind2D.Health, 0) && store.Snapshot().healthUpgradeLevel == 0 &&
                store.Snapshot().magicStones == 95, "Failed upgrade partially applied.");
        Check(File.ReadAllText(path) == before && store.TryPurchaseUpgrade(data, PermanentUpgradeKind2D.Health, 0), "Upgrade retry failed.");
    }
    [Serializable] private sealed class PreviousProfile
    {
        public int version = 1; public long currency = 44; public long magicStones = 50; public long lastMagicStones = 5;
        public List<WeaponSaveData2D> weapons = new List<WeaponSaveData2D>();
        public List<string> settledRunIds = new List<string> { "old-run" };
        public List<string> ownedMagicIds = new List<string> { "fire_bolt", "water_bolt", "thorn_bolt", "flame_blast" };
        public string equippedMagicId = "flame_blast";
        public List<string> defeatedBossIds = new List<string> { "flame_boss" };
    }
    private static void SaveEnvelope(string path, object profile)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string payload = UnityEngine.JsonUtility.ToJson(profile); string hash;
        using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-", "");
        File.WriteAllText(path, UnityEngine.JsonUtility.ToJson(new RewardPersistenceTests.TestEnvelope { version = 1, payload = payload, sha256 = hash }));
    }
    private static void LegacyOwnedScrollsReconcileWithoutReset()
    {
        var previous = new PreviousProfile();
        var weapon = new WeaponPartData2D(); Set(weapon, "weaponName", "Spear"); Set(weapon, "weaponId", "spear");
        var savedWeapon = WeaponInstance2D.Create(weapon, 1.2f, 0.87f, 1.09f, 1.1f, RewardSource2D.Chest).ToSaveData();
        previous.weapons.Add(savedWeapon);
        string path = PathFor("legacy"); SaveEnvelope(path, previous); string original = File.ReadAllText(path);
        var store = new RewardSaveStore2D(path); var data = Data(); var profile = store.GrowthSnapshot(data);
        Check(profile.healthUpgradeLevel == 0 && profile.unlockedTechNodeIds.Contains("node_fire_3") &&
            profile.equippedMagicId == "flame_blast" && profile.defeatedBossIds.Contains("flame_boss") &&
            File.ReadAllText(path) == original, "Legacy reconciliation rewrote/reset existing progress.");
        Check(!store.TryPurchaseNode(data, "node_fire_3") && store.Snapshot().magicStones == 50, "Existing test scroll repurchased.");
        Check(store.TryPurchaseNode(data, "node_water_2"), "New purchase after migration failed.");
        var cold = new RewardSaveStore2D(path).Snapshot();
        Check(cold.currency == 44 && cold.magicStones == 45 && cold.lastMagicStones == 5 && cold.settledRunIds.Contains("old-run") &&
            cold.defeatedBossIds.Contains("flame_boss") && cold.unlockedTechNodeIds.Contains("node_fire_3") &&
            cold.weapons[0].instanceId == savedWeapon.instanceId && cold.weapons[0].attackInterval == savedWeapon.attackInterval &&
            cold.weapons[0].range == savedWeapon.range && cold.weapons[0].manaOnHit == savedWeapon.manaOnHit,
            "Migration lost prior records or weapon instance stats.");
    }
    private static void NodeLedgerRestoresOwnership()
    {
        var profile = RewardProfile2D.Empty(); profile.unlockedTechNodeIds.Add("node_water_2"); profile.unlockedTechNodeIds.Add("future_node");
        string path = PathFor("node-only"); SaveEnvelope(path, profile);
        var store = new RewardSaveStore2D(path); var data = Data();
        Check(store.GrowthSnapshot(data).ownedMagicIds.Contains("piercing_stream") && store.TrySelectMagic("piercing_stream", data),
            "Known unlocked node did not restore scroll/equip.");
        Check(new RewardSaveStore2D(path).Snapshot().unlockedTechNodeIds.Contains("future_node"), "Unknown saved node deleted.");
    }
    private static void BonusesStaySeparateFromWeapons()
    {
        var data = Data(); var profile = RewardProfile2D.Empty(); profile.healthUpgradeLevel = 2; profile.manaUpgradeLevel = 3; profile.meleeUpgradeLevel = 1;
        var first = PermanentBonuses2D.From(profile, data); var restart = PermanentBonuses2D.From(profile, data);
        Check(PermanentBonuses2D.AddToBase(10, first.Health) == 12 && PermanentBonuses2D.AddToBase(100, first.Mana) == 130 &&
            first.Health == restart.Health && first.MeleeDamage == restart.MeleeDamage, "Bonuses accumulate on restart.");
        var definition = new WeaponPartData2D(); Set(definition, "weaponName", "Test"); Set(definition, "weaponId", "test"); Set(definition, "damage", 4);
        var rolled = WeaponInstance2D.Create(definition, 1.2f, 0.9f, 1.1f, 1f);
        int original = rolled.Damage;
        Check(PermanentBonuses2D.AddToBase(rolled.Damage, first.MeleeDamage) == original + 1 && rolled.Damage == original && definition.Damage == 4,
            "Growth mutated weapon instance or original asset.");
        var swapped = WeaponInstance2D.CreateBase(definition);
        Check(PermanentBonuses2D.AddToBase(swapped.Damage, first.MeleeDamage) == 5 && first.MeleeDamage == restart.MeleeDamage,
            "Weapon swap lost permanent damage.");
    }
    private static void PurchasesAreLobbyOnlyAndNotifyOnce()
    {
        UnityEngine.Application.persistentDataPath = Path.Combine(root, "bank");
        typeof(RewardBank2D).GetMethod("ResetCache", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        Check(RewardBank2D.TryCommit("fund", new[] { RewardRecord2D.BossStones("boss", "boss", 20, 2, false) }), "Bank funding failed.");
        int events = 0; Action changed = () => events++; RewardBank2D.Changed += changed;
        var data = Data(); SceneFlow2D.InLobby = false;
        Check(!RewardBank2D.TryPurchaseNode(data, "node_fire_2") && events == 0 && RewardBank2D.Snapshot().magicStones == 20,
            "Dungeon permitted purchase.");
        SceneFlow2D.InLobby = true;
        Check(RewardBank2D.TryPurchaseNode(data, "node_fire_2") && !RewardBank2D.TryPurchaseNode(data, "node_fire_2") && events == 1,
            "Duplicate purchase/notification.");
        RewardBank2D.Changed -= changed;
    }
}
