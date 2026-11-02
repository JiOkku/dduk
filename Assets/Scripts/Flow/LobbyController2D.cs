using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class LobbyController2D : MonoBehaviour
{
    [SerializeField] private PlayerMovement2D playerMovement;
    [SerializeField] private Transform entrance; // Existing scene reference retained.
    [SerializeField] private WeaponPartData2D startingWeapon;
    [SerializeField] private WeaponCatalog2D weaponCatalog;
    [SerializeField] private string basicMagicName = "Basic projectile (RMB)";
    [SerializeField] private DungeonStageCatalog2D stageCatalog;
    private LobbyStation2D[] stations;
    private int selectedStage;
    private bool menuOpen, movementWasEnabled;
    private LobbyStationKind2D activeMenu;
    private GameObject confirmation;
    private Text prompt, stageLabel, entryQuestion;
    private Button previousStageButton, nextStageButton, enterButton, cancelButton;
    public bool HasOpenDialog => menuOpen;
    public bool IsMenuOpen(LobbyStationKind2D kind) => menuOpen && activeMenu == kind;
    public void SetMagicName(string name) => basicMagicName = name;
    private DungeonStageData2D SelectedStage => stageCatalog.Stages[selectedStage];

    private void Start()
    {
        if (playerMovement == null || entrance == null || stageCatalog == null || stageCatalog.Count == 0 || startingWeapon == null)
        { Debug.LogError("Connect lobby player, starting weapon and stage catalog.", this); enabled = false; return; }
        foreach (var stage in stageCatalog.Stages)
            if (stage == null) { Debug.LogError("Missing lobby stage.", this); enabled = false; return; }
        stations = FindObjectsByType<LobbyStation2D>();
        selectedStage = stageCatalog.FindIndex(SceneFlow2D.StartStageId);
        BuildUI();
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("Lobby EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false); events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }
    private LobbyStation2D NearestStation()
    {
        LobbyStation2D best = null; float distance = float.PositiveInfinity;
        if (stations == null) return null;
        foreach (var station in stations)
        {
            if (station == null || !station.CanInteract(playerMovement.transform)) continue;
            float current = Vector2.Distance(playerMovement.transform.position, station.transform.position);
            if (current < distance) { distance = current; best = station; }
        }
        return best;
    }
    private void Update()
    {
        if (prompt == null) return;
        bool inLobby = SceneFlow2D.InLobby;
        var station = inLobby && !menuOpen ? NearestStation() : null;
        prompt.text = SceneFlow2D.IsLoading ? "Loading..." : station != null ? station.Prompt : "";
        prompt.gameObject.SetActive(!string.IsNullOrEmpty(prompt.text));
        if (IsMenuOpen(LobbyStationKind2D.Dungeon))
        {
            previousStageButton.interactable = nextStageButton.interactable = inLobby && stageCatalog.Count > 1;
            enterButton.interactable = cancelButton.interactable = inLobby;
        }
        if (!Application.isFocused || Keyboard.current == null || !inLobby) return;
        if (menuOpen)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame) CloseCurrentMenu();
            return;
        }
        if (station == null || !Keyboard.current.eKey.wasPressedThisFrame) return;
        InteractNearestStation();
    }
    public void InteractNearestStation()
    {
        if (!SceneFlow2D.InLobby || menuOpen) return;
        var station = NearestStation();
        if (station == null) return;
        switch (station.Kind)
        {
            case LobbyStationKind2D.Equipment: GetComponent<LobbyEquipmentUI2D>()?.OpenMenu(); break;
            case LobbyStationKind2D.Storage: GetComponent<LobbyRewardsUI2D>()?.OpenMenu(); break;
            case LobbyStationKind2D.Growth: GetComponent<LobbyGrowthUI2D>()?.OpenFromStation(); break;
            case LobbyStationKind2D.Dungeon: OpenConfirmation(); break;
        }
    }
    public bool TryBeginMenu(LobbyStationKind2D kind)
    {
        if (menuOpen || !SceneFlow2D.InLobby) return false;
        var station = NearestStation();
        if (station == null || station.Kind != kind) return false;
        menuOpen = true; activeMenu = kind;
        movementWasEnabled = playerMovement.enabled; playerMovement.enabled = false;
        return true;
    }
    public void EndMenu(LobbyStationKind2D kind)
    {
        if (!IsMenuOpen(kind)) return;
        menuOpen = false;
        if (!SceneFlow2D.IsLoading && playerMovement != null) playerMovement.enabled = movementWasEnabled;
    }
    public void CloseCurrentMenu()
    {
        switch (activeMenu)
        {
            case LobbyStationKind2D.Equipment: GetComponent<LobbyEquipmentUI2D>()?.CloseMenu(); break;
            case LobbyStationKind2D.Storage: GetComponent<LobbyRewardsUI2D>()?.CloseMenu(); break;
            case LobbyStationKind2D.Growth: GetComponent<LobbyGrowthUI2D>()?.CloseFromStation(); break;
            case LobbyStationKind2D.Dungeon: Cancel(); break;
        }
    }
    private void OpenConfirmation()
    {
        if (!TryBeginMenu(LobbyStationKind2D.Dungeon)) return;
        RefreshStage(); confirmation.SetActive(true); enterButton.Select();
    }
    private void Cancel()
    {
        if (SceneFlow2D.IsLoading) return;
        confirmation.SetActive(false); EndMenu(LobbyStationKind2D.Dungeon);
    }
    private void Enter()
    {
        if (!IsMenuOpen(LobbyStationKind2D.Dungeon) || SceneFlow2D.IsLoading) return;
        bool accepted = SceneFlow2D.EnterDungeon(SelectedStage.Id);
        enterButton.interactable = cancelButton.interactable = !accepted;
    }
    private void ChangeStage(int delta)
    {
        if (!IsMenuOpen(LobbyStationKind2D.Dungeon) || SceneFlow2D.IsLoading) return;
        selectedStage = (selectedStage + delta + stageCatalog.Count) % stageCatalog.Count; RefreshStage();
    }
    private void RefreshStage()
    {
        stageLabel.text = $"{SelectedStage.Name} / {stageCatalog.Count} | {SelectedStage.Element}";
        var weapon = weaponCatalog != null && weaponCatalog.Validate() ? weaponCatalog.StartingWeapon(RewardBank2D.Snapshot()) : null;
        entryQuestion.text = $"Weapon: {(weapon != null ? weapon.WeaponName : startingWeapon.WeaponName)}\nMagic: {basicMagicName}\nFull HP / Mana. Export rewards on final dungeon clear.";
    }
    private void BuildUI()
    {
        var canvas = LobbyMenuView2D.Canvas(transform, "Lobby Interaction Canvas"); canvas.GetComponent<Canvas>().sortingOrder = 0;
        prompt = LobbyMenuView2D.Text(canvas.transform, "", 0, 0, 700, 45, 27);
        var rect = prompt.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 20f); prompt.alignment = TextAnchor.MiddleCenter; prompt.gameObject.SetActive(false);
        confirmation = LobbyMenuView2D.Modal(canvas.transform, "Dungeon Entrance Menu");
        var panel = LobbyMenuView2D.Panel(confirmation.transform);
        LobbyMenuView2D.Text(panel, "Dungeon Entrance", 40, 35, 900, 50, 36);
        stageLabel = LobbyMenuView2D.Text(panel, "", 170, 145, 760, 60, 30); stageLabel.alignment = TextAnchor.MiddleCenter;
        previousStageButton = LobbyMenuView2D.Button(panel, "< Previous", 100, 225, 400, 55, () => ChangeStage(-1));
        nextStageButton = LobbyMenuView2D.Button(panel, "Next >", 600, 225, 400, 55, () => ChangeStage(1));
        entryQuestion = LobbyMenuView2D.Text(panel, "", 100, 325, 900, 140, 26);
        enterButton = LobbyMenuView2D.Button(panel, "Enter Dungeon", 100, 525, 400, 60, Enter);
        cancelButton = LobbyMenuView2D.Button(panel, "Close", 600, 525, 400, 60, Cancel);
        confirmation.SetActive(false);
    }
}
