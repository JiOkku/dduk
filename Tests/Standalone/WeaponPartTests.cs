// Standalone doubles for weapon data/instances, roll settings, loadout and drop tables.
// Verifies production geometry/selection logic; does not execute Unity physics, input or rendering.
using System;
using System.Reflection;

// Scene access is isolated for persistence tests; production uses the actual SceneFlow2D.
public static class SceneFlow2D { public static bool InLobby = true; }

namespace UnityEngine
{
    public class ScriptableObject { }
    public class MonoBehaviour
    {
        public bool enabled = true;
        public bool isActiveAndEnabled => enabled;
        public object Component;
        public T GetComponent<T>() => (T)Component;
    }
    public sealed class SerializeField : Attribute { }
    public sealed class MinAttribute : Attribute { public MinAttribute(float value) { } }
    public sealed class RangeAttribute : Attribute { public RangeAttribute(float min, float max) { } }
    public sealed class TooltipAttribute : Attribute { public TooltipAttribute(string value) { } }
    public sealed class TextAreaAttribute : Attribute { public TextAreaAttribute(int min, int max) { } }
    public sealed class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int order) { } }
    public sealed class DisallowMultipleComponent : Attribute { }
    public sealed class RequireComponent : Attribute { public RequireComponent(Type type) { } }
    public sealed class CreateAssetMenuAttribute : Attribute { public string fileName; public string menuName; }
    public sealed class ContextMenu : Attribute { public ContextMenu(string value) { } }
    public static class Application
    {
        public static bool isFocused = true;
        public static bool isPlaying = true;
        public static string persistentDataPath;
    }
    public static class Time { public static float time; }
    public static class Random { public static float value = 0.5f; }
    public static class Debug
    {
        public static void LogError(string message, object context) { }
        public static void LogWarning(string message, object context = null) { }
    }
    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int RoundToInt(float value) => (int)Math.Round(value, MidpointRounding.ToEven);
        public static float Lerp(float a, float b, float value) => a + (b - a) * Clamp01(value);
        public static float Abs(float value) => Math.Abs(value);
        public static float Clamp(float value, float min, float max) => Math.Clamp(value, min, max);
        public static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);
        public static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);
    }
    public struct Color { public static Color white => default; public static Color red => default; }
    public sealed class Sprite { }
    public sealed class Material { }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float sqrMagnitude => x * x + y * y;
        public Vector2 normalized { get { Vector2 value = this; value.Normalize(); return value; } }
        public void Normalize() { float length = (float)Math.Sqrt(sqrMagnitude); if (length > 0) { x /= length; y /= length; } }
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
        public static float Angle(Vector2 a, Vector2 b)
        {
            double denominator = Math.Sqrt(a.sqrMagnitude * b.sqrMagnitude);
            if (denominator < 1e-15) return 0f;
            return (float)(Math.Acos(Math.Clamp(Dot(a, b) / denominator, -1d, 1d)) * 180d / Math.PI);
        }
    }
}
namespace UnityEngine.InputSystem
{
    public sealed class Key { public bool wasPressedThisFrame; }
    public sealed class Keyboard
    {
        public static Keyboard current;
        public Key digit1Key = new Key(), digit2Key = new Key(), digit3Key = new Key();
    }
}
public sealed class PlayerHealth2D
{
    public bool CombatStopped;
    public int Health = 10;
    public int Mana = 100;
}

public static class WeaponPartTests
{
    private static readonly UnityEngine.Vector2 Origin = new UnityEngine.Vector2(0f, 0f);
    private static readonly UnityEngine.Vector2 Right = new UnityEngine.Vector2(1f, 0f);
    private static void Set(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    private static void Call(object target, string name) =>
        target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static WeaponPartData2D Weapon(MeleeShape2D shape, float range, float angle = 90f, float width = 0.5f)
    {
        var data = new WeaponPartData2D();
        Set(data, "attackShape", shape); Set(data, "range", range); Set(data, "angle", angle); Set(data, "width", width);
        return data;
    }
    private static bool Hit(WeaponPartData2D weapon, float x, float y) => weapon.ContainsPoint(Origin, Right, new UnityEngine.Vector2(x, y));
    private static WeaponInstance2D Instance(MeleeShape2D shape, float range) => WeaponInstance2D.CreateBase(Weapon(shape, range));

    public static int Main()
    {
        Action[] tests = { SectorFrontRangeAndRear, WhipWiderAndLonger, SpearWidthAndLength, RotatedSpear,
            ZeroDirectionRejected, DataClampsInvalidNumbers, SelectionStartsClubAndPreservesResources,
            TerminalStateBlocksSwitch, MissingDataFailsSafely, SpearTipSweetSpot, ChargeThresholds, EquipmentChangesResetChargeClock,
            ZeroChanceNeverDrops, WeightedDropsAndProbabilityGate, FullChanceAndInvalidWeights, ScaledWeightsPreserveRatios,
            RolledBoundsAndOriginalUnchanged, InstanceIdentitySurvivesReequip, SameTypeDistinctInstances,
            SnapshotStatsDoNotReroll, ScaledSpecialGeometry, TableCreatesInstances };
        try
        {
            foreach (Action test in tests) { test(); Console.WriteLine("PASS " + test.Method.Name); }
            Console.WriteLine("22 weapon/instance/drop-table tests passed with simulated dependencies. No Unity Play/physics/UI execution.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static void SectorFrontRangeAndRear()
    {
        var club = Weapon(MeleeShape2D.Sector, 1.5f);
        Check(Hit(club, 1.49f, 0f) && !Hit(club, 1.51f, 0f) && !Hit(club, -1f, 0f) && !Hit(club, 0f, 1f), "Club sector boundary incorrect.");
    }
    private static void WhipWiderAndLonger()
    {
        var club = Weapon(MeleeShape2D.Sector, 1.5f);
        var whip = Weapon(MeleeShape2D.ArcBand, 2.3f, 100f);
        Set(whip, "innerRange", 1.8f);
        Check(!Hit(whip, 1.79f, 0f) && Hit(whip, 1.81f, 0f), "Whip must leave a close dead zone.");
        Check(Hit(whip, 2.29f, 0f) && !Hit(whip, 2.31f, 0f), "Whip outer length incorrect.");
        Check(!Hit(club, 2f, 0f) && Hit(whip, 2f, 0f) && !Hit(whip, 0f, 2f), "Whip arc angle incorrect.");
    }
    private static void SpearWidthAndLength()
    {
        var spear = Weapon(MeleeShape2D.Line, 2.5f);
        Check(Hit(spear, 2.49f, 0.24f) && !Hit(spear, 2.51f, 0f) && !Hit(spear, 1f, 0.26f) && !Hit(spear, -0.1f, 0f), "Spear rectangle incorrect.");
        Check(Hit(spear, 0.7f, 0f) && Hit(spear, 2.2f, 0f), "Multiple targets along spear must be eligible.");
    }
    private static void RotatedSpear()
    {
        var spear = Weapon(MeleeShape2D.Line, 2.5f);
        Check(spear.ContainsPoint(new UnityEngine.Vector2(3f, 2f), new UnityEngine.Vector2(0f, 2f), new UnityEngine.Vector2(3.24f, 4.4f)), "Rotation/translation/normalization incorrect.");
        Check(!spear.ContainsPoint(Origin, new UnityEngine.Vector2(0f, 1f), new UnityEngine.Vector2(0.26f, 2f)), "Rotated width incorrect.");
    }
    private static void ZeroDirectionRejected()
    {
        Check(!Weapon(MeleeShape2D.Line, 2.5f).ContainsPoint(Origin, Origin, Origin), "Zero direction must fail.");
    }
    private static void ChargeThresholds()
    {
        var club = Weapon(MeleeShape2D.Sector, 1.5f);
        Set(club, "chargeEnabled", true);
        Check(club.DamageForCharge(0.59f) == 1 && club.DamageForCharge(0.6f) == 2 &&
            club.DamageForCharge(1.19f) == 2 && club.DamageForCharge(1.2f) == 3, "Charge damage thresholds incorrect.");
        Check(!club.IsFullyCharged(1.19f) && club.IsFullyCharged(1.2f), "Shockwave threshold incorrect.");
        Set(club, "chargeEnabled", false);
        Check(club.DamageForCharge(10f) == 1 && !club.IsFullyCharged(10f), "Noncharging weapon received charge bonus.");
    }
    private static void EquipmentChangesResetChargeClock()
    {
        UnityEngine.Time.time = 0f;
        var loadout = Loadout(out _);
        UnityEngine.Time.time = 2f;
        loadout.TryEquipWeapon(Instance(MeleeShape2D.Line, 2.5f));
        UnityEngine.Time.time = 3f;
        WeaponInstance2D club = Instance(MeleeShape2D.Sector, 1.5f);
        loadout.TryEquipWeapon(club);
        Check(loadout.EquipmentVersion == 2 && loadout.EquippedAt == 3f, "Returning to Club must record a fresh charge start.");
        UnityEngine.Time.time = 4f;
        loadout.TryEquipWeapon(club);
        Check(loadout.EquipmentVersion == 3 && loadout.EquippedAt == 4f, "Picking up same type must also reset charge.");
    }
    private static void SpearTipSweetSpot()
    {
        var spear = Weapon(MeleeShape2D.Line, 2.5f);
        Set(spear, "damage", 1); Set(spear, "tipLength", 0.7f); Set(spear, "tipDamage", 2);
        Check(spear.DamageAtPoint(Origin, Right, new UnityEngine.Vector2(1.79f, 0f)) == 1 &&
            spear.DamageAtPoint(Origin, Right, new UnityEngine.Vector2(1.81f, 0f)) == 2, "Spear tip threshold incorrect.");
        Check(spear.DamageAtPoint(new UnityEngine.Vector2(3f, 2f), new UnityEngine.Vector2(0f, 2f),
            new UnityEngine.Vector2(3f, 4f)) == 2, "Rotated spear tip incorrect.");
        Set(spear, "tipLength", 0f);
        Check(spear.DamageAtPoint(Origin, Right, new UnityEngine.Vector2(2f, 0f)) == 1, "Zero tip length must disable bonus.");
    }
    private static void DataClampsInvalidNumbers()
    {
        var data = Weapon(MeleeShape2D.Line, -1f, 500f, -2f);
        Set(data, "damage", -1); Set(data, "attackInterval", 0f); Set(data, "manaOnHit", -5);
        Check(data.Range > 0f && data.Width > 0f && data.Angle == 360f && data.Damage == 1 && data.AttackInterval > 0f && data.ManaOnHit == 0, "Runtime clamps missing.");
    }
    private static PlayerWeaponLoadout2D Loadout(out PlayerHealth2D health)
    {
        health = new PlayerHealth2D();
        var loadout = new PlayerWeaponLoadout2D { Component = health };
        Set(loadout, "club", Weapon(MeleeShape2D.Sector, 1.5f));
        Call(loadout, "Awake"); return loadout;
    }
    private static void SelectionStartsClubAndPreservesResources()
    {
        var loadout = Loadout(out PlayerHealth2D health);
        Check(loadout.CurrentWeapon.Range == 1.5f, "Fresh run must start Club.");
        Check(loadout.TryEquipWeapon(Instance(MeleeShape2D.Line, 2.5f)) && loadout.CurrentWeapon.AttackShape == MeleeShape2D.Line, "Spear selection failed.");
        Check(loadout.TryEquipWeapon(Instance(MeleeShape2D.ArcBand, 2.3f)) && loadout.CurrentWeapon.Range == 2.3f, "Whip selection failed.");
        Check(!loadout.TryEquipWeapon(null) && health.Health == 10 && health.Mana == 100, "Selection changed resources or accepted null.");
        Check(Loadout(out _).CurrentWeapon.Range == 1.5f, "New instance must reset to Club.");
    }
    private static void TerminalStateBlocksSwitch()
    {
        var loadout = Loadout(out PlayerHealth2D health);
        health.CombatStopped = true;
        Check(!loadout.TryEquipWeapon(Instance(MeleeShape2D.Line, 2.5f)) && loadout.CurrentWeapon.Range == 1.5f, "Terminal state allowed switching.");
        health.CombatStopped = false; loadout.enabled = false;
        Check(!loadout.TryEquipWeapon(Instance(MeleeShape2D.ArcBand, 2.3f)), "Disabled component allowed switching.");
    }
    private static WeaponDropTable2D Table(float chance, out WeaponPartData2D club, out WeaponPartData2D spear, out WeaponPartData2D whip, float scale = 1f)
    {
        club = Weapon(MeleeShape2D.Sector, 1.5f); spear = Weapon(MeleeShape2D.Line, 2.5f); whip = Weapon(MeleeShape2D.ArcBand, 2.3f);
        var table = new WeaponDropTable2D();
        Set(table, "dropChance", chance);
        Set(table, "entries", new[] { new WeightedWeaponDrop2D { weapon = club, weight = 40f * scale },
            new WeightedWeaponDrop2D { weapon = spear, weight = 30f * scale }, new WeightedWeaponDrop2D { weapon = whip, weight = 30f * scale } });
        return table;
    }
    private static void ZeroChanceNeverDrops()
    {
        var table = Table(0f, out _, out _, out _);
        Check(!table.TryRoll(0f, 0f, out var data) && data == null, "0% must never drop, including roll zero.");
    }
    private static void WeightedDropsAndProbabilityGate()
    {
        var table = Table(0.4f, out var club, out var spear, out var whip);
        Check(!table.TryRoll(0.4f, 0f, out _), "40% gate incorrect.");
        Check(table.TryRoll(0.39f, 0.2f, out var a) && a == club && table.TryRoll(0.1f, 0.5f, out var b) && b == spear &&
            table.TryRoll(0.1f, 0.8f, out var c) && c == whip, "Weighted buckets incorrect.");
    }
    private static void FullChanceAndInvalidWeights()
    {
        var table = Table(1f, out _, out _, out var whip);
        Check(table.TryRoll(1f, 1f, out var last) && last == whip, "100%/inclusive random endpoint must drop.");
        Set(table, "entries", new[] { new WeightedWeaponDrop2D { weapon = whip, weight = 0f }, null, new WeightedWeaponDrop2D { weight = 5f } });
        Check(!table.TryRoll(0f, 0f, out _), "No valid positive weights must not drop.");
    }
    private static void ScaledWeightsPreserveRatios()
    {
        var table = Table(1f, out _, out var spear, out _, 0.1f);
        Check(table.TryRoll(0f, 0.5f, out var result) && result == spear, "Weights should be relative, not absolute percentages.");
    }
    private static void MissingDataFailsSafely()
    {
        var loadout = new PlayerWeaponLoadout2D { Component = new PlayerHealth2D() };
        Call(loadout, "Awake");
        Check(!loadout.enabled && loadout.CurrentWeapon == null, "Missing data did not disable safely.");
    }
    private static void RolledBoundsAndOriginalUnchanged()
    {
        var data = Weapon(MeleeShape2D.Sector, 1.5f);
        Set(data, "damage", 10); Set(data, "manaOnHit", 10);
        var settings = new WeaponRollSettings2D();
        var low = settings.Create(data, 0f, 0f, 0f, 0f);
        var high = settings.Create(data, 1f, 1f, 1f, 1f);
        Check(low.Damage == 8 && high.Damage == 12 && low.ManaOnHit == 8 && high.ManaOnHit == 12, "Integer roll bounds incorrect.");
        Check(Math.Abs(low.AttackInterval - 0.34f) < 0.0001f && Math.Abs(high.AttackInterval - 0.46f) < 0.0001f &&
            Math.Abs(low.Range - 1.35f) < 0.0001f && Math.Abs(high.Range - 1.65f) < 0.0001f, "Float roll bounds incorrect.");
        Check(data.Damage == 10 && data.AttackInterval == 0.4f && data.Range == 1.5f && data.ManaOnHit == 10, "Roll mutated base asset.");
        Check(WeaponInstance2D.Create(data, 0f, 0f, 0f, 0f).Damage == 1, "Damage must never be zero.");
    }
    private static void InstanceIdentitySurvivesReequip()
    {
        var loadout = Loadout(out _);
        var item = WeaponInstance2D.Create(Weapon(MeleeShape2D.Line, 2.5f), 1.1f, 0.9f, 1.07f, 1.2f);
        string id = item.InstanceId;
        loadout.TryEquipWeapon(item);
        var discarded = loadout.CurrentWeapon;
        loadout.TryEquipWeapon(Instance(MeleeShape2D.Sector, 1.5f));
        loadout.TryEquipWeapon(discarded);
        Check(ReferenceEquals(item, loadout.CurrentWeapon) && loadout.CurrentWeapon.InstanceId == id &&
            Math.Abs(loadout.CurrentWeapon.Range - 2.675f) < 0.0001f, "Reequip changed instance or rolled values.");
    }
    private static void SameTypeDistinctInstances()
    {
        var data = Weapon(MeleeShape2D.Sector, 1.5f);
        var a = WeaponInstance2D.Create(data, 1f, 0.85f, 0.9f, 0.8f);
        var b = WeaponInstance2D.Create(data, 1f, 1.15f, 1.1f, 1.2f);
        Check(a.InstanceId != b.InstanceId && ReferenceEquals(a.BaseData, b.BaseData) && a.AttackInterval < b.AttackInterval && a.Range < b.Range,
            "Same-type drops must have separate identities and stats.");
    }
    private static void SnapshotStatsDoNotReroll()
    {
        var data = Weapon(MeleeShape2D.Sector, 1.5f);
        var item = WeaponInstance2D.CreateBase(data);
        Set(data, "damage", 99); Set(data, "range", 99f); Set(data, "attackInterval", 99f); Set(data, "manaOnHit", 99);
        Check(item.Damage == 1 && item.Range == 1.5f && item.AttackInterval == 0.4f && item.ManaOnHit == 10,
            "Rolled main stats must be snapshots, not reads from mutable base values.");
    }
    private static void ScaledSpecialGeometry()
    {
        var data = Weapon(MeleeShape2D.ArcBand, 2.3f, 100f);
        Set(data, "innerRange", 1.8f);
        var whip = WeaponInstance2D.Create(data, 1f, 1f, 1.1f, 1f);
        Check(Math.Abs(whip.InnerRange - 1.98f) < 0.0001f && !whip.ContainsPoint(Origin, Right, new UnityEngine.Vector2(1.9f, 0f)) &&
            whip.ContainsPoint(Origin, Right, new UnityEngine.Vector2(2.4f, 0f)) && whip.Angle == 100f && whip.Width == data.Width, "Scaled whip geometry incorrect.");
        var spearData = Weapon(MeleeShape2D.Line, 2.5f);
        Set(spearData, "tipLength", 0.7f);
        var spear = WeaponInstance2D.Create(spearData, 1f, 1f, 1.1f, 1f);
        Check(spear.DamageAtPoint(Origin, Right, new UnityEngine.Vector2(1.97f, 0f)) == 1 &&
            spear.DamageAtPoint(Origin, Right, new UnityEngine.Vector2(2f, 0f)) == 2, "Scaled spear tip threshold incorrect.");
    }
    private static void TableCreatesInstances()
    {
        var table = Table(1f, out var club, out _, out _);
        Set(table, "rollSettings", new WeaponRollSettings2D());
        var a = table.CreateInstance(club); var b = table.CreateInstance(club);
        Check(a.IsValid && b.IsValid && a.InstanceId != b.InstanceId && a.BaseData == club, "Table did not create distinct instances.");
        Set(table, "rollSettings", null);
        Check(table.CreateInstance(club).Range == club.Range, "Missing settings must safely use base stats.");
    }
}
