using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[DefaultExecutionOrder(300)]
[DisallowMultipleComponent]
public sealed class GameHUD2D : MonoBehaviour
{
    [SerializeField] private PlayerHealth2D playerHealth;
    [SerializeField] private WaveManager2D waves;
    [SerializeField] private DungeonSession2D session;

    private Text healthText;
    private Text manaText;
    private Text magicText;
    private Text waveText;
    private Text stageText;
    private Button nextStageButton;
    private GameObject bossPanel;
    private Text bossNameText;
    private Image bossHealthFill;
    private Text spawnText;
    private Text goldText;
    private Text weaponText;
    private PlayerWeaponLoadout2D playerLoadout;
    private PlayerMelee2D playerMelee;
    private PlayerWeaponPickup2D playerPickup;
    private Text pickupText;
    private GameObject comparisonPanel;
    private Text comparisonText;
    private Text announcementText;
    private Text resultTitle;
    private Text resultStats;
    private Text rewardStats;
    private Text rewardNotice;
    private Button retrySaveButton;
    private Button lobbyButton;
    private Text lobbyButtonLabel;
    private PlayerGold2D playerGold;
    private PlayerMana2D playerMana;
    private PlayerShooting2D playerMagic;
    private GameObject gameOverPanel;
    private Button restartButton;
    private Font font;
    private bool restarting;

    private void Start()
    {
        if (playerHealth == null)
        {
            Debug.LogError("Assign Player's PlayerHealth2D to GameHUD2D.Player Health.", this);
            enabled = false;
            return;
        }

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        playerMana = playerHealth.GetComponent<PlayerMana2D>();
        playerMagic = playerHealth.GetComponent<PlayerShooting2D>();
        playerGold = playerHealth.GetComponent<PlayerGold2D>();
        playerLoadout = playerHealth.GetComponent<PlayerWeaponLoadout2D>();
        playerMelee = playerHealth.GetComponent<PlayerMelee2D>();
        playerPickup = playerHealth.GetComponent<PlayerWeaponPickup2D>();
        if (waves == null)
            waves = FindAnyObjectByType<WaveManager2D>();
        if (session == null)
            session = GetComponent<DungeonSession2D>();
        BuildInterface();
        EnsureEventSystem();
        Refresh();
    }

    private void LateUpdate()
    {
        if (playerHealth != null)
            Refresh();
    }

    private void Refresh()
    {
        string label = $"HP: {playerHealth.CurrentHealth} / {playerHealth.MaxHealth}";
        if (healthText.text != label)
            healthText.text = label;

        manaText.text = playerMana != null ? $"Mana: {playerMana.CurrentMana} / {playerMana.MaxMana}" : "Mana: not configured";
        if (playerHealth.CombatStopped)
            magicText.text = "Magic: Disabled";
        else if (playerMagic == null || !playerMagic.isActiveAndEnabled || playerMana == null || !playerMana.CanUseMana)
            magicText.text = "Magic: Unavailable";
        else if (playerMagic.CooldownRemaining > 0f)
            magicText.text = $"Magic: {playerMagic.CooldownRemaining:F1}s";
        else
            magicText.text = playerMagic.CanCast ? "Magic: Ready" : "Magic: Not enough mana";
        MagicData2D currentMagic = playerMagic != null ? playerMagic.CurrentMagic : null;
        if (currentMagic != null)
            magicText.text = $"{currentMagic.Name} [{currentMagic.Element} T{currentMagic.Tier}] Cost {currentMagic.ManaCost}\n" + magicText.text;

        bool stageClear = waves != null && waves.State == GameState2D.StageClear;
        bool intermediate = waves != null && (waves.State == GameState2D.StageCompleted || waves.State == GameState2D.StageTransition);
        stageText.text = waves != null ? $"Stage {waves.CurrentStageNumber} / {waves.TotalStages}\nElement: {(waves.CurrentStage != null ? waves.CurrentStage.Element.ToString() : "None")}" : "";
        var boss = waves != null ? waves.CurrentBoss : null;
        bool showBoss = !playerHealth.CombatStopped && waves != null && waves.State == GameState2D.BossFight &&
            boss != null && boss.Health != null && !boss.Health.IsDefeated;
        bossPanel.SetActive(showBoss);
        if (showBoss)
        {
            bossNameText.text = $"{boss.Data.Name}  {boss.Health.CurrentHealth} / {boss.Health.MaxHealth}";
            bossHealthFill.rectTransform.anchorMax = new Vector2((float)boss.Health.CurrentHealth / boss.Health.MaxHealth, 1f);
            bossHealthFill.color = boss.Data.Color;
        }
        if (waves != null)
        {
            waveText.text = $"Wave: {waves.CurrentWave} / {waves.TotalWaves}    Remaining: {waves.RemainingEnemies}";
            spawnText.text = waves.SpawnBlocked ? "Spawn blocked - retrying..." :
                $"Alive: {waves.AliveEnemies}    Pending: {waves.PendingEnemies}";
            announcementText.text = waves.Announcement;
        }
        else
        {
            waveText.text = "";
            spawnText.text = "";
            announcementText.text = "";
        }
        goldText.text = $"Gold: {(playerGold != null ? playerGold.CurrentGold : 0)}";
        weaponText.text = playerLoadout != null && playerLoadout.CurrentWeapon != null ?
            $"Equipped: {playerLoadout.CurrentWeapon.WeaponName}" : "Equipped: not configured";
        pickupText.text = playerPickup != null && playerPickup.isActiveAndEnabled ? playerPickup.Prompt : "";
        if (playerMelee != null && playerMelee.PermanentDamageBonus > 0)
            weaponText.text += $" | Melee +{playerMelee.PermanentDamageBonus}";
        if (!playerHealth.CombatStopped && playerMelee != null && playerLoadout != null &&
            playerLoadout.CurrentWeapon != null && playerLoadout.CurrentWeapon.ChargeEnabled)
        {
            WeaponInstance2D weapon = playerLoadout.CurrentWeapon;
            weaponText.text += weapon.IsFullyCharged(playerMelee.ChargeElapsed) ? " - Full Charge" :
                $" - {playerMelee.ChargeElapsed:F1}/{weapon.FullChargeTime:F1}s";
        }
        RefreshComparison();
        bool transitioning = waves != null && waves.State == GameState2D.StageTransition;
        resultTitle.text = stageClear ? "Dungeon Clear" : transitioning ? "Preparing Next Stage" : intermediate ? "Stage Clear" : "Game Over";
        bool showGameOver = playerHealth.IsDead || stageClear || intermediate;
        DungeonRunResult2D result = session != null ? session.Result : null;
        resultStats.text = result != null ?
            $"Kills: {result.Kills}    Stage: {result.ReachedStage}    Wave: {result.ReachedWave}\nGold earned: {result.EarnedGold} (run only)\nCleared: {(result.Cleared ? "Yes" : "No")}" : transitioning ? "Preparing the next room..." : intermediate ?
            $"Gold +{(waves.CurrentStage != null ? waves.CurrentStage.ClearGold : 0)} | Magic Stones +{waves.StageStoneReward} (pending)\nHP / mana / weapon / gold carry over.\nLeave now: pending rewards are lost." : "";
        DungeonRewards2D rewards = session != null ? session.Rewards : null;
        rewardStats.text = rewards != null && rewards.Finished ? rewards.Summary() : "";
        rewardNotice.text = rewards != null && !showGameOver ? rewards.Notice : "";
        bool pendingSave = rewards != null && rewards.NeedsSaveRetry;
        retrySaveButton.gameObject.SetActive(showGameOver && pendingSave);
        lobbyButton.interactable = !restarting && !pendingSave;
        lobbyButton.gameObject.SetActive(!transitioning);
        lobbyButtonLabel.text = intermediate ? "Leave (Lose Rewards)" : "Return to Lobby";
        restartButton.gameObject.SetActive(!intermediate);
        nextStageButton.gameObject.SetActive(intermediate);
        nextStageButton.interactable = intermediate && waves.CanAdvance;
        if (gameOverPanel.activeSelf != showGameOver)
        {
            gameOverPanel.SetActive(showGameOver);
            if (showGameOver)
            {
                if (intermediate) nextStageButton.Select(); else restartButton.Select();
            }
        }
    }

    private void RefreshComparison()
    {
        WeaponPickup2D item = playerPickup != null && playerPickup.isActiveAndEnabled ? playerPickup.Nearest : null;
        WeaponInstance2D equipped = playerLoadout != null ? playerLoadout.CurrentWeapon : null;
        bool show = !playerHealth.CombatStopped && item != null && item.Available && equipped != null;
        comparisonPanel.SetActive(show);
        if (!show)
            return;
        WeaponInstance2D ground = item.Weapon;
        comparisonText.text = $"Equipped: {equipped.WeaponName}\nGround: {ground.WeaponName}\n\n" +
            ComparisonRow("Damage", equipped.Damage, ground.Damage, false, "0") +
            ComparisonRow("Interval (s)", equipped.AttackInterval, ground.AttackInterval, true, "0.000") +
            ComparisonRow("Range", equipped.Range, ground.Range, false, "0.00") +
            ComparisonRow("Mana / hit", equipped.ManaOnHit, ground.ManaOnHit, false, "0") +
            "\nDifferent attack shapes; stats aren't an overall rating.\nE: Pick up";
    }

    private static string ComparisonRow(string name, float current, float candidate, bool lowerIsBetter, string format)
    {
        float delta = candidate - current;
        bool equal = Mathf.Abs(delta) < 0.0001f;
        bool better = lowerIsBetter ? delta < 0f : delta > 0f;
        string color = equal ? "#DDDDDD" : better ? "#70EE90" : "#FF8888";
        string verdict = equal ? "=" : better ? "better" : "worse";
        return $"{name}: {current.ToString(format)} -> <color={color}>{candidate.ToString(format)} ({verdict})</color>\n";
    }

    private void BuildInterface()
    {
        GameObject canvasObject = new GameObject("HUD Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        stageText = CreateText("Stage", canvasObject.transform, "", 24);
        stageText.alignment = TextAnchor.UpperRight;
        SetRect(stageText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-24f, -24f), new Vector2(350f, 65f));

        healthText = CreateText("Health", canvasObject.transform, "", 32);
        healthText.alignment = TextAnchor.UpperLeft;
        SetRect(healthText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -24f), new Vector2(300f, 60f));

        manaText = CreateText("Mana", canvasObject.transform, "", 28);
        manaText.alignment = TextAnchor.UpperLeft;
        SetRect(manaText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -72f), new Vector2(400f, 45f));
        magicText = CreateText("Magic Status", canvasObject.transform, "", 22);
        magicText.alignment = TextAnchor.UpperLeft;
        SetRect(magicText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -116f), new Vector2(680f, 70f));

        waveText = CreateText("Wave", canvasObject.transform, "", 24);
        waveText.alignment = TextAnchor.UpperLeft;
        SetRect(waveText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -200f), new Vector2(620f, 40f));
        spawnText = CreateText("Spawn Count", canvasObject.transform, "", 24);
        spawnText.alignment = TextAnchor.UpperLeft;
        SetRect(spawnText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -240f), new Vector2(620f, 40f));
        goldText = CreateText("Gold", canvasObject.transform, "", 24);
        goldText.alignment = TextAnchor.UpperLeft;
        SetRect(goldText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -280f), new Vector2(300f, 40f));

        weaponText = CreateText("Weapon", canvasObject.transform, "", 24);
        weaponText.alignment = TextAnchor.UpperLeft;
        SetRect(weaponText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -320f), new Vector2(650f, 40f));
        pickupText = CreateText("Pickup Prompt", canvasObject.transform, "", 24);
        pickupText.alignment = TextAnchor.UpperLeft;
        SetRect(pickupText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, -360f), new Vector2(650f, 40f));

        comparisonPanel = new GameObject("Weapon Comparison", typeof(RectTransform), typeof(Image));
        comparisonPanel.transform.SetParent(canvasObject.transform, false);
        SetRect(comparisonPanel.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-24f, -90f), new Vector2(460f, 330f));
        Image comparisonBackground = comparisonPanel.GetComponent<Image>();
        comparisonBackground.color = new Color(0f, 0f, 0f, 0.85f);
        comparisonBackground.raycastTarget = false;
        comparisonText = CreateText("Stats", comparisonPanel.transform, "", 22);
        comparisonText.alignment = TextAnchor.UpperLeft;
        comparisonText.supportRichText = true;
        comparisonText.rectTransform.anchorMin = Vector2.zero;
        comparisonText.rectTransform.anchorMax = Vector2.one;
        comparisonText.rectTransform.offsetMin = new Vector2(16f, 12f);
        comparisonText.rectTransform.offsetMax = new Vector2(-16f, -12f);
        comparisonPanel.SetActive(false);

        gameOverPanel = new GameObject("Game Over", typeof(RectTransform), typeof(Image));
        gameOverPanel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        gameOverPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

        Text title = CreateText("Title", gameOverPanel.transform, "Game Over", 48);
        resultTitle = title;
        SetRect(title.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(0f, 260f), new Vector2(500f, 70f));
        resultStats = CreateText("Run Result", gameOverPanel.transform, "", 25);
        SetRect(resultStats.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(0f, 160f), new Vector2(680f, 100f));
        rewardStats = CreateText("Rewards", gameOverPanel.transform, "", 22);
        SetRect(rewardStats.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(0f, -15f), new Vector2(1050f, 220f));

        GameObject buttonObject = new GameObject("Restart", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(gameOverPanel.transform, false);
        SetRect(buttonObject.GetComponent<RectTransform>(), Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(0f, -285f), new Vector2(300f, 60f));
        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.15f, 0.4f, 0.65f);
        restartButton = buttonObject.GetComponent<Button>();
        restartButton.targetGraphic = buttonImage;
        restartButton.onClick.AddListener(Restart);

        Text buttonLabel = CreateText("Label", buttonObject.transform, "Restart Dungeon", 25);
        buttonLabel.rectTransform.anchorMin = Vector2.zero;
        buttonLabel.rectTransform.anchorMax = Vector2.one;
        buttonLabel.rectTransform.offsetMin = Vector2.zero;
        buttonLabel.rectTransform.offsetMax = Vector2.zero;
        GameObject lobbyObject = new GameObject("Return to Lobby", typeof(RectTransform), typeof(Image), typeof(Button));
        lobbyObject.transform.SetParent(gameOverPanel.transform, false);
        SetRect(lobbyObject.GetComponent<RectTransform>(), Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(0f, -215f), new Vector2(300f, 60f));
        Image lobbyImage = lobbyObject.GetComponent<Image>();
        lobbyImage.color = new Color(0.15f, 0.4f, 0.65f);
        lobbyButton = lobbyObject.GetComponent<Button>();
        lobbyButton.targetGraphic = lobbyImage;
        lobbyButton.onClick.AddListener(ReturnToLobby);
        Text lobbyLabel = CreateText("Label", lobbyObject.transform, "Return to Lobby", 25);
        lobbyButtonLabel = lobbyLabel;
        lobbyLabel.rectTransform.anchorMin = Vector2.zero;
        lobbyLabel.rectTransform.anchorMax = Vector2.one;
        lobbyLabel.rectTransform.offsetMin = Vector2.zero;
        lobbyLabel.rectTransform.offsetMax = Vector2.zero;
        GameObject retryObject = new GameObject("Retry Reward Save", typeof(RectTransform), typeof(Image), typeof(Button));
        retryObject.transform.SetParent(gameOverPanel.transform, false);
        SetRect(retryObject.GetComponent<RectTransform>(), Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(0f, -150f), new Vector2(300f, 45f));
        Image retryImage = retryObject.GetComponent<Image>();
        retryImage.color = new Color(0.4f, 0.25f, 0.1f);
        retrySaveButton = retryObject.GetComponent<Button>();
        retrySaveButton.targetGraphic = retryImage;
        retrySaveButton.onClick.AddListener(() => { if (session != null && session.Rewards != null) session.Rewards.RetrySave(); });
        Text retryLabel = CreateText("Label", retryObject.transform, "Retry Save", 24);
        retryLabel.rectTransform.anchorMin = Vector2.zero;
        retryLabel.rectTransform.anchorMax = Vector2.one;
        retryLabel.rectTransform.offsetMin = Vector2.zero;
        retryLabel.rectTransform.offsetMax = Vector2.zero;
        retryObject.SetActive(false);
        GameObject nextObject = new GameObject("Next Stage", typeof(RectTransform), typeof(Image), typeof(Button));
        nextObject.transform.SetParent(gameOverPanel.transform, false);
        SetRect(nextObject.GetComponent<RectTransform>(), Vector2.one * 0.5f, Vector2.one * 0.5f,
            new Vector2(0f, -145f), new Vector2(300f, 60f));
        Image nextImage = nextObject.GetComponent<Image>();
        nextImage.color = new Color(0.15f, 0.4f, 0.65f);
        nextStageButton = nextObject.GetComponent<Button>();
        nextStageButton.targetGraphic = nextImage;
        nextStageButton.onClick.AddListener(() => { if (waves != null) waves.TryNextStage(); });
        Text nextLabel = CreateText("Label", nextObject.transform, "Next Stage", 25);
        nextLabel.rectTransform.anchorMin = Vector2.zero;
        nextLabel.rectTransform.anchorMax = Vector2.one;
        nextLabel.rectTransform.offsetMin = Vector2.zero;
        nextLabel.rectTransform.offsetMax = Vector2.zero;
        nextObject.SetActive(false);
        gameOverPanel.SetActive(false);

        announcementText = CreateText("Wave Announcement", canvasObject.transform, "", 30);
        SetRect(announcementText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -24f), new Vector2(500f, 55f));
        rewardNotice = CreateText("Reward Acquired", canvasObject.transform, "", 23);
        SetRect(rewardNotice.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 35f), new Vector2(1150f, 70f));
        bossPanel = new GameObject("Boss Health", typeof(RectTransform));
        bossPanel.transform.SetParent(canvasObject.transform, false);
        SetRect(bossPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 100f), new Vector2(420f, 70f));
        bossNameText = CreateText("Boss Name", bossPanel.transform, "", 24);
        SetRect(bossNameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            Vector2.zero, new Vector2(520f, 35f));
        var bar = new GameObject("Health Bar", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(bossPanel.transform, false);
        SetRect(bar.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(420f, 18f));
        bar.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);
        bar.GetComponent<Image>().raycastTarget = false;
        var fill = new GameObject("Current HP", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(bar.transform, false);
        bossHealthFill = fill.GetComponent<Image>();
        bossHealthFill.raycastTarget = false;
        bossHealthFill.rectTransform.anchorMin = Vector2.zero;
        bossHealthFill.rectTransform.anchorMax = Vector2.one;
        bossHealthFill.rectTransform.offsetMin = bossHealthFill.rectTransform.offsetMax = Vector2.zero;
        bossPanel.SetActive(false);
    }

    private Text CreateText(string objectName, Transform parent, string text, int size)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text label = textObject.GetComponent<Text>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }

    private static void SetRect(RectTransform rect, Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject events = new GameObject("HUD EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.transform.SetParent(transform, false);
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    private void Restart()
    {
        bool stageClear = waves != null && waves.State == GameState2D.StageClear;
        if (restarting || SceneFlow2D.IsLoading || (!playerHealth.IsDead && !stageClear))
            return;
        SetLeaving(SceneFlow2D.RestartDungeon());
    }

    private void ReturnToLobby()
    {
        bool stageClear = waves != null && waves.State == GameState2D.StageClear;
        bool intermediate = waves != null && waves.State == GameState2D.StageCompleted;
        if (restarting || SceneFlow2D.IsLoading || (!playerHealth.IsDead && !stageClear && !intermediate) ||
            (session != null && session.Rewards != null && session.Rewards.NeedsSaveRetry))
            return;
        SetLeaving(SceneFlow2D.ReturnToLobby());
    }

    private void SetLeaving(bool accepted)
    {
        restarting = accepted;
        restartButton.interactable = !accepted;
        lobbyButton.interactable = !accepted;
    }
}
