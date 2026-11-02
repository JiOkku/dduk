using System;
using UnityEngine;

// Runtime identity and rolled values. The shared base asset is never mutated.
[Serializable]
public sealed class WeaponInstance2D
{
    [SerializeField] private string instanceId;
    [SerializeField] private WeaponPartData2D baseData;
    [SerializeField] private int damage;
    [SerializeField] private float attackInterval;
    [SerializeField] private float range;
    [SerializeField] private int manaOnHit;
    [SerializeField] private float damageScale;
    [SerializeField] private float rangeScale;
    [SerializeField] private RewardSource2D source;

    public string InstanceId => instanceId;
    public WeaponPartData2D BaseData => baseData;
    public bool IsValid => baseData != null && !string.IsNullOrEmpty(instanceId);
    public RewardSource2D Source => source;
    public bool Exportable => RewardRules2D.CanExport(RewardKind2D.Weapon, source);
    public int Damage => damage;
    public float AttackInterval => attackInterval;
    public float Range => range;
    public int ManaOnHit => manaOnHit;
    public string WeaponName => baseData.WeaponName;
    public Color PickupColor => baseData.PickupColor;
    public MeleeShape2D AttackShape => baseData.AttackShape;
    public float Angle => baseData.Angle;
    public float Width => baseData.Width;
    public float InnerRange => Mathf.Clamp(baseData.InnerRange * rangeScale, 0f, Range);
    public float TipLength => Mathf.Clamp(baseData.TipLength * rangeScale, 0f, Range);
    public int ManaOnKill => baseData.ManaOnKill;
    public float KnockbackSpeed => baseData.KnockbackSpeed;
    public float KnockbackDuration => baseData.KnockbackDuration;
    public float StunDuration => baseData.StunDuration;
    public bool ChargeEnabled => baseData.ChargeEnabled;
    public float FullChargeTime => baseData.FullChargeTime;
    public float ShockwaveRange => Mathf.Max(Range, baseData.ShockwaveRange * rangeScale);
    public float ShockwaveAngle => baseData.ShockwaveAngle;
    public int ShockwaveDamage => ScaleDamage(baseData.ShockwaveDamage);
    public bool IsFullyCharged(float elapsed) => baseData.IsFullyCharged(elapsed);

    public static WeaponInstance2D CreateBase(WeaponPartData2D data) => Create(data, 1f, 1f, 1f, 1f, RewardSource2D.StartingEquipment);

    public static WeaponInstance2D Create(WeaponPartData2D data, float damageFactor, float intervalFactor, float rangeFactor, float manaFactor,
        RewardSource2D source = RewardSource2D.Monster)
    {
        if (data == null)
            return null;
        return new WeaponInstance2D
        {
            instanceId = Guid.NewGuid().ToString("N"),
            baseData = data,
            source = source,
            damageScale = Mathf.Max(0f, damageFactor),
            rangeScale = Mathf.Max(0.01f, rangeFactor),
            damage = Mathf.Max(1, Mathf.RoundToInt(data.Damage * Mathf.Max(0f, damageFactor))),
            attackInterval = Mathf.Max(0.01f, data.AttackInterval * Mathf.Max(0f, intervalFactor)),
            range = Mathf.Max(0.01f, data.Range * Mathf.Max(0.01f, rangeFactor)),
            manaOnHit = Mathf.Max(0, Mathf.RoundToInt(data.ManaOnHit * Mathf.Max(0f, manaFactor)))
        };
    }

    public WeaponSaveData2D ToSaveData() => new WeaponSaveData2D
    {
        instanceId = instanceId, definitionId = baseData.WeaponId, name = WeaponName, damage = Damage,
        attackInterval = AttackInterval, range = Range, manaOnHit = ManaOnHit,
        damageScale = damageScale, rangeScale = rangeScale, source = source, exportable = Exportable
    };
    public static WeaponInstance2D Restore(WeaponSaveData2D saved, WeaponPartData2D definition)
    {
        if (saved == null || !saved.IsValid || definition == null || definition.WeaponId != saved.definitionId ||
            saved.exportable != RewardRules2D.CanExport(RewardKind2D.Weapon, saved.source))
            return null;
        return new WeaponInstance2D
        {
            instanceId = saved.instanceId, baseData = definition, damage = saved.damage,
            attackInterval = saved.attackInterval, range = saved.range, manaOnHit = saved.manaOnHit,
            damageScale = saved.damageScale, rangeScale = saved.rangeScale, source = saved.source
        };
    }

    private int ScaleDamage(int value) => Mathf.Max(1, Mathf.RoundToInt(value * damageScale));
    public bool ContainsPoint(Vector2 origin, Vector2 direction, Vector2 point) =>
        baseData.ContainsPoint(origin, direction, point, Range, InnerRange);
    public int DamageForCharge(float elapsed) => !ChargeEnabled || elapsed < baseData.PartialChargeTime ? Damage :
        ScaleDamage(baseData.DamageForCharge(elapsed));
    public int DamageAtPoint(Vector2 origin, Vector2 direction, Vector2 point)
    {
        if (AttackShape == MeleeShape2D.Line && TipLength > 0f && direction.sqrMagnitude >= 0.0001f &&
            Vector2.Dot(point - origin, direction.normalized) >= Range - TipLength)
            return ScaleDamage(baseData.TipDamage);
        return Damage;
    }
}
