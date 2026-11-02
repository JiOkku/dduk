using UnityEngine;

public enum MagicElement2D { Fire, Water, Grass }
public enum MagicForm2D { Projectile, ExplosiveProjectile, TargetArea, PiercingProjectile, SelfArea, BurstProjectiles, RadialProjectiles, Cone }

[CreateAssetMenu(fileName = "Magic", menuName = "Prototype/Magic Scroll")]
public sealed class MagicData2D : ScriptableObject
{
    [SerializeField] private string magicId;
    [SerializeField] private string magicName;
    [SerializeField, TextArea(2, 4)] private string description;
    [SerializeField] private MagicElement2D element;
    [SerializeField, Range(1, 3)] private int tier = 1;
    [SerializeField, Min(0)] private int manaCost = 20;
    [SerializeField, Min(0f)] private float cooldown = 1f;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField] private MagicForm2D attackForm;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 12f;
    [SerializeField, Min(0.1f)] private float lifetime = 2f;
    [SerializeField, Min(0.1f)] private float range = 6f;
    [SerializeField, Min(0f)] private float areaRadius;
    [SerializeField, Range(1f, 180f)] private float coneAngle = 60f;
    [SerializeField, Min(0.1f)] private float projectileScale = 1f;
    [SerializeField, Min(1)] private int projectileCount = 3;
    [SerializeField, Min(0.01f)] private float burstInterval = 0.12f;
    [SerializeField] private Color effectColor = Color.red;
    [SerializeField] private Sprite projectileSprite;
    [SerializeField] private Material effectMaterial;
    public string Id => magicId;
    public string Name => magicName;
    public string Description => description;
    public MagicElement2D Element => element;
    public int Tier => Mathf.Clamp(tier, 1, 3);
    public int ManaCost => Mathf.Max(0, manaCost);
    public float Cooldown => Mathf.Max(0f, cooldown);
    public int Damage => Mathf.Max(1, damage);
    public MagicForm2D Form => attackForm;
    public float ProjectileSpeed => Mathf.Max(0.1f, projectileSpeed);
    public float Lifetime => Mathf.Max(0.1f, lifetime);
    public float Range => Mathf.Max(0.1f, range);
    public float AreaRadius => Mathf.Max(0f, areaRadius);
    public float ConeAngle => Mathf.Clamp(coneAngle, 1f, 180f);
    public float ProjectileScale => Mathf.Max(0.1f, projectileScale);
    public bool ContainsConePoint(Vector2 origin, Vector2 direction, Vector2 point)
    {
        Vector2 delta = point - origin;
        return direction.sqrMagnitude > 0.0001f && delta.sqrMagnitude <= Range * Range &&
            Vector2.Angle(direction, delta) <= ConeAngle * 0.5f;
    }
    public int ProjectileCount => Mathf.Clamp(projectileCount, 1, 64);
    public float BurstInterval => Mathf.Max(0.01f, burstInterval);
    public Color EffectColor => effectColor;
    public Sprite ProjectileSprite => projectileSprite;
    public Material EffectMaterial => effectMaterial;
    public bool IsValid => !string.IsNullOrWhiteSpace(magicId) && !string.IsNullOrWhiteSpace(magicName) &&
        System.Enum.IsDefined(typeof(MagicForm2D), attackForm);
}
