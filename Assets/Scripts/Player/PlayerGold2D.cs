using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth2D))]
public sealed class PlayerGold2D : MonoBehaviour
{
    [SerializeField] private int currentGold;
    private PlayerHealth2D health;
    public int CurrentGold => currentGold;
    public int EarnedGold { get; private set; }

    private void Awake()
    {
        health = GetComponent<PlayerHealth2D>();
        currentGold = 0;
        EarnedGold = 0;
    }

    private void OnEnable()
    {
        health = GetComponent<PlayerHealth2D>();
        health.Died += ResetGold;
    }

    private void OnDisable()
    {
        if (health != null)
            health.Died -= ResetGold;
    }

    public void Award(int amount)
    {
        if (!isActiveAndEnabled || health == null || health.CombatStopped || amount <= 0)
            return;
        int credited = Mathf.Min(amount, int.MaxValue - currentGold);
        currentGold += credited;
        EarnedGold += Mathf.Min(credited, int.MaxValue - EarnedGold);
    }

    public void ResetGold() => currentGold = 0;
}
