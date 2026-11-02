using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class TreasureChest2D : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float interactionRange = 1.5f;
    [SerializeField, Min(0)] private int currencyAmount = 10;
    [SerializeField, Range(0, 10)] private int weaponCount = 1;
    [SerializeField] private WeaponPartData2D weapon;
    [SerializeField] private WeaponRollSettings2D rollSettings;
    [SerializeField] private bool opened;
    private PlayerHealth2D observedPlayer;
    private Color closedColor;
    public bool Opened => opened;
    private void Awake() => closedColor = GetComponent<SpriteRenderer>().color;
    private void OnDisable()
    {
        if (observedPlayer != null) observedPlayer.Died -= ResetOnDeath;
    }
    private void ResetOnDeath()
    {
        opened = false;
        GetComponent<SpriteRenderer>().color = closedColor;
        if (observedPlayer != null) observedPlayer.Died -= ResetOnDeath;
        observedPlayer = null;
    }
    public void ResetForStage() => ResetOnDeath();

    public bool CanOpen(PlayerHealth2D player)
    {
        if (!isActiveAndEnabled || opened || player == null || player.CombatStopped ||
            Vector2.Distance(player.transform.position, transform.position) > Mathf.Max(0.01f, interactionRange)) return false;
        Vector2 delta = transform.position - player.transform.position;
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(player.transform.position, delta.normalized, delta.magnitude))
        {
            Collider2D collider = hit.collider;
            if (!collider.isTrigger && !collider.transform.IsChildOf(transform) && !collider.transform.IsChildOf(player.transform) &&
                collider.GetComponentInParent<EnemyChase2D>() == null && collider.GetComponentInParent<Projectile2D>() == null)
                return false;
        }
        return true;
    }

    public bool TryOpen(PlayerHealth2D player, DungeonRewards2D rewards)
    {
        if (!CanOpen(player) || rewards == null || !rewards.CanAcquire || (weaponCount > 0 && weapon == null)) return false;
        var batch = new List<RewardRecord2D>();
        if (currencyAmount > 0) batch.Add(RewardRecord2D.Currency(currencyAmount, RewardSource2D.Chest));
        int count = Mathf.Clamp(weaponCount, 0, 10);
        for (int i = 0; i < count; i++)
        {
            WeaponInstance2D item = rollSettings != null ? rollSettings.CreateRandom(weapon, RewardSource2D.Chest) :
                WeaponInstance2D.Create(weapon, 1f, 1f, 1f, 1f, RewardSource2D.Chest);
            batch.Add(RewardRecord2D.FromWeapon(item));
        }
        string weaponReward = count > 0 ? $" | {weapon.WeaponName} x{count} (lobby reward, not equipped)" : "";
        if (!rewards.TryAddBatch(batch, $"Chest: Export Tokens +{Mathf.Max(0, currencyAmount)}{weaponReward}\n" +
            "Export on dungeon clear; lost on death or leaving early.")) return false;
        opened = true;
        observedPlayer = player;
        observedPlayer.Died += ResetOnDeath;
        GetComponent<SpriteRenderer>().color = new Color(0.35f, 0.35f, 0.35f);
        return true;
    }
}
