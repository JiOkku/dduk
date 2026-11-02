using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponCatalog", menuName = "Prototype/Weapon Catalog")]
public sealed class WeaponCatalog2D : ScriptableObject
{
    [SerializeField] private WeaponPartData2D defaultWeapon;
    [SerializeField] private WeaponPartData2D[] weapons;
    public WeaponPartData2D DefaultWeapon => defaultWeapon;
    public WeaponPartData2D Find(string id)
    {
        if (weapons != null) foreach (var weapon in weapons)
            if (weapon != null && weapon.WeaponId == id) return weapon;
        return null;
    }
    public bool Validate()
    {
        if (defaultWeapon == null || weapons == null) return false;
        var ids = new HashSet<string>();
        foreach (var weapon in weapons)
            if (weapon == null || string.IsNullOrWhiteSpace(weapon.WeaponId) || !ids.Add(weapon.WeaponId)) return false;
        return Find(defaultWeapon.WeaponId) == defaultWeapon;
    }
    public WeaponInstance2D StartingWeapon(RewardProfile2D profile)
    {
        if (profile != null && !string.IsNullOrEmpty(profile.equippedWeaponInstanceId) && profile.weapons != null)
            foreach (var saved in profile.weapons)
                if (saved.instanceId == profile.equippedWeaponInstanceId)
                {
                    var restored = WeaponInstance2D.Restore(saved, Find(saved.definitionId));
                    if (restored != null) return restored;
                }
        return WeaponInstance2D.CreateBase(defaultWeapon);
    }
}
