using System;
using System.Reflection;

// Uses production catalog/data/profile logic with the shared standalone Unity doubles.
public static class MagicDefinitionTests
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Set(object target, string name, object value) => target.GetType().GetField(name,
        BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static MagicData2D Magic(string id)
    {
        var magic = new MagicData2D(); Set(magic, "magicId", id); Set(magic, "magicName", id); return magic;
    }
    private static MagicCatalog2D Catalog(params MagicData2D[] entries)
    { var catalog = new MagicCatalog2D(); Set(catalog, "magics", entries); return catalog; }
    public static int Main()
    {
        Action[] tests = { InitialThreeScrolls, UnknownIdFallsBackWithoutDeletingData, DuplicateIdsRejected,
            TierDoesNotForceCombatStats, UnownedMagicCannotResolve, FlameConeBounds };
        try
        {
            foreach (Action test in tests) { test(); Console.WriteLine("PASS " + test.Method.Name); }
            Console.WriteLine("6 catalog/ownership/data tests passed. No real Unity casting/physics/UI execution."); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static void InitialThreeScrolls()
    {
        var profile = RewardProfile2D.Empty();
        Check(profile.ownedMagicIds.Count == 3 && profile.ownedMagicIds.Contains("water_bolt") &&
            profile.ownedMagicIds.Contains("thorn_bolt") && profile.equippedMagicId == "fire_bolt", "Initial ownership incorrect.");
    }
    private static void UnknownIdFallsBackWithoutDeletingData()
    {
        var fire = Magic("fire_bolt"); var catalog = Catalog(fire); var profile = RewardProfile2D.Empty();
        profile.ownedMagicIds.Add("future_scroll"); profile.equippedMagicId = "future_scroll";
        MagicOwnership2D.Normalize(profile);
        Check(catalog.Equipped(profile) == fire && profile.ownedMagicIds.Contains("future_scroll"), "Unknown IDs unsafe or deleted.");
    }
    private static void DuplicateIdsRejected()
    {
        Check(!Catalog(Magic("fire_bolt"), Magic("fire_bolt")).Validate(out _) &&
            !Catalog(Magic("water_bolt")).Validate(out _), "Duplicate/default validation failed.");
    }
    private static void TierDoesNotForceCombatStats()
    {
        var magic = Magic("custom"); Set(magic, "tier", 3); Set(magic, "manaCost", 7);
        Set(magic, "cooldown", 0.25f); Set(magic, "damage", 2); Set(magic, "attackForm", MagicForm2D.SelfArea);
        Check(magic.Tier == 3 && magic.ManaCost == 7 && magic.Cooldown == 0.25f && magic.Damage == 2 &&
            magic.Form == MagicForm2D.SelfArea, "Tier hardcoded spell stats.");
    }
    private static void UnownedMagicCannotResolve()
    {
        var fire = Magic("fire_bolt"); var blast = Magic("flame_blast"); var catalog = Catalog(fire, blast);
        var profile = RewardProfile2D.Empty(); profile.equippedMagicId = blast.Id;
        Check(catalog.Equipped(profile) == fire, "Unowned scroll resolved.");
        profile.ownedMagicIds.Add(blast.Id); Check(catalog.Equipped(profile) == blast, "Owned scroll did not resolve.");
    }
    private static void FlameConeBounds()
    {
        var spray = Magic("explosive_fireball"); Set(spray, "attackForm", MagicForm2D.Cone); Set(spray, "range", 3f);
        var origin = new UnityEngine.Vector2(0f, 0f); var forward = new UnityEngine.Vector2(1f, 0f);
        Check(spray.ContainsConePoint(origin, forward, new UnityEngine.Vector2(2f, 0.5f)) &&
            !spray.ContainsConePoint(origin, forward, new UnityEngine.Vector2(1f, 2f)) &&
            !spray.ContainsConePoint(origin, forward, new UnityEngine.Vector2(-1f, 0f)) &&
            !spray.ContainsConePoint(origin, forward, new UnityEngine.Vector2(3.1f, 0f)), "Flame cone angle/range/rear bounds incorrect.");
    }
}
