using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public sealed class Projectile2D : MonoBehaviour
{
    private static readonly HashSet<Projectile2D> active = new HashSet<Projectile2D>();
    [SerializeField, Min(0.1f)] private float speed = 12f;
    [SerializeField, Min(0.1f)] private float lifetime = 2f;

    private Rigidbody2D body;
    private bool consumed;
    private int damage = 1;
    private MagicData2D magic;
    private Collider2D[] owner;
    private Vector2 origin;
    private Vector2 direction;
    private float travelSpeed;
    private float expiresAt;
    private bool hostile;
    private EnemyChase2D hostileSource;
    private PlayerHealth2D hostileTarget;
    private float hostileRange;
    private readonly HashSet<EnemyChase2D> hitEnemies = new HashSet<EnemyChase2D>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => active.Clear();
    private void OnEnable()
    {
        Collider2D own = GetComponent<CircleCollider2D>();
        foreach (Projectile2D other in active)
            if (other != null) Physics2D.IgnoreCollision(own, other.GetComponent<CircleCollider2D>());
        active.Add(this);
    }
    private void OnDisable() => active.Remove(this);

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.linearDamping = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        GetComponent<CircleCollider2D>().isTrigger = false;
        expiresAt = Time.time + Mathf.Max(0.1f, lifetime);
    }

    public void Launch(Vector2 direction, Collider2D[] ownerColliders, int attackDamage = 1)
    {
        damage = Mathf.Max(1, attackDamage);
        owner = ownerColliders;
        origin = body.position;
        this.direction = direction.normalized;
        travelSpeed = Mathf.Max(0.1f, speed);
        // Ignore only the shooter; keep ordinary collision with the existing walls.
        Collider2D projectileCollider = GetComponent<CircleCollider2D>();
        foreach (Collider2D ownerCollider in ownerColliders)
        {
            if (ownerCollider != null)
                Physics2D.IgnoreCollision(projectileCollider, ownerCollider);
        }

        body.linearVelocity = direction.normalized * Mathf.Max(0.1f, speed);
    }

    public void LaunchMagic(Vector2 direction, Collider2D[] ownerColliders, MagicData2D data)
    {
        magic = data;
        Launch(direction, ownerColliders, data.Damage);
        travelSpeed = data.ProjectileSpeed;
        transform.localScale *= data.ProjectileScale;
        expiresAt = Time.time + data.Lifetime;
        body.linearVelocity = this.direction * travelSpeed;
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        if (sprite != null)
        {
            sprite.color = data.EffectColor;
            if (data.ProjectileSprite != null) sprite.sprite = data.ProjectileSprite;
        }
    }

    public void LaunchHostile(Vector2 direction, EnemyChase2D source, PlayerHealth2D target,
        int attackDamage, float projectileSpeed, float duration, float range, Color color)
    {
        hostile = true; hostileSource = source; hostileTarget = target;
        Launch(direction, source.GetComponentsInChildren<Collider2D>(), attackDamage);
        travelSpeed = Mathf.Max(0.1f, projectileSpeed);
        hostileRange = Mathf.Max(0.1f, range);
        expiresAt = Time.time + Mathf.Max(0.1f, duration);
        body.linearVelocity = this.direction * travelSpeed;
        foreach (var enemy in FindObjectsByType<EnemyChase2D>())
            foreach (var part in enemy.GetComponentsInChildren<Collider2D>())
                Physics2D.IgnoreCollision(GetComponent<CircleCollider2D>(), part);
        var sprite = GetComponent<SpriteRenderer>();
        if (sprite != null) sprite.color = color;
    }

    private void Update()
    {
        if (hostile && (hostileSource == null || !hostileSource.isActiveAndEnabled || hostileSource.IsDefeated || hostileTarget == null || hostileTarget.CombatStopped))
        { gameObject.SetActive(false); Destroy(gameObject); return; }
        if (Time.time >= expiresAt || (magic != null && Vector2.Distance(origin, body.position) >= magic.Range) ||
            (hostile && Vector2.Distance(origin, body.position) >= hostileRange))
            Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (consumed)
            return;
        if (hostile)
        {
            consumed = true;
            var player = collision.collider.GetComponentInParent<PlayerHealth2D>();
            if (player != null) player.TryTakeDamage(damage);
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            Destroy(gameObject);
            return;
        }

        EnemyChase2D enemy = collision.collider.GetComponentInParent<EnemyChase2D>();
        if (magic != null && magic.Form == MagicForm2D.PiercingProjectile && enemy != null)
        {
            if (hitEnemies.Add(enemy)) enemy.TryTakeDamage(damage, out _);
            Collider2D own = GetComponent<CircleCollider2D>();
            foreach (Collider2D part in enemy.GetComponentsInChildren<Collider2D>()) Physics2D.IgnoreCollision(own, part);
            body.linearVelocity = direction * travelSpeed;
            return;
        }
        consumed = true;
        if (magic != null && magic.Form == MagicForm2D.ExplosiveProjectile)
        {
            Vector2 impact = collision.contactCount > 0 ? collision.GetContact(0).point - direction * 0.06f : body.position;
            MagicCombat2D.Area(impact, magic, owner);
        }
        else if (enemy != null)
            enemy.TryTakeDamage(damage, out _);

        body.linearVelocity = Vector2.zero;
        body.simulated = false;
        Destroy(gameObject);
    }
}
