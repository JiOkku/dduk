using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerShooting2D), typeof(PlayerMana2D))]
[RequireComponent(typeof(PlayerWeaponLoadout2D))]
public sealed class PlayerMelee2D : MonoBehaviour
{
    [Tooltip("Solid non-enemy colliders on these layers block melee. Leave Everything for the existing walls.")]
    [SerializeField] private LayerMask obstructionLayers = ~0;
    [SerializeField, Min(0.01f)] private float effectDuration = 0.15f;
    [Header("Charge indicator")]
    [SerializeField] private Vector2 chargeRingOffset = new Vector2(0f, -0.45f);
    [SerializeField, Min(0.01f)] private float chargeRingRadius = 0.55f;
    [SerializeField, Min(0.001f)] private float chargeRingWidth = 0.035f;
    [SerializeField] private Color chargeRingColor = new Color(1f, 0.85f, 0.2f, 0.9f);

    private PlayerShooting2D aiming;
    private PlayerMana2D mana;
    private PlayerHealth2D health;
    private PlayerWeaponLoadout2D loadout;
    private PlayerPermanentStats2D permanentStats;
    public int PermanentDamageBonus => permanentStats != null ? permanentStats.Bonuses.MeleeDamage : 0;
    private InputAction attackAction;
    private float nextAttackTime;
    private readonly HashSet<EnemyChase2D> hitEnemies = new HashSet<EnemyChase2D>();
    private LineRenderer attackOutline;
    private LineRenderer tipOutline;
    private LineRenderer chargeRing;
    private Material outlineMaterial;
    private float hideEffectTime;
    private float chargeStartedAt;
    private int equipmentVersion;
    public float ChargeElapsed => isActiveAndEnabled && health != null && !health.CombatStopped &&
        loadout != null && loadout.CurrentWeapon != null && loadout.CurrentWeapon.ChargeEnabled ?
        Mathf.Max(0f, Time.time - chargeStartedAt) : 0f;

    private void Awake()
    {
        aiming = GetComponent<PlayerShooting2D>();
        mana = GetComponent<PlayerMana2D>();
        health = GetComponent<PlayerHealth2D>();
        loadout = GetComponent<PlayerWeaponLoadout2D>();
        permanentStats = GetComponent<PlayerPermanentStats2D>();
        InputAction source = aiming.InputActions != null
            ? aiming.InputActions.FindAction("Player/Attack", false) : null;
        if (source == null || health == null)
        {
            Debug.LogError("Melee requires Player/Attack in PlayerShooting2D.Input Actions and PlayerHealth2D.", this);
            enabled = false;
            return;
        }

        attackAction = source.Clone();
        // Retain the existing left-click action while excluding its other device bindings.
        attackAction.bindingMask = new InputBinding { path = "<Mouse>/leftButton" };
        BuildEffect();
    }

    private void OnEnable()
    {
        attackAction?.Enable();
        chargeStartedAt = Time.time;
        equipmentVersion = loadout != null ? loadout.EquipmentVersion : 0;
    }

    private void LateUpdate()
    {
        if (equipmentVersion != loadout.EquipmentVersion)
        {
            equipmentVersion = loadout.EquipmentVersion;
            chargeStartedAt = loadout.EquippedAt;
        }
        if (attackOutline != null && Time.time >= hideEffectTime)
        {
            attackOutline.enabled = false;
            tipOutline.enabled = false;
        }
        RefreshChargeRing();

        if (!Application.isFocused || health.CombatStopped || !mana.CanUseMana ||
            loadout.CurrentWeapon == null ||
            Time.time < nextAttackTime || !attackAction.WasPressedThisFrame() ||
            !aiming.TryGetAimDirection(out Vector2 direction))
            return;

        WeaponInstance2D weapon = loadout.CurrentWeapon;
        float charge = ChargeElapsed;
        bool fullCharge = weapon.IsFullyCharged(charge);
        // Preserve the USED weapon's recovery even if another weapon is equipped immediately.
        nextAttackTime = Time.time + weapon.AttackInterval;
        chargeStartedAt = Time.time; // Every valid swing consumes charge, including misses.
        PerformAttack(direction, weapon, charge);
        if (fullCharge)
            PerformShockwave(direction, weapon);
        ShowEffect(direction, weapon, fullCharge);
        RefreshChargeRing();
    }

    private void PerformAttack(Vector2 direction, WeaponInstance2D weapon, float charge)
    {
        Vector2 origin = transform.position;
        Collider2D[] candidates;
        if (weapon.AttackShape == MeleeShape2D.Line)
        {
            Vector2 center = origin + direction * (weapon.Range * 0.5f);
            float rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            candidates = Physics2D.OverlapBoxAll(center, new Vector2(weapon.Range, weapon.Width), rotation);
        }
        else
            candidates = Physics2D.OverlapCircleAll(origin, weapon.Range);
        hitEnemies.Clear();
        foreach (Collider2D candidate in candidates)
        {
            if (candidate.isTrigger)
                continue;

            EnemyChase2D enemy = candidate.GetComponentInParent<EnemyChase2D>();
            if (enemy == null || enemy.IsDefeated || hitEnemies.Contains(enemy))
                continue;

            // Keep the existing collider-center convention for both shapes and wall visibility.
            Vector2 point = candidate.bounds.center;
            if (!weapon.ContainsPoint(origin, direction, point) || IsObstructed(origin, point))
                continue;

            hitEnemies.Add(enemy);
            int damage = weapon.ChargeEnabled ? weapon.DamageForCharge(charge) : weapon.DamageAtPoint(origin, direction, point);
            damage = PermanentBonuses2D.AddToBase(damage, PermanentDamageBonus);
            if (enemy.TryTakeDamage(damage, out bool killed))
            {
                if (!killed)
                    enemy.ApplyHitReaction(point - origin, weapon.KnockbackSpeed, weapon.KnockbackDuration, weapon.StunDuration);
                mana.Restore(weapon.ManaOnHit);
                if (killed)
                    mana.Restore(weapon.ManaOnKill);
            }
        }
    }

    private void PerformShockwave(Vector2 direction, WeaponInstance2D weapon)
    {
        Vector2 origin = transform.position;
        foreach (Collider2D candidate in Physics2D.OverlapCircleAll(origin, weapon.ShockwaveRange))
        {
            if (candidate.isTrigger)
                continue;
            EnemyChase2D enemy = candidate.GetComponentInParent<EnemyChase2D>();
            if (enemy == null || enemy.IsDefeated || hitEnemies.Contains(enemy))
                continue;
            Vector2 point = candidate.bounds.center;
            Vector2 delta = point - origin;
            if (delta.sqrMagnitude > weapon.ShockwaveRange * weapon.ShockwaveRange ||
                Vector2.Angle(direction, delta) > weapon.ShockwaveAngle * 0.5f || IsObstructed(origin, point))
                continue;
            hitEnemies.Add(enemy);
            // Normal enemy death events still award gold, but shockwaves restore no mana.
            enemy.TryTakeDamage(PermanentBonuses2D.AddToBase(weapon.ShockwaveDamage, PermanentDamageBonus), out _);
        }
    }

    private bool IsObstructed(Vector2 origin, Vector2 point)
    {
        Vector2 delta = point - origin;
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(origin, delta.normalized, delta.magnitude, obstructionLayers))
        {
            Collider2D collider = hit.collider;
            if (collider.isTrigger || collider.transform.IsChildOf(transform) ||
                collider.GetComponentInParent<EnemyChase2D>() != null ||
                collider.GetComponentInParent<Projectile2D>() != null)
                continue;
            return true;
        }
        return false;
    }

    private void BuildEffect()
    {
        GameObject effect = new GameObject("Melee Range");
        effect.transform.SetParent(transform, false);
        attackOutline = effect.AddComponent<LineRenderer>();
        attackOutline.useWorldSpace = true;
        attackOutline.loop = true;
        attackOutline.widthMultiplier = 0.035f;
        SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
        if (playerSprite != null)
        {
            attackOutline.sortingLayerID = playerSprite.sortingLayerID;
            attackOutline.sortingOrder = playerSprite.sortingOrder + 2;
        }
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            outlineMaterial = new Material(shader);
            attackOutline.sharedMaterial = outlineMaterial;
        }
        attackOutline.startColor = new Color(1f, 0.85f, 0.2f);
        attackOutline.endColor = attackOutline.startColor;
        attackOutline.enabled = false;
        GameObject tipEffect = new GameObject("Spear Tip Range");
        tipEffect.transform.SetParent(transform, false);
        tipOutline = tipEffect.AddComponent<LineRenderer>();
        tipOutline.useWorldSpace = true;
        tipOutline.loop = true;
        tipOutline.widthMultiplier = 0.05f;
        tipOutline.sortingLayerID = attackOutline.sortingLayerID;
        tipOutline.sortingOrder = attackOutline.sortingOrder + 1;
        tipOutline.sharedMaterial = attackOutline.sharedMaterial;
        tipOutline.startColor = Color.yellow;
        tipOutline.endColor = Color.yellow;
        tipOutline.enabled = false;
        GameObject chargeEffect = new GameObject("Melee Charge Ring");
        chargeEffect.transform.SetParent(transform, false);
        chargeRing = chargeEffect.AddComponent<LineRenderer>();
        chargeRing.useWorldSpace = true;
        chargeRing.loop = true;
        chargeRing.positionCount = 48;
        chargeRing.sortingLayerID = attackOutline.sortingLayerID;
        chargeRing.sortingOrder = attackOutline.sortingOrder - 1;
        chargeRing.sharedMaterial = attackOutline.sharedMaterial;
        chargeRing.enabled = false;
    }

    private void RefreshChargeRing()
    {
        if (chargeRing == null) return;
        WeaponInstance2D weapon = loadout != null ? loadout.CurrentWeapon : null;
        bool show = health != null && !health.CombatStopped && weapon != null &&
            weapon.ChargeEnabled && !weapon.IsFullyCharged(ChargeElapsed);
        chargeRing.enabled = show;
        if (!show) return;

        float remaining = 1f - Mathf.Clamp01(ChargeElapsed / Mathf.Max(0.001f, weapon.FullChargeTime));
        float radius = Mathf.Max(0.01f, chargeRingRadius) * remaining;
        Vector3 center = transform.position + (Vector3)chargeRingOffset;
        chargeRing.widthMultiplier = Mathf.Max(0.001f, chargeRingWidth) * remaining;
        chargeRing.startColor = chargeRing.endColor = chargeRingColor;
        for (int i = 0; i < chargeRing.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / chargeRing.positionCount;
            chargeRing.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }

    private void ShowEffect(Vector2 direction, WeaponInstance2D weapon, bool fullCharge)
    {
        Vector3 origin = transform.position;
        tipOutline.enabled = false;
        if (weapon.AttackShape == MeleeShape2D.Line)
        {
            Vector3 side = new Vector3(-direction.y, direction.x) * (weapon.Width * 0.5f);
            Vector3 tip = origin + (Vector3)(direction * weapon.Range);
            attackOutline.positionCount = 4;
            attackOutline.SetPosition(0, origin - side);
            attackOutline.SetPosition(1, tip - side);
            attackOutline.SetPosition(2, tip + side);
            attackOutline.SetPosition(3, origin + side);
            if (weapon.TipLength > 0f)
            {
                Vector3 tipStart = origin + (Vector3)(direction * (weapon.Range - weapon.TipLength));
                tipOutline.positionCount = 4;
                tipOutline.SetPosition(0, tipStart - side);
                tipOutline.SetPosition(1, tip - side);
                tipOutline.SetPosition(2, tip + side);
                tipOutline.SetPosition(3, tipStart + side);
                tipOutline.enabled = true;
            }
        }
        else
        {
            const int segments = 24;
            float sweep = weapon.Angle;
            float start = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - sweep * 0.5f;
            bool band = weapon.AttackShape == MeleeShape2D.ArcBand;
            attackOutline.positionCount = band ? (segments + 1) * 2 : segments + 2;
            if (!band)
                attackOutline.SetPosition(0, origin);
            for (int i = 0; i <= segments; i++)
            {
                float radians = (start + sweep * i / segments) * Mathf.Deg2Rad;
                Vector3 radial = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians));
                attackOutline.SetPosition(band ? i : i + 1, origin + radial * weapon.Range);
                if (band)
                    attackOutline.SetPosition(segments * 2 + 1 - i, origin + radial * weapon.InnerRange);
            }
        }
        Color effectColor = weapon.AttackShape == MeleeShape2D.Line ? Color.cyan :
            weapon.Angle > 90f ? new Color(1f, 0.4f, 0.9f) : new Color(1f, 0.85f, 0.2f);
        attackOutline.startColor = effectColor;
        attackOutline.endColor = effectColor;
        if (fullCharge)
        {
            const int segments = 24;
            float start = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - weapon.ShockwaveAngle * 0.5f;
            tipOutline.positionCount = segments + 2;
            tipOutline.SetPosition(0, origin);
            for (int i = 0; i <= segments; i++)
            {
                float radians = (start + weapon.ShockwaveAngle * i / segments) * Mathf.Deg2Rad;
                tipOutline.SetPosition(i + 1, origin + new Vector3(Mathf.Cos(radians), Mathf.Sin(radians)) * weapon.ShockwaveRange);
            }
            tipOutline.enabled = true;
        }
        hideEffectTime = Time.time + Mathf.Max(0.01f, effectDuration);
        attackOutline.enabled = true;
    }

    private void OnDisable()
    {
        attackAction?.Disable();
        if (attackOutline != null)
            attackOutline.enabled = false;
        if (tipOutline != null)
            tipOutline.enabled = false;
        if (chargeRing != null)
            chargeRing.enabled = false;
    }

    private void OnDestroy()
    {
        attackAction?.Dispose();
        if (outlineMaterial != null)
            Destroy(outlineMaterial);
    }
}
