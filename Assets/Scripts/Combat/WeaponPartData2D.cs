using UnityEngine;

public enum MeleeShape2D { Sector, Line, ArcBand }

[CreateAssetMenu(fileName = "WeaponPart", menuName = "Prototype/Weapon Part 2D")]
public sealed class WeaponPartData2D : ScriptableObject
{
    [SerializeField] private string weaponName = "Club";
    [SerializeField] private string weaponId;
    [SerializeField] private Color pickupColor = Color.white;
    [SerializeField] private MeleeShape2D attackShape = MeleeShape2D.Sector;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Min(0.01f)] private float attackInterval = 0.4f;
    [SerializeField, Min(0.01f)] private float range = 1.5f;
    [SerializeField, Range(1f, 360f)] private float angle = 90f;
    [Tooltip("Full width of a Line attack; ignored by Sector attacks.")]
    [SerializeField, Min(0.01f)] private float width = 0.5f;
    [SerializeField, Min(0)] private int manaOnHit = 10;
    [SerializeField, Min(0)] private int manaOnKill = 15;
    [Tooltip("ArcBand's inner dead zone, measured from player center.")]
    [SerializeField, Min(0f)] private float innerRange;
    [Tooltip("Line's tip sweet spot length. Zero disables tip bonus.")]
    [SerializeField, Min(0f)] private float tipLength;
    [SerializeField, Min(1)] private int tipDamage = 2;
    [SerializeField, Min(0f)] private float knockbackSpeed;
    [SerializeField, Min(0f)] private float knockbackDuration;
    [SerializeField, Min(0f)] private float stunDuration;
    [SerializeField] private bool chargeEnabled;
    [SerializeField, Min(0.01f)] private float partialChargeTime = 0.6f;
    [SerializeField, Min(0.01f)] private float fullChargeTime = 1.2f;
    [SerializeField, Min(1)] private int partialChargeDamage = 2;
    [SerializeField, Min(1)] private int fullChargeDamage = 3;
    [SerializeField, Min(0.01f)] private float shockwaveRange = 1.9f;
    [SerializeField, Range(1f, 360f)] private float shockwaveAngle = 110f;
    [SerializeField, Min(1)] private int shockwaveDamage = 1;

    public string WeaponName => weaponName;
    public string WeaponId => string.IsNullOrEmpty(weaponId) ? weaponName.ToLowerInvariant() : weaponId;
    public Color PickupColor => pickupColor;
    public MeleeShape2D AttackShape => attackShape;
    public int Damage => Mathf.Max(1, damage);
    public float AttackInterval => Mathf.Max(0.01f, attackInterval);
    public float Range => Mathf.Max(0.01f, range);
    public float Angle => Mathf.Clamp(angle, 1f, 360f);
    public float Width => Mathf.Max(0.01f, width);
    public int ManaOnHit => Mathf.Max(0, manaOnHit);
    public int ManaOnKill => Mathf.Max(0, manaOnKill);
    public float InnerRange => Mathf.Clamp(innerRange, 0f, Range);
    public float TipLength => Mathf.Clamp(tipLength, 0f, Range);
    public int TipDamage => Mathf.Max(1, tipDamage);
    public float KnockbackSpeed => Mathf.Max(0f, knockbackSpeed);
    public float KnockbackDuration => Mathf.Max(0f, knockbackDuration);
    public float StunDuration => Mathf.Max(0f, stunDuration);
    public bool ChargeEnabled => chargeEnabled;
    public float PartialChargeTime => Mathf.Max(0.01f, partialChargeTime);
    public float FullChargeTime => Mathf.Max(PartialChargeTime, fullChargeTime);
    public float ShockwaveRange => Mathf.Max(Range, shockwaveRange);
    public float ShockwaveAngle => Mathf.Clamp(shockwaveAngle, 1f, 360f);
    public int ShockwaveDamage => Mathf.Max(1, shockwaveDamage);
    public bool IsFullyCharged(float elapsed) => ChargeEnabled && elapsed >= FullChargeTime;
    public int DamageForCharge(float elapsed) => IsFullyCharged(elapsed) ? Mathf.Max(1, fullChargeDamage) :
        ChargeEnabled && elapsed >= PartialChargeTime ? Mathf.Max(1, partialChargeDamage) : Damage;

    public int DamageAtPoint(Vector2 origin, Vector2 direction, Vector2 point)
    {
        if (AttackShape == MeleeShape2D.Line && TipLength > 0f && direction.sqrMagnitude >= 0.0001f &&
            Vector2.Dot(point - origin, direction.normalized) >= Range - TipLength)
            return Mathf.Max(1, tipDamage);
        return Damage;
    }

    public bool ContainsPoint(Vector2 origin, Vector2 direction, Vector2 point)
        => ContainsPoint(origin, direction, point, Range, InnerRange);

    public bool ContainsPoint(Vector2 origin, Vector2 direction, Vector2 point, float effectiveRange, float effectiveInnerRange)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return false;
        direction.Normalize();
        Vector2 offset = point - origin;
        if (AttackShape == MeleeShape2D.Line)
        {
            float forward = Vector2.Dot(offset, direction);
            float sideways = Vector2.Dot(offset, new Vector2(-direction.y, direction.x));
            return forward >= 0f && forward <= effectiveRange && Mathf.Abs(sideways) <= Width * 0.5f;
        }
        return offset.sqrMagnitude <= effectiveRange * effectiveRange &&
            (AttackShape != MeleeShape2D.ArcBand || offset.sqrMagnitude >= effectiveInnerRange * effectiveInnerRange) &&
            Vector2.Angle(direction, offset) <= Angle * 0.5f;
    }
}
