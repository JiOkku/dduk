using UnityEngine;

// Loaded once for a fresh dungeon; neither base stats nor weapon instances are rewritten.
[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public sealed class PlayerPermanentStats2D : MonoBehaviour
{
    [SerializeField] private PermanentGrowthData2D growthData;
    public PermanentBonuses2D Bonuses { get; private set; }
    private void Awake()
    {
        if (growthData == null || !growthData.Validate(out _))
        { Debug.LogError("Connect valid Permanent Growth data; using base player stats.", this); return; }
        Bonuses = PermanentBonuses2D.From(RewardBank2D.Snapshot(), growthData);
    }
}
