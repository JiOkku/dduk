using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Aim after the following camera has reached its render position.
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class PlayerShooting2D : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Camera aimCamera;
    [SerializeField] private Projectile2D projectilePrefab;
    [SerializeField] private MagicCatalog2D magicCatalog;
    [SerializeField] private MagicData2D fallbackMagic;
    private MagicData2D magic;
    private WaveManager2D waves;

    private InputAction attackAction;
    private Collider2D[] ownerColliders;
    private PlayerMana2D mana;
    private PlayerHealth2D health;
    private float nextCastTime;

    public InputActionAsset InputActions => inputActions;
    public MagicData2D CurrentMagic => magic;
    public float CooldownRemaining => Mathf.Max(0f, nextCastTime - Time.time);
    public int ManaCost => magic != null ? magic.ManaCost : 0;
    public bool CanCast => isActiveAndEnabled && health != null && !health.CombatStopped && magic != null &&
        SceneManager.GetActiveScene().path == SceneFlow2D.DungeonScene && !SceneFlow2D.IsLoading && waves != null &&
        (waves.State == GameState2D.Playing || waves.State == GameState2D.WaveTransition ||
        waves.State == GameState2D.BossIntro || waves.State == GameState2D.BossFight) &&
        mana != null && mana.CanUseMana && CooldownRemaining <= 0f && mana.CurrentMana >= ManaCost;

    private void Awake()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;

        mana = GetComponent<PlayerMana2D>();
        health = GetComponent<PlayerHealth2D>();
        waves = FindAnyObjectByType<WaveManager2D>();

        if (aimCamera == null || projectilePrefab == null || !projectilePrefab.gameObject.activeSelf || mana == null || health == null)
        {
            Debug.LogError("Assign camera, active projectile prefab, valid Magic Catalog, mana and health.", this);
            enabled = false;
            return;
        }
        if (magicCatalog != null && magicCatalog.Validate(out _))
            magic = magicCatalog.Equipped(RewardBank2D.Snapshot());
        else
        {
            magic = fallbackMagic != null && fallbackMagic.IsValid ? fallbackMagic : magicCatalog != null ? magicCatalog.Find(MagicOwnership2D.DefaultId) : null;
            Debug.LogWarning("Magic catalog missing/invalid; using the configured default magic.", this);
        }
        if (magic == null)
        { Debug.LogError("No valid equipped or fallback magic. Casting disabled.", this); enabled = false; return; }

        // A local Input System action leaves the existing shared input asset untouched.
        attackAction = new InputAction("CastMagic", InputActionType.Button, "<Mouse>/rightButton");
        ownerColliders = GetComponentsInChildren<Collider2D>();
    }

    private void OnEnable()
    {
        attackAction?.Enable();
    }

    private void LateUpdate()
    {
        if (!Application.isFocused || Mouse.current == null || !CanCast ||
            !attackAction.WasPressedThisFrame())
            return;

        if (!TryGetAimPoint(out Vector2 point)) return;
        Vector2 delta = point - (Vector2)transform.position;
        bool centered = magic.Form == MagicForm2D.SelfArea || magic.Form == MagicForm2D.RadialProjectiles;
        if (delta.sqrMagnitude < 0.0001f && !centered && magic.Form != MagicForm2D.TargetArea) return;
        Vector2 direction = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
        if (!mana.TrySpend(ManaCost))
            return;

        nextCastTime = Time.time + magic.Cooldown;
        switch (magic.Form)
        {
            case MagicForm2D.Cone: MagicCombat2D.Cone(transform.position, direction, magic, ownerColliders); break;
            case MagicForm2D.TargetArea:
                Vector2 desired = (Vector2)transform.position + Vector2.ClampMagnitude(delta, magic.Range);
                MagicCombat2D.Area(MagicCombat2D.ClampTarget(transform.position, desired, ownerColliders), magic, ownerColliders);
                break;
            case MagicForm2D.SelfArea: MagicCombat2D.Area(transform.position, magic, ownerColliders); break;
            case MagicForm2D.BurstProjectiles: StartCoroutine(Burst(direction)); break;
            case MagicForm2D.RadialProjectiles:
                float start = Mathf.Atan2(direction.y, direction.x);
                for (int i = 0; i < magic.ProjectileCount; i++)
                {
                    float angle = start + i * Mathf.PI * 2f / magic.ProjectileCount;
                    Fire(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
                }
                break;
            default: Fire(direction); break;
        }
    }

    private void Fire(Vector2 direction)
    {
        Projectile2D projectile = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        projectile.LaunchMagic(direction, ownerColliders, magic);
    }
    private IEnumerator Burst(Vector2 direction)
    {
        for (int i = 0; i < magic.ProjectileCount; i++)
        {
            if (health.CombatStopped || SceneFlow2D.IsLoading) yield break;
            Fire(direction);
            if (i + 1 < magic.ProjectileCount) yield return new WaitForSeconds(magic.BurstInterval);
        }
    }

    public bool TryGetAimDirection(out Vector2 direction)
    {
        direction = Vector2.zero;
        if (!TryGetAimPoint(out Vector2 point)) return false;
        direction = point - (Vector2)transform.position;
        if (direction.sqrMagnitude < 0.0001f) return false;
        direction.Normalize();
        return true;
    }

    private bool TryGetAimPoint(out Vector2 point)
    {
        point = Vector2.zero;
        if (aimCamera == null || Mouse.current == null)
            return false;

        Ray ray = aimCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane playerPlane = new Plane(Vector3.forward, transform.position);
        if (!playerPlane.Raycast(ray, out float distance))
            return false;

        point = ray.GetPoint(distance);
        return true;
    }

    private void OnDisable()
    {
        attackAction?.Disable();
        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        attackAction?.Dispose();
    }
}
