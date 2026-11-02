using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(60)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerWeaponLoadout2D))]
public sealed class PlayerWeaponPickup2D : MonoBehaviour
{
    [SerializeField] private WeaponPickup2D pickupPrefab;
    [SerializeField, Min(0.01f)] private float pickupRange = 1.5f;
    [SerializeField] private Rect roomBounds = new Rect(-7.5f, -4.5f, 15f, 9f);
    [SerializeField, Min(0.01f)] private float dropDistance = 0.8f;
    [SerializeField, Min(0.01f)] private float itemClearance = 0.25f;
    [SerializeField, Min(1)] private int placementAttempts = 24;
    [SerializeField] private LayerMask blockingLayers = ~0;
    [SerializeField] private DungeonRewards2D rewards;
    private TreasureChest2D chest;
    private PlayerWeaponLoadout2D loadout;
    private PlayerHealth2D health;
    private int lastPickupFrame = -1;
    private float blockedUntil;
    public WeaponPickup2D Nearest { get; private set; }
    private bool ChestAvailable => chest != null && rewards != null && rewards.CanAcquire && chest.CanOpen(health);
    public string Prompt => health == null || health.CombatStopped ? "" : ChestAvailable ? "E: Open Chest" : Time.time < blockedUntil ?
        "No room to drop equipped weapon" : Nearest != null && Nearest.Available ?
        $"E: Pick up {Nearest.Weapon.WeaponName}" : "";

    private void Awake()
    {
        loadout = GetComponent<PlayerWeaponLoadout2D>();
        health = GetComponent<PlayerHealth2D>();
        chest = FindAnyObjectByType<TreasureChest2D>();
        if (rewards == null) rewards = FindAnyObjectByType<DungeonRewards2D>();
        if (pickupPrefab == null || health == null)
        {
            Debug.LogError("Assign Pickup Prefab and PlayerHealth2D to PlayerWeaponPickup2D.", this);
            enabled = false;
        }
    }
    private void Update()
    {
        Nearest = null;
        if (health.CombatStopped)
            return;
        float nearestDistance = Mathf.Max(0.01f, pickupRange);
        foreach (WeaponPickup2D item in WeaponPickup2D.Active)
        {
            if (item == null || !item.Available)
                continue;
            float distance = Vector2.Distance(transform.position, item.transform.position);
            if (distance > nearestDistance || !ClearPath(transform.position, item.transform.position))
                continue;
            nearestDistance = distance;
            Nearest = item;
        }
        if (Application.isFocused && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (ChestAvailable)
            {
                if (chest.TryOpen(health, rewards)) lastPickupFrame = Time.frameCount;
            }
            else TryPickupNearest();
        }
    }

    public bool TryPickupNearest()
    {
        if (!isActiveAndEnabled || health.CombatStopped || ChestAvailable || lastPickupFrame == Time.frameCount ||
            Nearest == null || Vector2.Distance(transform.position, Nearest.transform.position) > Mathf.Max(0.01f, pickupRange) ||
            !ClearPath(transform.position, Nearest.transform.position) || !Nearest.TryReserve())
            return false;
        lastPickupFrame = Time.frameCount;
        WeaponPickup2D target = Nearest;
        WeaponInstance2D previous = loadout.CurrentWeapon;
        if (previous == null || !TryFindDropPosition(out Vector3 position))
        {
            target.Release();
            blockedUntil = Time.time + 1.5f;
            return false;
        }
        WeaponPickup2D oldItem = WeaponPickup2D.Spawn(pickupPrefab, previous, position);
        if (oldItem == null || !loadout.TryEquipWeapon(target.Weapon))
        {
            if (oldItem != null)
                oldItem.Consume();
            target.Release();
            return false;
        }
        target.Consume();
        Nearest = null;
        blockedUntil = 0f;
        return true;
    }

    private bool TryFindDropPosition(out Vector3 position)
    {
        position = transform.position;
        Physics2D.SyncTransforms();
        int attempts = Mathf.Clamp(placementAttempts, 1, 128);
        float clearance = Mathf.Max(0.01f, itemClearance);
        for (int i = 0; i < attempts; i++)
        {
            float radians = i * Mathf.PI * 2f / attempts;
            Vector3 candidate = transform.position + new Vector3(Mathf.Cos(radians), Mathf.Sin(radians)) * Mathf.Max(0.01f, dropDistance);
            if (candidate.x - clearance < roomBounds.xMin || candidate.x + clearance > roomBounds.xMax ||
                candidate.y - clearance < roomBounds.yMin || candidate.y + clearance > roomBounds.yMax ||
                !ClearPath(transform.position, candidate))
                continue;
            bool blocked = false;
            foreach (Collider2D collider in Physics2D.OverlapCircleAll(candidate, clearance, blockingLayers))
                if (IsObstacle(collider)) { blocked = true; break; }
            if (blocked)
                continue;
            position = candidate;
            return true;
        }
        return false;
    }
    private bool ClearPath(Vector2 origin, Vector2 point)
    {
        Vector2 delta = point - origin;
        foreach (RaycastHit2D hit in Physics2D.RaycastAll(origin, delta.normalized, delta.magnitude, blockingLayers))
            if (IsObstacle(hit.collider))
                return false;
        return true;
    }
    private bool IsObstacle(Collider2D collider) => !collider.isTrigger &&
        !collider.transform.IsChildOf(transform) && collider.GetComponentInParent<EnemyChase2D>() == null &&
        collider.GetComponentInParent<Projectile2D>() == null;
}
