using System;
using UnityEngine;

public enum BossPatternKind2D { Dash, Projectile, Area }

[Serializable]
public sealed class BossPattern2D
{
    public BossPatternKind2D kind;
    [Min(1)] public int damage = 1;
    [Min(0.1f)] public float warningTime = 0.8f;
    [Min(0f)] public float recoveryTime = 0.6f;
    [Min(0f)] public float cooldown = 1.5f;
    [Min(0.1f)] public float range = 5f;
    [Min(0.1f)] public float radius = 1.5f;
    [Min(0.1f)] public float dashSpeed = 8f;
    [Min(0.1f)] public float dashDuration = 0.6f;
    [Min(0.1f)] public float projectileSpeed = 6f;
    [Min(0.1f)] public float projectileLifetime = 3f;
}

[CreateAssetMenu(fileName = "Boss", menuName = "Prototype/Boss")]
public sealed class BossData2D : ScriptableObject
{
    [SerializeField] private string bossId;
    [SerializeField] private string bossName;
    [SerializeField] private MagicElement2D element;
    [SerializeField, Min(1)] private int maxHealth = 20;
    [SerializeField, Min(0f)] private float moveSpeed = 1.5f;
    [SerializeField, Min(1)] private int contactDamage = 1;
    [SerializeField, Min(0.1f)] private float attackInterval = 1.5f;
    [SerializeField] private BossPattern2D[] patterns;
    [SerializeField] private EnemyChase2D prefab;
    [SerializeField] private Projectile2D projectilePrefab;
    [SerializeField] private Material effectMaterial;
    [SerializeField] private Color color = Color.red;
    [SerializeField, Min(0)] private int firstKillStones = 5;
    [SerializeField, Min(0)] private int repeatKillStones = 2;
    public string Id => bossId;
    public string Name => bossName;
    public MagicElement2D Element => element;
    public int MaxHealth => Mathf.Max(1, maxHealth);
    public float MoveSpeed => Mathf.Max(0f, moveSpeed);
    public int ContactDamage => Mathf.Max(1, contactDamage);
    public float AttackInterval => Mathf.Max(0.1f, attackInterval);
    public BossPattern2D[] Patterns => patterns;
    public EnemyChase2D Prefab => prefab;
    public Projectile2D ProjectilePrefab => projectilePrefab;
    public Material EffectMaterial => effectMaterial;
    public Color Color => color;
    public int FirstKillStones => Mathf.Max(0, firstKillStones);
    public int RepeatKillStones => Mathf.Max(0, repeatKillStones);
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(bossId) || string.IsNullOrWhiteSpace(bossName) || prefab == null ||
            !prefab.gameObject.activeSelf || prefab.GetComponent<BossController2D>() == null || prefab.GetComponent<PolygonCollider2D>() == null ||
            patterns == null || patterns.Length == 0) return false;
        foreach (var pattern in patterns)
            if (pattern == null || !Enum.IsDefined(typeof(BossPatternKind2D), pattern.kind) ||
                (pattern.kind == BossPatternKind2D.Projectile &&
                (projectilePrefab == null || !projectilePrefab.gameObject.activeSelf))) return false;
        return true;
    }
}
