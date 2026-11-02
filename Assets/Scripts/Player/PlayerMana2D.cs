using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth2D))]
public sealed class PlayerMana2D : MonoBehaviour
{
    [SerializeField, Min(0)] private int maxMana = 100;
    [SerializeField, Min(0)] private int startingMana = 100;
    [Tooltip("Runtime value; initialized from Starting Mana.")]
    [SerializeField] private int currentMana;

    private PlayerHealth2D health;
    private PlayerPermanentStats2D permanentStats;
    public int MaxMana => PermanentBonuses2D.AddToBase(Mathf.Max(0, maxMana), permanentStats != null ? permanentStats.Bonuses.Mana : 0);
    public int CurrentMana => Mathf.Clamp(currentMana, 0, MaxMana);
    public bool CanUseMana => isActiveAndEnabled && health != null && !health.CombatStopped;

    private void Awake()
    {
        health = GetComponent<PlayerHealth2D>();
        permanentStats = GetComponent<PlayerPermanentStats2D>();
        currentMana = Mathf.Clamp(startingMana, 0, MaxMana);
    }

    public bool TrySpend(int amount)
    {
        if (!CanUseMana || amount < 0 || CurrentMana < amount)
            return false;

        currentMana = CurrentMana - amount;
        return true;
    }

    public void RefillForRun() => currentMana = MaxMana;

    public void Restore(int amount)
    {
        if (!CanUseMana || amount <= 0)
            return;

        currentMana = CurrentMana + Mathf.Min(amount, MaxMana - CurrentMana);
    }
}
