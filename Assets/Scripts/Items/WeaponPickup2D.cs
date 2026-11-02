using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class WeaponPickup2D : MonoBehaviour
{
    private static readonly HashSet<WeaponPickup2D> active = new HashSet<WeaponPickup2D>();
    [SerializeField] private WeaponPartData2D weapon;
    [SerializeField] private WeaponInstance2D instance;
    private bool reserved;
    public WeaponInstance2D Weapon => instance;
    public bool Available => isActiveAndEnabled && !reserved && instance != null && instance.IsValid;
    public static IEnumerable<WeaponPickup2D> Active => active;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => active.Clear();
    private void Awake()
    {
        if ((instance == null || !instance.IsValid) && weapon != null)
            instance = WeaponInstance2D.CreateBase(weapon);
    }
    private void OnEnable()
    {
        active.Add(this);
        if (instance != null && instance.IsValid)
            GetComponent<SpriteRenderer>().color = instance.PickupColor;
    }
    private void OnDisable() => active.Remove(this);

    public static WeaponPickup2D Spawn(WeaponPickup2D prefab, WeaponInstance2D data, Vector3 position)
    {
        if (prefab == null || data == null || !data.IsValid || !prefab.gameObject.activeSelf)
            return null;
        WeaponPickup2D item = Instantiate(prefab, position, Quaternion.identity);
        item.instance = data;
        item.weapon = data.BaseData;
        item.GetComponent<SpriteRenderer>().color = data.PickupColor;
        return item;
    }

    public bool TryReserve()
    {
        if (!Available)
            return false;
        reserved = true;
        return true;
    }
    public void Release() => reserved = false;
    public void Consume()
    {
        reserved = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
