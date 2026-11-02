using System.Text;
using UnityEngine;
using UnityEngine.UI;

// Existing reward storage becomes an interaction-only inventory/currency view.
[DisallowMultipleComponent]
public sealed class LobbyRewardsUI2D : MonoBehaviour
{
    [SerializeField] private MagicCatalog2D magicCatalog;
    private LobbyController2D lobby;
    private GameObject modal;
    private RectTransform content;
    private Text header, items;
    private bool dirty;
    private void OnEnable() => RewardBank2D.Changed += MarkDirty;
    private void OnDisable()
    {
        RewardBank2D.Changed -= MarkDirty;
        if (lobby != null && modal != null && modal.activeSelf) lobby.EndMenu(LobbyStationKind2D.Storage);
    }
    private void MarkDirty() => dirty = true;
    private void Start()
    {
        lobby = GetComponent<LobbyController2D>();
        var canvas = LobbyMenuView2D.Canvas(transform, "Storage Station Canvas");
        modal = LobbyMenuView2D.Modal(canvas.transform, "Storage Menu");
        var panel = LobbyMenuView2D.Panel(modal.transform);
        LobbyMenuView2D.Text(panel, "Storage", 40, 20, 850, 50, 34);
        LobbyMenuView2D.Button(panel, "Close", 950, 20, 110, 45, CloseMenu);
        header = LobbyMenuView2D.Text(panel, "", 40, 90, 1020, 110, 24);
        content = LobbyMenuView2D.Scroll(panel, 40, 225, 1020, 350, out _);
        items = LobbyMenuView2D.Text(content, "", 15, 10, 980, 350, 23);
        items.verticalOverflow = VerticalWrapMode.Overflow;
        LobbyMenuView2D.Text(panel, "Use the equipment station to equip stored weapons or scrolls.", 40, 600, 1020, 40, 22);
        modal.SetActive(false);
    }
    public void OpenMenu()
    {
        if (!isActiveAndEnabled || modal == null || lobby == null || !lobby.TryBeginMenu(LobbyStationKind2D.Storage)) return;
        modal.SetActive(true); Refresh();
    }
    public void CloseMenu()
    {
        if (modal == null) return;
        modal.SetActive(false); lobby.EndMenu(LobbyStationKind2D.Storage);
    }
    private void Update() { if (dirty && modal != null && modal.activeSelf) { dirty = false; Refresh(); } }
    private void Refresh()
    {
        var profile = RewardBank2D.Snapshot();
        header.text = $"Magic Stones: {profile.magicStones} | Last clear: +{profile.lastMagicStones}\n" +
            $"Export Tokens: {profile.currency}\nStored weapons: {profile.weapons.Count} | Owned scrolls: {profile.ownedMagicIds.Count}";
        var text = new StringBuilder(); int lines = 0;
        text.AppendLine("WEAPONS"); lines++;
        if (profile.weapons.Count == 0) { text.AppendLine("No exported weapons yet."); lines++; }
        foreach (var weapon in profile.weapons)
        {
            text.AppendLine($"{weapon.name}{(profile.equippedWeaponInstanceId == weapon.instanceId ? " [Equipped]" : "")} [{weapon.source}]\n" +
                $"Damage {weapon.damage} | Interval {weapon.attackInterval:F3}s | Range {weapon.range:F2} | Mana/hit {weapon.manaOnHit}\n");
            lines += 3;
        }
        text.AppendLine("\nMAGIC SCROLLS"); lines += 2;
        foreach (string id in profile.ownedMagicIds)
        {
            var magic = magicCatalog != null ? magicCatalog.Find(id) : null;
            if (magic == null) continue;
            text.AppendLine($"{magic.Name} | {magic.Element} T{magic.Tier}{(profile.equippedMagicId == id ? " [Equipped]" : "")}"); lines++;
        }
        if (!string.IsNullOrEmpty(RewardBank2D.Error)) { text.AppendLine("\nSave warning: " + RewardBank2D.Error); lines += 3; }
        items.text = text.ToString();
        content.sizeDelta = new Vector2(0, Mathf.Max(350f, lines * 34f));
        items.rectTransform.sizeDelta = new Vector2(980f, content.sizeDelta.y);
    }
}
