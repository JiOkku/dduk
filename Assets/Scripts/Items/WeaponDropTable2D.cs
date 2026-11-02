using System;
using UnityEngine;

[Serializable]
public sealed class WeightedWeaponDrop2D
{
    public WeaponPartData2D weapon;
    [Min(0f)] public float weight = 1f;
}

[CreateAssetMenu(fileName = "WeaponDropTable", menuName = "Prototype/Weapon Drop Table 2D")]
public sealed class WeaponDropTable2D : ScriptableObject
{
    [Tooltip("0 = no drops, 1 = always drop.")]
    [SerializeField, Range(0f, 1f)] private float dropChance = 0.4f;
    [SerializeField] private WeightedWeaponDrop2D[] entries;
    [SerializeField] private WeaponRollSettings2D rollSettings;

    public WeaponInstance2D CreateInstance(WeaponPartData2D data) =>
        rollSettings != null ? rollSettings.CreateRandom(data) : WeaponInstance2D.Create(data, 1f, 1f, 1f, 1f);

    // Explicit rolls allow deterministic checks without changing Unity's random state.
    public bool TryRoll(float chanceRoll, float weightRoll, out WeaponPartData2D weapon)
    {
        weapon = null;
        float chance = Mathf.Clamp01(dropChance);
        if (chance <= 0f || (chance < 1f && chanceRoll >= chance) || entries == null)
            return false;
        float total = 0f;
        foreach (WeightedWeaponDrop2D entry in entries)
            if (entry != null && entry.weapon != null && entry.weight > 0f)
                total += entry.weight;
        if (total <= 0f)
            return false;
        float remaining = Mathf.Clamp01(weightRoll) * total;
        foreach (WeightedWeaponDrop2D entry in entries)
        {
            if (entry == null || entry.weapon == null || entry.weight <= 0f)
                continue;
            weapon = entry.weapon;
            if (remaining < entry.weight)
                return true;
            remaining -= entry.weight;
        }
        return weapon != null; // Inclusive random endpoint 1 selects the final valid entry.
    }
}
