using System;
using System.Collections.Generic;
using UnityEngine;

public enum PermanentUpgradeKind2D { Health, Mana, MeleeDamage }
public enum MagicNodeState2D { Locked, Available, Unlocked }

[Serializable]
public sealed class MagicTechNode2D
{
    public string nodeId;
    public string magicId;
    public string displayName;
    [TextArea(1, 3)] public string description;
    [Min(0)] public int cost;
    public string[] prerequisites;
}

[Serializable]
public sealed class PermanentUpgradeData2D
{
    public PermanentUpgradeKind2D kind;
    public string displayName;
    [Min(1)] public int bonusPerLevel = 1;
    public int[] costs;
    public int MaxLevel => costs != null ? costs.Length : 0;
}

[CreateAssetMenu(fileName = "PermanentGrowth", menuName = "Prototype/Permanent Growth")]
public sealed class PermanentGrowthData2D : ScriptableObject
{
    [SerializeField] private MagicCatalog2D magicCatalog;
    [SerializeField] private MagicTechNode2D[] nodes;
    [SerializeField] private PermanentUpgradeData2D[] upgrades;
    public MagicCatalog2D MagicCatalog => magicCatalog;
    public IEnumerable<MagicTechNode2D> Nodes => nodes ?? new MagicTechNode2D[0];
    public IEnumerable<PermanentUpgradeData2D> Upgrades => upgrades ?? new PermanentUpgradeData2D[0];
    public MagicTechNode2D FindNode(string id)
    { foreach (var node in Nodes) if (node != null && node.nodeId == id) return node; return null; }
    public PermanentUpgradeData2D FindUpgrade(PermanentUpgradeKind2D kind)
    { foreach (var upgrade in Upgrades) if (upgrade != null && upgrade.kind == kind) return upgrade; return null; }
    public bool Validate(out string error)
    {
        if (magicCatalog == null || !magicCatalog.Validate(out error)) { error = "Invalid growth magic catalog."; return false; }
        var ids = new HashSet<string>(); var magics = new HashSet<string>();
        foreach (var node in Nodes)
            if (node == null || string.IsNullOrWhiteSpace(node.nodeId) || !ids.Add(node.nodeId) ||
                !magics.Add(node.magicId ?? "") || magicCatalog.Find(node.magicId) == null || node.cost < 0)
            { error = "Missing/duplicate node or magic ID, or invalid node cost."; return false; }
        if (ids.Count == 0) { error = "Growth tree is empty."; return false; }
        foreach (var node in Nodes)
            if (node.prerequisites != null) foreach (string id in node.prerequisites)
                if (string.IsNullOrWhiteSpace(id) || !ids.Contains(id))
                { error = "Unknown prerequisite on " + node.nodeId; return false; }
        var visiting = new HashSet<string>(); var visited = new HashSet<string>();
        foreach (var node in Nodes) if (!Visit(node, visiting, visited))
        { error = "Tech tree contains a cycle."; return false; }
        var kinds = new HashSet<PermanentUpgradeKind2D>();
        foreach (var upgrade in Upgrades)
        {
            if (upgrade == null || !Enum.IsDefined(typeof(PermanentUpgradeKind2D), upgrade.kind) || !kinds.Add(upgrade.kind) ||
                upgrade.MaxLevel == 0 || upgrade.bonusPerLevel < 1 || (long)upgrade.MaxLevel * upgrade.bonusPerLevel > int.MaxValue)
            { error = "Invalid/duplicate permanent upgrade."; return false; }
            foreach (int cost in upgrade.costs) if (cost < 0) { error = "Negative upgrade cost."; return false; }
        }
        if (kinds.Count != 3) { error = "Configure health, mana and melee upgrades."; return false; }
        error = null; return true;
    }
    private bool Visit(MagicTechNode2D node, HashSet<string> visiting, HashSet<string> visited)
    {
        if (visited.Contains(node.nodeId)) return true;
        if (!visiting.Add(node.nodeId)) return false;
        if (node.prerequisites != null) foreach (string id in node.prerequisites)
            if (!Visit(FindNode(id), visiting, visited)) return false;
        visiting.Remove(node.nodeId); visited.Add(node.nodeId); return true;
    }
}

public static class PermanentGrowthRules2D
{
    // Reconcile old owned scrolls and known tree nodes without deleting unknown IDs.
    public static void Normalize(RewardProfile2D profile, PermanentGrowthData2D data)
    {
        MagicOwnership2D.Normalize(profile);
        if (profile.unlockedTechNodeIds == null) profile.unlockedTechNodeIds = new List<string>();
        foreach (var node in data.Nodes)
        {
            if (profile.ownedMagicIds.Contains(node.magicId) && !profile.unlockedTechNodeIds.Contains(node.nodeId))
                profile.unlockedTechNodeIds.Add(node.nodeId);
            if (profile.unlockedTechNodeIds.Contains(node.nodeId) && !profile.ownedMagicIds.Contains(node.magicId))
                profile.ownedMagicIds.Add(node.magicId);
        }
    }
    public static MagicNodeState2D NodeState(RewardProfile2D profile, MagicTechNode2D node)
    {
        if (profile.unlockedTechNodeIds.Contains(node.nodeId) || profile.ownedMagicIds.Contains(node.magicId)) return MagicNodeState2D.Unlocked;
        if (node.prerequisites != null) foreach (string id in node.prerequisites)
            if (!profile.unlockedTechNodeIds.Contains(id)) return MagicNodeState2D.Locked;
        return MagicNodeState2D.Available;
    }
    public static int Level(RewardProfile2D profile, PermanentUpgradeKind2D kind) =>
        kind == PermanentUpgradeKind2D.Health ? profile.healthUpgradeLevel :
        kind == PermanentUpgradeKind2D.Mana ? profile.manaUpgradeLevel : profile.meleeUpgradeLevel;
    public static void SetLevel(RewardProfile2D profile, PermanentUpgradeKind2D kind, int level)
    {
        if (kind == PermanentUpgradeKind2D.Health) profile.healthUpgradeLevel = level;
        else if (kind == PermanentUpgradeKind2D.Mana) profile.manaUpgradeLevel = level;
        else profile.meleeUpgradeLevel = level;
    }
}

public struct PermanentBonuses2D
{
    public int Health { get; private set; }
    public int Mana { get; private set; }
    public int MeleeDamage { get; private set; }
    public static PermanentBonuses2D From(RewardProfile2D profile, PermanentGrowthData2D data)
    {
        var bonuses = new PermanentBonuses2D();
        foreach (var upgrade in data.Upgrades)
        {
            int amount = Math.Max(0, Math.Min(PermanentGrowthRules2D.Level(profile, upgrade.kind), upgrade.MaxLevel)) * upgrade.bonusPerLevel;
            if (upgrade.kind == PermanentUpgradeKind2D.Health) bonuses.Health = amount;
            else if (upgrade.kind == PermanentUpgradeKind2D.Mana) bonuses.Mana = amount;
            else bonuses.MeleeDamage = amount;
        }
        return bonuses;
    }
    public static int AddToBase(int baseValue, int bonus) => (int)Math.Min(int.MaxValue, (long)baseValue + Math.Max(0, bonus));
}
