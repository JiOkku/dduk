using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerHealth2D : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 10;
    [SerializeField, Min(1)] private int contactDamage = 1;
    [SerializeField, Min(0.1f)] private float invulnerabilityDuration = 1f;
    [Tooltip("Runtime value. Initialized from Max Health when Play starts.")]
    [SerializeField] private int currentHealth;

    private Rigidbody2D body;
    private PlayerPermanentStats2D permanentStats;
    private float nextDamageTime;
    private bool isDead;
    private bool stageCleared;
    private bool resumeMovement, resumeShooting, resumeMelee;
    private RigidbodyConstraints2D previousConstraints;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => PermanentBonuses2D.AddToBase(Mathf.Max(1, maxHealth), permanentStats != null ? permanentStats.Bonuses.Health : 0);
    public bool IsDead => isDead;
    public bool CombatStopped => isDead || stageCleared;
    public event Action Died;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        permanentStats = GetComponent<PlayerPermanentStats2D>();
        currentHealth = MaxHealth;
        nextDamageTime = float.NegativeInfinity;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryContactDamage(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Sustained contact can deal damage again after invulnerability expires.
        TryContactDamage(collision);
    }

    private void TryContactDamage(Collision2D collision)
    {
        if (!isActiveAndEnabled || CombatStopped || Time.time < nextDamageTime)
            return;

        EnemyChase2D enemy = collision.collider.GetComponentInParent<EnemyChase2D>();
        if (enemy == null || !enemy.isActiveAndEnabled || enemy.IsDefeated)
            return;

        int damage = enemy.ContactDamageOverride > 0 ? enemy.ContactDamageOverride :
            Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, contactDamage) * enemy.ContactDamageMultiplier));
        TryTakeDamage(damage);
    }

    public bool TryTakeDamage(int damage)
    {
        if (!isActiveAndEnabled || CombatStopped || damage <= 0 || Time.time < nextDamageTime) return false;
        nextDamageTime = Time.time + Mathf.Max(0.1f, invulnerabilityDuration);
        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log($"Player HP: {currentHealth}/{MaxHealth}", this);

        if (currentHealth == 0)
            Die();
        return true;
    }

    private void Die()
    {
        isDead = true;
        StopControls();
        Debug.Log("Player defeated.", this);
        Died?.Invoke();
    }

    public void StopForStageClear()
    {
        if (isDead || stageCleared)
            return;
        stageCleared = true;
        StopControls();
    }

    private void StopControls()
    {
        previousConstraints = body.constraints;
        PlayerMovement2D movement = GetComponent<PlayerMovement2D>();
        resumeMovement = movement != null && movement.enabled;
        if (movement != null)
            movement.enabled = false;

        PlayerShooting2D shooting = GetComponent<PlayerShooting2D>();
        resumeShooting = shooting != null && shooting.enabled;
        if (shooting != null)
            shooting.enabled = false;

        PlayerMelee2D melee = GetComponent<PlayerMelee2D>();
        resumeMelee = melee != null && melee.enabled;
        if (melee != null)
            melee.enabled = false;

        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        // Keep the defeated player from being pushed by the remaining enemies.
        body.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    // Keep current HP, invulnerability and attack cooldown fields when changing stages.
    public void ResumeAfterStage()
    {
        if (isDead || !stageCleared) return;
        stageCleared = false;
        body.constraints = previousConstraints;
        var movement = GetComponent<PlayerMovement2D>();
        var shooting = GetComponent<PlayerShooting2D>();
        var melee = GetComponent<PlayerMelee2D>();
        if (movement != null && resumeMovement) movement.enabled = true;
        if (shooting != null && resumeShooting) shooting.enabled = true;
        if (melee != null && resumeMelee) melee.enabled = true;
    }
}
