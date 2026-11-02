using UnityEngine;

[CreateAssetMenu(fileName = "WeaponRollSettings", menuName = "Prototype/Weapon Roll Settings 2D")]
public sealed class WeaponRollSettings2D : ScriptableObject
{
    [Tooltip("Multipliers, not percentages: 0.8 means 80%.")]
    [SerializeField] private Vector2 damageMultipliers = new Vector2(0.8f, 1.2f);
    [SerializeField] private Vector2 intervalMultipliers = new Vector2(0.85f, 1.15f);
    [SerializeField] private Vector2 rangeMultipliers = new Vector2(0.9f, 1.1f);
    [SerializeField] private Vector2 manaMultipliers = new Vector2(0.8f, 1.2f);

    public WeaponInstance2D CreateRandom(WeaponPartData2D data, RewardSource2D source = RewardSource2D.Monster) =>
        Create(data, Random.value, Random.value, Random.value, Random.value, source);
    public WeaponInstance2D Create(WeaponPartData2D data, float damageRoll, float intervalRoll, float rangeRoll, float manaRoll,
        RewardSource2D source = RewardSource2D.Monster) =>
        WeaponInstance2D.Create(data, Factor(damageMultipliers, damageRoll), Factor(intervalMultipliers, intervalRoll),
            Factor(rangeMultipliers, rangeRoll), Factor(manaMultipliers, manaRoll), source);

    private static float Factor(Vector2 limits, float roll)
    {
        float min = Mathf.Max(0f, Mathf.Min(limits.x, limits.y));
        float max = Mathf.Max(min, Mathf.Max(limits.x, limits.y));
        return Mathf.Lerp(min, max, Mathf.Clamp01(roll));
    }
}
