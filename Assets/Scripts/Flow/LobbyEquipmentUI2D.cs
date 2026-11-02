using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyEquipmentUI2D : MonoBehaviour
{
    [SerializeField] private WeaponCatalog2D weaponCatalog;
    private LobbyController2D lobby;
    private LobbyMagicUI2D magicUI;
    private GameObject modal;
    private RectTransform list;
    private Text details, status;
    private Button equipButton;
    private bool magicTab, dirty;
    private string weaponId, magicId;
    private void OnEnable() => RewardBank2D.Changed += MarkDirty;
    private void OnDisable()
    {
        RewardBank2D.Changed -= MarkDirty;
        if (lobby != null && modal != null && modal.activeSelf) lobby.EndMenu(LobbyStationKind2D.Equipment);
    }
    private void MarkDirty() => dirty = true;
    private void Start()
    {
        lobby = GetComponent<LobbyController2D>(); magicUI = GetComponent<LobbyMagicUI2D>();
        if (lobby == null || magicUI == null || weaponCatalog == null || !weaponCatalog.Validate())
        { Debug.LogError("Connect lobby equipment Weapon Catalog and magic service.", this); enabled = false; return; }
        var canvas = LobbyMenuView2D.Canvas(transform, "Equipment Station Canvas");
        modal = LobbyMenuView2D.Modal(canvas.transform, "Equipment Menu");
        var panel = LobbyMenuView2D.Panel(modal.transform);
        LobbyMenuView2D.Text(panel, "Equipment", 40, 20, 900, 50, 34);
        LobbyMenuView2D.Button(panel, "Close", 950, 20, 110, 45, CloseMenu);
        LobbyMenuView2D.Button(panel, "Weapons", 40, 85, 490, 45, () => { magicTab = false; Refresh(); });
        LobbyMenuView2D.Button(panel, "Magic Scrolls", 560, 85, 500, 45, () => { magicTab = true; Refresh(); });
        list = LobbyMenuView2D.Scroll(panel, 40, 155, 405, 420, out _);
        details = LobbyMenuView2D.Text(panel, "", 475, 155, 580, 320, 24);
        equipButton = LobbyMenuView2D.Button(panel, "Equip", 475, 500, 580, 60, EquipSelected);
        status = LobbyMenuView2D.Text(panel, "", 40, 600, 1020, 45, 21);
        modal.SetActive(false);
    }
    public void OpenMenu()
    {
        if (!isActiveAndEnabled || modal == null || !lobby.TryBeginMenu(LobbyStationKind2D.Equipment)) return;
        var profile = magicUI.Profile;
        weaponId = profile.equippedWeaponInstanceId ?? ""; magicId = profile.equippedMagicId;
        status.text = "Choose an item, then Equip. Selection is saved for future dungeon entries.";
        modal.SetActive(true); Refresh();
    }
    public void CloseMenu()
    {
        if (modal == null) return;
        modal.SetActive(false); lobby.EndMenu(LobbyStationKind2D.Equipment);
    }
    private void Update() { if (dirty && modal != null && modal.activeSelf) { dirty = false; Refresh(); } }
    private WeaponInstance2D Preview(RewardProfile2D profile)
    {
        if (string.IsNullOrEmpty(weaponId)) return WeaponInstance2D.CreateBase(weaponCatalog.DefaultWeapon);
        var saved = profile.weapons.Find(item => item.instanceId == weaponId);
        return saved != null ? WeaponInstance2D.Restore(saved, weaponCatalog.Find(saved.definitionId)) : null;
    }
    private void Refresh()
    {
        var profile = magicUI.Profile;
        for (int i = list.childCount - 1; i >= 0; i--)
        { var old = list.GetChild(i).gameObject; old.SetActive(false); Destroy(old); }
        int row = 0;
        if (!magicTab)
        {
            string defaultName = weaponCatalog.DefaultWeapon.WeaponName;
            AddRow(defaultName + " (default)" + (string.IsNullOrEmpty(profile.equippedWeaponInstanceId) ? " [Equipped]" : ""), row++, () => { weaponId = ""; Refresh(); });
            foreach (var item in profile.weapons)
            {
                string id = item.instanceId;
                AddRow($"{item.name} | Damage {item.damage} | Range {item.range:F2}" + (profile.equippedWeaponInstanceId == id ? " [Equipped]" : ""),
                    row++, () => { weaponId = id; Refresh(); });
            }
            var weapon = Preview(profile);
            details.text = weapon != null ? $"{weapon.WeaponName}\nAttack: {weapon.AttackShape}\nDamage: {weapon.Damage}\nInterval: {weapon.AttackInterval:F3}s\nRange: {weapon.Range:F2}\nMana / hit: {weapon.ManaOnHit}\n\nStorage retains the original item.\nA copy of these stats starts each dungeon." : "Weapon definition unavailable.\nChoose the default weapon to continue.";
            equipButton.interactable = SceneFlow2D.InLobby && weapon != null && (profile.equippedWeaponInstanceId ?? "") != (weaponId ?? "");
        }
        else
        {
            foreach (var spell in magicUI.Catalog.All)
            {
                if (!profile.ownedMagicIds.Contains(spell.Id)) continue;
                string id = spell.Id;
                AddRow($"{spell.Name} | {spell.Element} T{spell.Tier}" + (profile.equippedMagicId == id ? " [Equipped]" : ""),
                    row++, () => { magicId = id; Refresh(); });
            }
            var magic = magicUI.Catalog.Find(magicId);
            details.text = magic != null ? $"{magic.Name} | {magic.Element} Tier {magic.Tier}\nMana: {magic.ManaCost}\nCooldown: {magic.Cooldown:F1}s\nDamage: {magic.Damage}\nAttack: {magic.Form}\n\n{magic.Description}" : "Choose an owned scroll.";
            equipButton.interactable = SceneFlow2D.InLobby && magic != null && profile.ownedMagicIds.Contains(magic.Id) && profile.equippedMagicId != magic.Id;
        }
        list.sizeDelta = new Vector2(0, row * 76f);
    }
    private void AddRow(string label, int row, UnityEngine.Events.UnityAction action)
    { LobbyMenuView2D.Button(list, label, 8, row * 76f + 5f, 385, 65, action); }
    private void EquipSelected()
    {
        if (!lobby.IsMenuOpen(LobbyStationKind2D.Equipment) || !SceneFlow2D.InLobby) return;
        bool saved = magicTab ? magicUI.Equip(magicId) : Preview(magicUI.Profile) != null && RewardBank2D.TrySelectWeapon(weaponId);
        status.text = saved ? "Equipped and saved." : "Equip failed: " + RewardBank2D.Error;
        Refresh();
    }
}
