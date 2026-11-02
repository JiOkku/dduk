using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(PolygonCollider2D))]
public sealed class EnemyChase2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float moveSpeed = 2f;
    [SerializeField, Min(0f)] private float stopDistance = 0f;
    [SerializeField, Min(1)] private int maxHealth = 1;
    [Tooltip("Runtime value; initialized from Max Health.")]
    [SerializeField] private int currentHealth;

    private Rigidbody2D body;
    private bool defeated;
    private Vector2 knockbackVelocity;
    private float knockbackUntil;
    private float stunnedUntil;

    public bool IsDefeated => defeated;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => Mathf.Max(1, maxHealth);
    public int ContactDamageOverride { get; set; }
    public bool ExternalMovement { get; private set; }
    public float LastDamageTime { get; private set; } = float.NegativeInfinity;
    public void ConfigureBoss(BossData2D data, float healthScale, float damageScale)
    {
        ExternalMovement = true;
        maxHealth = Mathf.Max(1, Mathf.RoundToInt(data.MaxHealth * healthScale));
        currentHealth = maxHealth;
        ContactDamageOverride = Mathf.Max(1, Mathf.RoundToInt(data.ContactDamage * damageScale));
        Element = data.Element;
    }
    public bool TryGetHitVelocity(out Vector2 velocity)
    {
        velocity = Time.time < knockbackUntil ? knockbackVelocity : Vector2.zero;
        return Time.time < knockbackUntil || Time.time < stunnedUntil;
    }
    public MagicElement2D Element { get; private set; }
    public float ContactDamageMultiplier { get; private set; } = 1f;
    public void ApplyStage(DungeonStageData2D stage, EnemyElementData2D data)
    {
        currentHealth = Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, maxHealth) * stage.HealthMultiplier));
        moveSpeed *= stage.SpeedMultiplier;
        ContactDamageMultiplier = stage.DamageMultiplier;
        Element = data.Element;
        foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>()) sprite.color = data.Color;
    }
    public event Action<EnemyChase2D> Died;
    public event Action<EnemyChase2D> Removed;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        currentHealth = Mathf.Max(1, maxHealth);
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.linearDamping = 0f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

    }

    private void Start()
    {
        if (target == null)
            Debug.LogWarning("Assign the Player Transform to EnemyChase2D.Target.", this);
    }

    public void SetTarget(Transform newTarget) => target = newTarget;

    private void FixedUpdate()
    {
        if (defeated || target == null)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }
        if (ExternalMovement) return;

        if (Time.time < knockbackUntil)
        {
            body.linearVelocity = knockbackVelocity;
            return;
        }
        if (Time.time < stunnedUntil)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = (Vector2)target.position - body.position;
        float distance = toTarget.magnitude;
        float remainingDistance = distance - Mathf.Max(0f, stopDistance);
        if (remainingDistance <= 0f)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        // Do not step past the stopping distance within a physics tick.
        float speed = Mathf.Min(Mathf.Max(0f, moveSpeed), remainingDistance / Time.fixedDeltaTime);
        body.linearVelocity = toTarget / distance * speed;
    }

    public void ApplyHitReaction(Vector2 direction, float speed, float pushDuration, float stunDuration)
    {
        if (defeated || !isActiveAndEnabled)
            return;
        if (speed > 0f && pushDuration > 0f && direction.sqrMagnitude > 0.0001f)
        {
            knockbackVelocity = direction.normalized * speed;
            knockbackUntil = Time.time + pushDuration;
            body.linearVelocity = knockbackVelocity;
        }
        if (stunDuration > 0f)
        {
            stunnedUntil = Mathf.Max(stunnedUntil, Time.time + stunDuration);
            if (Time.time >= knockbackUntil)
                body.linearVelocity = Vector2.zero;
        }
    }

    public bool TryTakeDamage(int damage, out bool killed)
    {
        killed = false;
        if (!isActiveAndEnabled || defeated || damage <= 0 || currentHealth <= 0)
            return false;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        LastDamageTime = Time.time;
        if (currentHealth == 0)
        {
            killed = true;
            Defeat();
        }
        return true;
    }

    public void Defeat()
    {
        if (defeated)
            return;

        defeated = true;
        currentHealth = 0;
        body.linearVelocity = Vector2.zero;
        body.simulated = false;
        Died?.Invoke(this);
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (body != null)
            body.linearVelocity = Vector2.zero;
    }

    private void OnDestroy() => Removed?.Invoke(this);
}
