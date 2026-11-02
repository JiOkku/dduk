using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Only plain profile data is shared across scenes. No persistent GameObject or scene references.
public static class RewardBank2D
{
    private static RewardSaveStore2D store;
    public static event Action Changed;
    public static string SavePath => Path.Combine(Application.persistentDataPath, "prototype-rewards-v1.json");
    private static RewardSaveStore2D Store => store ?? (store = new RewardSaveStore2D(SavePath));
    public static RewardProfile2D Snapshot() => Store.Snapshot();
    public static string Error => Store.Error;
    public static RewardProfile2D GrowthSnapshot(PermanentGrowthData2D growth) => Store.GrowthSnapshot(growth);
    public static bool TryCommit(string runId, IList<RewardRecord2D> rewards) => Notify(Store.TryCommit(runId, rewards));
    public static bool TrySelectMagic(string id, PermanentGrowthData2D growth = null) => Notify(Store.TrySelectMagic(id, growth));
    public static bool TrySelectWeapon(string id) => SceneFlow2D.InLobby && Notify(Store.TrySelectWeapon(id));
    public static bool TryUnlockMagics(IList<string> ids) => Notify(Store.TryUnlockMagics(ids));
    public static bool TryPurchaseNode(PermanentGrowthData2D growth, string id) =>
        SceneFlow2D.InLobby && Notify(Store.TryPurchaseNode(growth, id));
    public static bool TryPurchaseUpgrade(PermanentGrowthData2D growth, PermanentUpgradeKind2D kind, int level) =>
        SceneFlow2D.InLobby && Notify(Store.TryPurchaseUpgrade(growth, kind, level));
    private static bool Notify(bool saved) { if (saved) Changed?.Invoke(); return saved; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() { store = null; Changed = null; }
}
