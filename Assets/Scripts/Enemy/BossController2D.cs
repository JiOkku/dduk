using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyChase2D))]
public sealed class BossController2D : MonoBehaviour
{
    private EnemyChase2D enemy;
    private Rigidbody2D body;
    private PlayerHealth2D player;
    private BossData2D data;
    private float damageScale;
    private bool attacking, dashing;
    private Vector2 dashDirection;
    private float dashSpeed;
    private GameObject warning;
    private Collider2D[] owner;
    private SpriteRenderer sprite;
    public BossData2D Data => data;
    public EnemyChase2D Health => enemy;
    private bool Operational => data != null && enemy != null && !enemy.IsDefeated &&
        player != null && !player.CombatStopped && !SceneFlow2D.IsLoading;

    private void Awake()
    {
        enemy = GetComponent<EnemyChase2D>(); body = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>(); owner = GetComponentsInChildren<Collider2D>();
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }
    public void Initialize(BossData2D bossData, PlayerHealth2D target, float healthScale, float attackScale)
    {
        if (data != null) return;
        data = bossData; player = target; damageScale = attackScale;
        enemy.SetTarget(player.transform);
        enemy.ConfigureBoss(data, healthScale, attackScale);
        if (sprite != null) sprite.color = data.Color;
        enemy.Died += OnDeath;
        player.Died += StopFight;
        StartCoroutine(PatternLoop());
    }
    private void FixedUpdate()
    {
        if (!Operational) { body.linearVelocity = Vector2.zero; return; }
        if (enemy.TryGetHitVelocity(out Vector2 reaction)) { body.linearVelocity = reaction; return; }
        if (dashing) { body.linearVelocity = dashDirection * dashSpeed; return; }
        Vector2 delta = (Vector2)player.transform.position - body.position;
        body.linearVelocity = attacking ? Vector2.zero : delta.normalized * Mathf.Min(data.MoveSpeed, delta.magnitude / Time.fixedDeltaTime);
    }
    private void LateUpdate()
    {
        if (data != null && sprite != null)
            sprite.color = Time.time - enemy.LastDamageTime < 0.1f ? Color.white : data.Color;
    }
    private IEnumerator PatternLoop()
    {
        yield return new WaitForSeconds(data.AttackInterval);
        int index = 0;
        while (Operational)
        {
            BossPattern2D pattern = data.Patterns[index++ % data.Patterns.Length];
            Vector2 origin = body.position;
            Vector2 delta = (Vector2)player.transform.position - origin;
            Vector2 direction = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
            Vector2 areaCenter = MagicCombat2D.ClampTarget(origin,
                origin + Vector2.ClampMagnitude(delta, Mathf.Max(0.1f, pattern.range)), owner);
            attacking = true;
            ShowWarning(pattern, origin, direction, areaCenter);
            yield return new WaitForSeconds(Mathf.Max(0.1f, pattern.warningTime));
            ClearWarning();
            if (!Operational) yield break;
            int damage = Mathf.Max(1, Mathf.RoundToInt(pattern.damage * damageScale));
            switch (pattern.kind)
            {
                case BossPatternKind2D.Dash:
                    dashing = true; dashDirection = direction; dashSpeed = Mathf.Max(0.1f, pattern.dashSpeed);
                    enemy.ContactDamageOverride = damage;
                    yield return new WaitForSeconds(Mathf.Min(Mathf.Max(0.1f, pattern.dashDuration), Mathf.Max(0.1f, pattern.range) / dashSpeed));
                    dashing = false;
                    enemy.ContactDamageOverride = Mathf.Max(1, Mathf.RoundToInt(data.ContactDamage * damageScale));
                    break;
                case BossPatternKind2D.Projectile:
                    var projectile = Instantiate(data.ProjectilePrefab, transform.position, Quaternion.identity);
                    projectile.LaunchHostile(direction, enemy, player, damage,
                        pattern.projectileSpeed, pattern.projectileLifetime, pattern.range, data.Color);
                    break;
                case BossPatternKind2D.Area:
                    var playerCollider = player.GetComponent<Collider2D>();
                    Vector2 point = playerCollider != null ? playerCollider.ClosestPoint(areaCenter) : (Vector2)player.transform.position;
                    if (Vector2.Distance(point, areaCenter) <= Mathf.Max(0.1f, pattern.radius) && ClearPath(areaCenter, point))
                        player.TryTakeDamage(damage);
                    break;
            }
            body.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(Mathf.Max(0f, pattern.recoveryTime));
            attacking = false;
            yield return new WaitForSeconds(Mathf.Max(data.AttackInterval, pattern.cooldown));
        }
    }
    private bool ClearPath(Vector2 origin, Vector2 target)
    {
        Vector2 delta = target - origin;
        foreach (var hit in Physics2D.RaycastAll(origin, delta.normalized, delta.magnitude))
            if (!hit.collider.transform.IsChildOf(player.transform) && MagicCombat2D.IsObstacle(hit.collider, owner)) return false;
        return true;
    }
    private void ShowWarning(BossPattern2D pattern, Vector2 origin, Vector2 direction, Vector2 center)
    {
        ClearWarning();
        warning = new GameObject("Boss Warning: " + pattern.kind);
        warning.transform.SetParent(transform, false);
        var line = warning.AddComponent<LineRenderer>();
        line.useWorldSpace = true; line.widthMultiplier = 0.08f; line.sortingOrder = 9;
        line.sharedMaterial = data.EffectMaterial;
        line.startColor = line.endColor = new Color(1f, 0.2f, 0.1f, 0.9f);
        line.loop = pattern.kind == BossPatternKind2D.Area;
        if (line.loop)
        {
            line.positionCount = 48;
            for (int i = 0; i < 48; i++)
            {
                float angle = i * Mathf.PI * 2f / 48;
                line.SetPosition(i, (Vector3)center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Max(0.1f, pattern.radius));
            }
        }
        else
        {
            line.positionCount = 2; line.SetPosition(0, origin);
            float distance = pattern.kind == BossPatternKind2D.Dash ? Mathf.Min(pattern.range, pattern.dashSpeed * pattern.dashDuration) : pattern.range;
            line.SetPosition(1, MagicCombat2D.ClampTarget(origin, origin + direction * Mathf.Max(0.1f, distance), owner));
        }
    }
    private void ClearWarning()
    {
        if (warning == null) return;
        warning.SetActive(false); Destroy(warning); warning = null;
    }
    private void OnDeath(EnemyChase2D _) => StopFight();
    private void StopFight()
    {
        StopAllCoroutines(); attacking = false; dashing = false;
        if (body != null) body.linearVelocity = Vector2.zero;
        ClearWarning();
    }
    private void OnDisable()
    {
        StopFight();
        if (enemy != null) enemy.Died -= OnDeath;
        if (player != null) player.Died -= StopFight;
    }
}
