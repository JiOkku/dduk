using System;
using UnityEngine;

[DefaultExecutionOrder(50)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth2D))]
public sealed class PlayerWeaponLoadout2D : MonoBehaviour
{
    [SerializeField] private WeaponPartData2D club;
    [SerializeField] private WeaponCatalog2D weaponCatalog;
    private PlayerHealth2D health;
    [Tooltip("Runtime instance; recreated as a base Club when the scene starts.")]
    [SerializeField] private WeaponInstance2D currentWeapon;

    public WeaponInstance2D CurrentWeapon => currentWeapon;
    public int EquipmentVersion { get; private set; }
    public float EquippedAt { get; private set; }
    public event Action<WeaponInstance2D> WeaponEquipped;

    private void Awake()
    {
        health = GetComponent<PlayerHealth2D>();
        if (club == null)
        {
            Debug.LogError("Assign starting Club data to PlayerWeaponLoadout2D.", this);
            enabled = false;
            return;
        }
        // A new run restores a separate runtime copy; storage itself is never consumed.
        currentWeapon = weaponCatalog != null && weaponCatalog.Validate() ?
            weaponCatalog.StartingWeapon(RewardBank2D.Snapshot()) : WeaponInstance2D.CreateBase(club);
        EquippedAt = Time.time;
    }

    public bool TryEquipWeapon(WeaponInstance2D selected)
    {
        if (!isActiveAndEnabled || health == null || health.CombatStopped)
            return false;
        if (selected == null || !selected.IsValid)
            return false;
        currentWeapon = selected;
        EquippedAt = Time.time;
        EquipmentVersion++;
        WeaponEquipped?.Invoke(selected);
        // Attack timers belong to the attack components, so switching resets no timer or resource.
        return true;
    }
}
