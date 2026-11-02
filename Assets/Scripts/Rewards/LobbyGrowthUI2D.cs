using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(20)]
[DisallowMultipleComponent]
public sealed class LobbyGrowthUI2D : MonoBehaviour
{
    [SerializeField] private PermanentGrowthData2D growthData;
    private LobbyController2D lobby;
    private Font font;
    private GameObject modal;
    private GameObject body;
    private Text balance;
    private Text message;
    private bool dirty;
    private bool upgradesTab;
    private MagicElement2D element;
    private string selectedNodeId;
    private float nextPurchaseTime;
    private void OnEnable() => RewardBank2D.Changed += MarkDirty;
    private void OnDisable()
    {
        RewardBank2D.Changed -= MarkDirty;
        if (lobby != null && modal != null && modal.activeSelf) lobby.EndMenu(LobbyStationKind2D.Growth);
    }
    private void MarkDirty() => dirty = true;
    private void Start()
    {
        lobby = GetComponent<LobbyController2D>();
        if (lobby == null || growthData == null || !growthData.Validate(out _))
        { Debug.LogError("Connect valid Permanent Growth data and LobbyController2D.", this); enabled = false; return; }
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUI();
    }
    private void Update()
    {
        if (modal == null) return;
        if (dirty && modal.activeSelf) { dirty = false; Refresh(); }
    }
    public void OpenFromStation()
    {
        if (lobby == null || modal == null || !lobby.TryBeginMenu(LobbyStationKind2D.Growth)) return;
        modal.SetActive(true);
        message.text = "Purchases are saved immediately. No refunds in this prototype.";
        Refresh();
    }
    private void Close()
    {
        modal.SetActive(false); lobby.EndMenu(LobbyStationKind2D.Growth);
    }
    public void CloseFromStation() => Close();
    private void Tab(MagicElement2D value)
    { element = value; upgradesTab = false; selectedNodeId = null; Refresh(); }
    private void BuyNode(string id)
    {
        if (!SceneFlow2D.InLobby || Time.unscaledTime < nextPurchaseTime) return;
        nextPurchaseTime = Time.unscaledTime + 0.35f;
        bool saved = RewardBank2D.TryPurchaseNode(growthData, id);
        message.text = saved ? "Scroll unlocked and saved. You can equip it now." : RewardBank2D.Error;
        Refresh();
    }
    private void BuyUpgrade(PermanentUpgradeKind2D kind, int expectedLevel)
    {
        if (!SceneFlow2D.InLobby || Time.unscaledTime < nextPurchaseTime) return;
        nextPurchaseTime = Time.unscaledTime + 0.35f;
        bool saved = RewardBank2D.TryPurchaseUpgrade(growthData, kind, expectedLevel);
        message.text = saved ? "Upgrade saved. Applies on the next dungeon entry." : RewardBank2D.Error;
        Refresh();
    }
    private void Refresh()
    {
        if (!growthData.Validate(out string error)) { message.text = error; return; }
        var profile = RewardBank2D.GrowthSnapshot(growthData);
        balance.text = $"Permanent Growth | Magic Stones: {profile.magicStones}";
        for (int i = body.transform.childCount - 1; i >= 0; i--)
        { var child = body.transform.GetChild(i).gameObject; child.SetActive(false); Destroy(child); }
        if (upgradesTab) { BuildUpgrades(profile); return; }
        int row = 0;
        MagicTechNode2D selected = null;
        foreach (var node in growthData.Nodes)
        {
            var magic = growthData.MagicCatalog.Find(node.magicId);
            if (magic.Element != element) continue;
            if (selectedNodeId == null) selectedNodeId = node.nodeId;
            var state = PermanentGrowthRules2D.NodeState(profile, node);
            string id = node.nodeId;
            var button = MakeButton(body.transform, $"{magic.Name} (T{magic.Tier})\n{state} | {node.cost} stones",
                0f, row * 125f, 310f, 90f, () => { selectedNodeId = id; Refresh(); });
            button.GetComponent<Image>().color = state == MagicNodeState2D.Unlocked ? new Color(0.15f, 0.45f, 0.25f) :
                state == MagicNodeState2D.Available ? new Color(0.15f, 0.35f, 0.6f) : new Color(0.25f, 0.25f, 0.25f);
            if (node.nodeId == selectedNodeId) selected = node;
            if (row > 0 && node.prerequisites != null && node.prerequisites.Length > 0)
            {
                var required = growthData.FindNode(node.prerequisites[0]);
                string name = growthData.MagicCatalog.Find(required.magicId).Name;
                MakeText(body.transform, "Requires: " + name + (node.prerequisites.Length > 1 ? " + more" : ""),
                    0f, row * 125f - 30f, 310f, 25f, 17);
            }
            row++;
        }
        if (selected == null) return;
        var spell = growthData.MagicCatalog.Find(selected.magicId);
        var nodeState = PermanentGrowthRules2D.NodeState(profile, selected);
        string prerequisites = "None";
        if (selected.prerequisites != null && selected.prerequisites.Length > 0)
        {
            var names = new System.Collections.Generic.List<string>();
            foreach (string id in selected.prerequisites)
            {
                var prerequisite = growthData.FindNode(id);
                names.Add(growthData.MagicCatalog.Find(prerequisite.magicId).Name);
            }
            prerequisites = string.Join(", ", names.ToArray());
        }
        MakeText(body.transform,
            $"{spell.Name} | {spell.Element} | Tier {spell.Tier}\n" +
            $"Mana {spell.ManaCost} | Cooldown {spell.Cooldown:F1}s | Damage {spell.Damage}\nAttack: {spell.Form}\n\n" +
            $"{spell.Description}\n{selected.description}\n\nCost: {selected.cost} Magic Stones\nRequires: {prerequisites}\nStatus: {nodeState}",
            350f, 0f, 640f, 330f, 21);
        string selectedId = selected.nodeId;
        string unlockLabel = nodeState == MagicNodeState2D.Unlocked ? "Unlocked" :
            nodeState == MagicNodeState2D.Locked ? "Prerequisite Locked" :
            profile.magicStones < selected.cost ? $"Need {selected.cost} stones" : "Unlock Scroll";
        var unlock = MakeButton(body.transform, unlockLabel,
            350f, 350f, 290f, 52f, () => BuyNode(selectedId));
        unlock.interactable = SceneFlow2D.InLobby && nodeState == MagicNodeState2D.Available && profile.magicStones >= selected.cost;
        if (nodeState == MagicNodeState2D.Unlocked)
        {
            MakeText(body.transform, "Equip this scroll at the equipment station.", 665f, 350f, 330f, 70f, 20);
        }
    }
    private void BuildUpgrades(RewardProfile2D profile)
    {
        int row = 0;
        foreach (var upgrade in growthData.Upgrades)
        {
            int level = PermanentGrowthRules2D.Level(profile, upgrade.kind);
            int appliedLevel = Mathf.Min(level, upgrade.MaxLevel);
            bool max = level >= upgrade.MaxLevel;
            string next = max ? "MAX" : $"Next bonus: +{(appliedLevel + 1) * upgrade.bonusPerLevel} | Cost: {upgrade.costs[level]} stones";
            MakeText(body.transform, $"{upgrade.displayName} | Level {appliedLevel} / {upgrade.MaxLevel}\n" +
                $"Current bonus: +{appliedLevel * upgrade.bonusPerLevel}\n{next}", 0f, row * 140f, 720f, 120f, 24);
            var kind = upgrade.kind;
            int displayedLevel = level;
            string buttonLabel = max ? "MAX" : profile.magicStones < upgrade.costs[level] ? $"Need {upgrade.costs[level]} stones" : "Upgrade";
            var buy = MakeButton(body.transform, buttonLabel, 770f, row * 140f + 25f, 230f, 55f,
                () => BuyUpgrade(kind, displayedLevel));
            buy.interactable = SceneFlow2D.InLobby && !max && profile.magicStones >= upgrade.costs[level];
            row++;
        }
    }
    private void BuildUI()
    {
        var canvas = new GameObject("Permanent Growth Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().sortingOrder = 5;
        var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        modal = new GameObject("Growth Menu", typeof(RectTransform), typeof(Image));
        modal.transform.SetParent(canvas.transform, false);
        var overlay = modal.GetComponent<RectTransform>(); overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        modal.GetComponent<Image>().color = new Color(0.035f, 0.045f, 0.065f, 1f);
        var panel = new GameObject("Panel", typeof(RectTransform)); panel.transform.SetParent(modal.transform, false);
        var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
        rect.sizeDelta = new Vector2(1100f, 650f);
        balance = MakeText(panel.transform, "", 20f, 10f, 880f, 45f, 29);
        MakeButton(panel.transform, "Close", 960f, 10f, 120f, 40f, Close);
        MakeButton(panel.transform, "Fire", 20f, 65f, 235f, 45f, () => Tab(MagicElement2D.Fire));
        MakeButton(panel.transform, "Water", 285f, 65f, 235f, 45f, () => Tab(MagicElement2D.Water));
        MakeButton(panel.transform, "Grass", 550f, 65f, 235f, 45f, () => Tab(MagicElement2D.Grass));
        MakeButton(panel.transform, "Stat Upgrades", 815f, 65f, 265f, 45f, () => { upgradesTab = true; Refresh(); });
        body = new GameObject("Content", typeof(RectTransform)); body.transform.SetParent(panel.transform, false);
        SetRect(body.GetComponent<RectTransform>(), 40f, 140f, 1020f, 440f);
        message = MakeText(panel.transform, "", 35f, 595f, 1030f, 50f, 19);
        modal.SetActive(false);
    }
    private static void SetRect(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
    }
    private Text MakeText(Transform parent, string value, float x, float y, float width, float height, int size)
    {
        var obj = new GameObject("Text", typeof(RectTransform), typeof(Text)); obj.transform.SetParent(parent, false);
        var text = obj.GetComponent<Text>(); text.font = font; text.fontSize = size; text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft; text.text = value; text.raycastTarget = false;
        SetRect(text.rectTransform, x, y, width, height); return text;
    }
    private Button MakeButton(Transform parent, string label, float x, float y, float width, float height, UnityEngine.Events.UnityAction action)
    {
        var obj = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button)); obj.transform.SetParent(parent, false);
        SetRect(obj.GetComponent<RectTransform>(), x, y, width, height);
        var image = obj.GetComponent<Image>(); image.color = new Color(0.15f, 0.35f, 0.6f);
        var button = obj.GetComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
        var text = MakeText(obj.transform, label, 0f, 0f, width, height, 21); text.alignment = TextAnchor.MiddleCenter;
        return button;
    }
}
