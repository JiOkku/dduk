using System;
using System.Collections.Generic;

public enum RewardSource2D { StartingEquipment, Monster, Chest, Boss }
public enum RewardKind2D { Currency, Weapon, TemporaryGold, MagicStone }

public static class RewardRules2D
{
    public static bool CanExport(RewardKind2D kind, RewardSource2D source) =>
        (source == RewardSource2D.Chest || source == RewardSource2D.Boss) &&
        (kind == RewardKind2D.Currency || kind == RewardKind2D.Weapon ||
        (kind == RewardKind2D.MagicStone && source == RewardSource2D.Boss));
}

// Plain save DTOs: no scene objects or ScriptableObject references.
[Serializable]
public sealed class WeaponSaveData2D
{
    public string instanceId;
    public string definitionId;
    public string name;
    public int damage;
    public float attackInterval;
    public float range;
    public int manaOnHit;
    public float damageScale;
    public float rangeScale;
    public RewardSource2D source;
    public bool exportable;
    public bool IsValid => !string.IsNullOrEmpty(instanceId) && !string.IsNullOrEmpty(definitionId) &&
        damage >= 1 && manaOnHit >= 0 && Finite(attackInterval) && attackInterval > 0f &&
        Finite(range) && range > 0f && Finite(damageScale) && damageScale >= 0f &&
        Finite(rangeScale) && rangeScale > 0f && Enum.IsDefined(typeof(RewardSource2D), source);
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}

[Serializable]
public sealed class RewardRecord2D
{
    public string rewardId;
    public string name;
    public RewardKind2D kind;
    public int quantity;
    public RewardSource2D source;
    public bool exportable;
    public WeaponSaveData2D weapon;
    public string currencyId;
    public string bossId;
    public int firstKillAmount;
    public int repeatKillAmount;
    public bool CanExport => exportable && RewardRules2D.CanExport(kind, source);

    public static RewardRecord2D Currency(int amount, RewardSource2D source) => new RewardRecord2D
    {
        rewardId = Guid.NewGuid().ToString("N"), name = "Export Tokens", kind = RewardKind2D.Currency,
        quantity = Math.Max(0, amount), source = source, exportable = RewardRules2D.CanExport(RewardKind2D.Currency, source)
    };
    public static RewardRecord2D FromWeapon(WeaponInstance2D instance) => new RewardRecord2D
    {
        rewardId = instance.InstanceId, name = instance.WeaponName, kind = RewardKind2D.Weapon, quantity = 1,
        source = instance.Source, exportable = instance.Exportable, weapon = instance.ToSaveData()
    };
    public static RewardRecord2D TemporaryGold(int amount) => new RewardRecord2D
    {
        rewardId = Guid.NewGuid().ToString("N"), name = "Dungeon Gold", kind = RewardKind2D.TemporaryGold,
        quantity = Math.Max(0, amount), source = RewardSource2D.Monster, exportable = false
    };
    public static RewardRecord2D BossStones(string eventId, string boss, int first, int repeat, bool alreadyDefeated) => new RewardRecord2D
    {
        rewardId = "boss_" + eventId, name = "Magic Stones", kind = RewardKind2D.MagicStone,
        quantity = Math.Max(0, alreadyDefeated ? repeat : first), source = RewardSource2D.Boss, exportable = true,
        currencyId = MagicStoneRules2D.CurrencyId, bossId = boss,
        firstKillAmount = Math.Max(0, first), repeatKillAmount = Math.Max(0, repeat)
    };
}

[Serializable]
public sealed class RewardProfile2D
{
    public int version;
    public long currency;
    public List<WeaponSaveData2D> weapons;
    public List<string> settledRunIds;
    public List<string> ownedMagicIds;
    public string equippedMagicId;
    public string equippedWeaponInstanceId;
    public long magicStones;
    public long lastMagicStones;
    public List<string> defeatedBossIds;
    public List<string> unlockedTechNodeIds;
    public int healthUpgradeLevel;
    public int manaUpgradeLevel;
    public int meleeUpgradeLevel;
    public static RewardProfile2D Empty() => new RewardProfile2D
    { version = 1, weapons = new List<WeaponSaveData2D>(), settledRunIds = new List<string>(),
        ownedMagicIds = new List<string>(MagicOwnership2D.StarterIds), equippedMagicId = MagicOwnership2D.DefaultId,
        defeatedBossIds = new List<string>(), unlockedTechNodeIds = new List<string>() };
}

public static class MagicStoneRules2D
{
    public const string CurrencyId = "magic_stone";
    public static int Award(RewardProfile2D profile, RewardRecord2D reward)
    {
        if (profile.defeatedBossIds == null) profile.defeatedBossIds = new List<string>();
        bool first = !profile.defeatedBossIds.Contains(reward.bossId);
        if (first) profile.defeatedBossIds.Add(reward.bossId);
        return first ? reward.firstKillAmount : reward.repeatKillAmount;
    }
}

public static class MagicOwnership2D
{
    public const string DefaultId = "fire_bolt";
    public static string[] StarterIds => new[] { DefaultId, "water_bolt", "thorn_bolt" };
    public static void Normalize(RewardProfile2D profile)
    {
        var ids = new List<string>();
        if (profile.ownedMagicIds != null)
            foreach (string id in profile.ownedMagicIds)
                if (!string.IsNullOrWhiteSpace(id) && !ids.Contains(id)) ids.Add(id);
        foreach (string id in StarterIds) if (!ids.Contains(id)) ids.Add(id);
        profile.ownedMagicIds = ids; // Unknown IDs are retained, but catalog lookup never equips them.
        if (string.IsNullOrWhiteSpace(profile.equippedMagicId) || !ids.Contains(profile.equippedMagicId))
            profile.equippedMagicId = DefaultId;
    }
}
