using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyChase2D))]
public sealed class EnemyWeaponDrop2D : MonoBehaviour
{
    [SerializeField] private WeaponDropTable2D dropTable;
    [SerializeField] private WeaponPickup2D pickupPrefab;
    private EnemyChase2D enemy;
    private bool handled;

    private void OnEnable()
    {
        enemy = GetComponent<EnemyChase2D>();
        enemy.Died += HandleDeath;
    }
    private void OnDisable()
    {
        if (enemy != null)
            enemy.Died -= HandleDeath;
    }
    private void HandleDeath(EnemyChase2D defeated)
    {
        if (handled)
            return;
        handled = true;
        if (dropTable == null || pickupPrefab == null)
        {
            Debug.LogWarning("Assign Drop Table and Pickup Prefab to EnemyWeaponDrop2D.", this);
            return;
        }
        if (dropTable.TryRoll(Random.value, Random.value, out WeaponPartData2D weapon))
            WeaponPickup2D.Spawn(pickupPrefab, dropTable.CreateInstance(weapon), defeated.transform.position);
    }
}
