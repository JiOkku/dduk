using System;
using System.IO;
using System.Reflection;

public static class LobbyLoadoutTests
{
    private static string root;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static void Call(object target, string name) => target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
    private static WeaponPartData2D Definition(string id, int damage)
    {
        var data = new WeaponPartData2D(); Set(data, "weaponId", id); Set(data, "weaponName", id); Set(data, "damage", damage); return data;
    }
    private static WeaponCatalog2D Catalog(WeaponPartData2D club, WeaponPartData2D spear)
    {
        var catalog = new WeaponCatalog2D(); Set(catalog, "defaultWeapon", club); Set(catalog, "weapons", new[] { club, spear }); return catalog;
    }
    public static int Main()
    {
        root = Path.Combine(Path.GetTempPath(), "dduk-lobby-loadout-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Action[] tests = { SelectedWeaponRestoresExactStats, InvalidOrFailedSelectionKeepsPrevious,
            UnknownDefinitionFallsBackWithoutDeletingInventory, RuntimeSwapsDoNotDuplicateStoredWeapon, BankSelectionIsLobbyOnly };
        try
        {
            foreach (var test in tests) { test(); Console.WriteLine("PASS " + test.Method.Name); }
            Console.WriteLine("5 focused loadout/storage scenarios passed; temporary files and Unity doubles, no scene/UI execution."); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static void SelectedWeaponRestoresExactStats()
    {
        var club = Definition("club", 1); var spear = Definition("spear", 2); var catalog = Catalog(club, spear);
        Check(catalog.Validate(), "Catalog invalid.");
        var item = WeaponInstance2D.Create(spear, 1.2f, 0.87f, 1.09f, 1.1f, RewardSource2D.Chest);
        string path = Path.Combine(root, "selection.json"); var store = new RewardSaveStore2D(path);
        Check(store.TryCommit("chest", new[] { RewardRecord2D.FromWeapon(item) }) && store.TrySelectWeapon(item.InstanceId), "Cannot select stored weapon.");
        var profile = new RewardSaveStore2D(path).Snapshot(); var runtime = catalog.StartingWeapon(profile); var restart = catalog.StartingWeapon(profile);
        Check(runtime.InstanceId == item.InstanceId && runtime.Damage == item.Damage && runtime.AttackInterval == item.AttackInterval &&
            runtime.Range == item.Range && runtime.ManaOnHit == item.ManaOnHit && runtime.Source == item.Source,
            "Selection rerolled stats or lost identity/provenance.");
        Check(!ReferenceEquals(runtime, restart) && restart.InstanceId == item.InstanceId && profile.weapons.Count == 1,
            "New run must restore a copy without consuming inventory.");
        Check(store.TrySelectWeapon(null) && catalog.StartingWeapon(store.Snapshot()).BaseData == club && store.Snapshot().weapons.Count == 1,
            "Default weapon selection removed storage items.");
    }
    private static void InvalidOrFailedSelectionKeepsPrevious()
    {
        var item = WeaponInstance2D.Create(Definition("spear", 2), 1, 1, 1, 1, RewardSource2D.Chest);
        string path = Path.Combine(root, "failure.json"); var store = new RewardSaveStore2D(path);
        Check(store.TryCommit("chest", new[] { RewardRecord2D.FromWeapon(item) }) && store.TrySelectWeapon(item.InstanceId), "Setup failed.");
        string original = File.ReadAllText(path);
        Check(!store.TrySelectWeapon("not-owned") && File.ReadAllText(path) == original && store.Snapshot().equippedWeaponInstanceId == item.InstanceId,
            "Unowned selection changed save.");
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Check(!store.TrySelectWeapon(null) && store.Snapshot().equippedWeaponInstanceId == item.InstanceId, "Failed save changed selection.");
        Check(File.ReadAllText(path) == original && store.TrySelectWeapon(null), "Original/retry invalid.");
    }
    private static void UnknownDefinitionFallsBackWithoutDeletingInventory()
    {
        var club = Definition("club", 1); var spear = Definition("spear", 2); var catalog = Catalog(club, spear);
        var profile = RewardProfile2D.Empty(); var item = WeaponInstance2D.Create(Definition("future_weapon", 3), 1, 1, 1, 1, RewardSource2D.Chest);
        profile.weapons.Add(item.ToSaveData()); profile.equippedWeaponInstanceId = item.InstanceId;
        Check(catalog.StartingWeapon(profile).BaseData == club && profile.weapons.Count == 1 && profile.equippedWeaponInstanceId == item.InstanceId,
            "Missing definition deleted saved inventory/selection.");
        profile.equippedWeaponInstanceId = null;
        Check(catalog.StartingWeapon(profile).BaseData == club, "Legacy missing selection must use default.");
    }
    private static void RuntimeSwapsDoNotDuplicateStoredWeapon()
    {
        UnityEngine.Application.persistentDataPath = Path.Combine(root, "runtime");
        typeof(RewardBank2D).GetMethod("ResetCache", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        SceneFlow2D.InLobby = true;
        var club = Definition("club", 1); var spear = Definition("spear", 2); var catalog = Catalog(club, spear);
        var item = WeaponInstance2D.Create(spear, 1.2f, 0.9f, 1.1f, 1f, RewardSource2D.Chest);
        Check(RewardBank2D.TryCommit("chest", new[] { RewardRecord2D.FromWeapon(item) }) && RewardBank2D.TrySelectWeapon(item.InstanceId), "Setup failed.");
        var health = new PlayerHealth2D(); var loadout = new PlayerWeaponLoadout2D { Component = health };
        Set(loadout, "club", club); Set(loadout, "weaponCatalog", catalog); Call(loadout, "Awake");
        Check(loadout.CurrentWeapon.InstanceId == item.InstanceId, "Dungeon loadout ignored lobby selection.");
        var ledger = new DungeonRewards2D(); Set(ledger, "playerHealth", health); Set(ledger, "loadout", loadout); Set(ledger, "policy", new RewardSettlementPolicy2D());
        Call(ledger, "Awake"); Call(ledger, "OnEnable");
        var storedRuntime = loadout.CurrentWeapon;
        loadout.TryEquipWeapon(WeaponInstance2D.Create(spear, 1, 1, 1, 1)); loadout.TryEquipWeapon(storedRuntime);
        ledger.Finish(true, 0); ledger.Finish(true, 0);
        var profile = RewardBank2D.Snapshot();
        Check(profile.weapons.Count == 1 && profile.weapons[0].instanceId == item.InstanceId && profile.equippedWeaponInstanceId == item.InstanceId,
            "Dungeon swap/settlement duplicated storage weapon or rewrote lobby selection.");
    }
    private static void BankSelectionIsLobbyOnly()
    {
        SceneFlow2D.InLobby = false;
        string previous = RewardBank2D.Snapshot().equippedWeaponInstanceId;
        Check(!RewardBank2D.TrySelectWeapon(null) && RewardBank2D.Snapshot().equippedWeaponInstanceId == previous, "Dungeon changed persistent selection.");
        SceneFlow2D.InLobby = true;
        Check(RewardBank2D.TrySelectWeapon(null), "Lobby cannot choose default.");
    }
}
