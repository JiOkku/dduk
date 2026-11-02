#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Read-only audit of Unity's imported references, including components inside prefabs.
public static class PrototypeCombatAudit
{
    [MenuItem("Tools/Prototype/Check Lobby Station Menus (Play Mode)")]
    public static void CheckLobbyStations()
    {
        if (!Application.isPlaying || !SceneFlow2D.InLobby) { Debug.LogWarning("Run this check in the lobby Play mode."); return; }
        var lobby = Object.FindAnyObjectByType<LobbyController2D>();
        var movement = Object.FindAnyObjectByType<PlayerMovement2D>();
        if (lobby == null || movement == null || lobby.HasOpenDialog) { Debug.LogWarning("Close all lobby menus first."); return; }
        var body = movement.GetComponent<Rigidbody2D>();
        Vector3 original = movement.transform.position;
        Vector2 velocity = body != null ? body.linearVelocity : Vector2.zero;
        bool wasEnabled = movement.enabled;
        var text = new StringBuilder();
        try
        {
            foreach (var station in Object.FindObjectsByType<LobbyStation2D>())
            {
                Vector3 position = station.transform.position + Vector3.down * 1.3f;
                movement.transform.position = position;
                if (body != null) body.position = position;
                Physics2D.SyncTransforms();
                lobby.InteractNearestStation();
                bool opened = lobby.IsMenuOpen(station.Kind);
                bool blockedSecondMenu = !lobby.TryBeginMenu(station.Kind);
                bool paused = !movement.enabled;
                lobby.CloseCurrentMenu();
                bool restored = !lobby.HasOpenDialog && movement.enabled == wasEnabled;
                text.AppendLine(station.Kind + ": open=" + opened + " exclusive=" + blockedSecondMenu + " paused=" + paused + " restored=" + restored);
            }
        }
        finally
        {
            if (lobby.HasOpenDialog) lobby.CloseCurrentMenu();
            movement.transform.position = original;
            movement.enabled = wasEnabled;
            if (body != null) { body.position = original; body.linearVelocity = velocity; }
            Physics2D.SyncTransforms();
        }
        Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/lobby-station-check.txt", text.ToString());
        Debug.Log(text.ToString());
    }
    [MenuItem("Tools/Prototype/Audit Combat Configuration")]
    public static void Run()
    {
        var text = new StringBuilder();
        var growth = AssetDatabase.LoadAssetAtPath<PermanentGrowthData2D>("Assets/Settings/PermanentGrowth.asset");
        if (growth != null)
        {
            text.AppendLine("Growth valid=" + growth.Validate(out string error) + " " + error);
            foreach (var upgrade in growth.Upgrades)
                text.AppendLine("  Upgrade " + upgrade.kind + " levels=" + upgrade.MaxLevel + " bonus/level=" + upgrade.bonusPerLevel);
        }
        var catalog = AssetDatabase.LoadAssetAtPath<DungeonStageCatalog2D>("Assets/Settings/Stages/DungeonStages.asset");
        text.AppendLine("Catalog loaded: " + (catalog != null));
        foreach (var manager in Object.FindObjectsByType<WaveManager2D>())
        {
            var serialized = new SerializedObject(manager);
            var spawner = serialized.FindProperty("spawner").objectReferenceValue as EnemySpawner2D;
            text.AppendLine("Scene " + manager.gameObject.scene.path + ": spawner=" + (spawner != null) +
                " configured=" + (spawner != null && spawner.IsConfigured) +
                " health=" + (serialized.FindProperty("playerHealth").objectReferenceValue != null) +
                " gold=" + (serialized.FindProperty("playerGold").objectReferenceValue != null) +
                " rewards=" + (Object.FindAnyObjectByType<DungeonRewards2D>() != null));
        }
        if (catalog != null) foreach (var stage in catalog.Stages)
        {
            if (stage == null) { text.AppendLine("Missing stage"); continue; }
            text.AppendLine(stage.name + " valid=" + stage.Validate());
            if (stage.Waves != null) foreach (var wave in stage.Waves)
            {
                if (wave == null || wave.enemies == null) { text.AppendLine("  Missing wave/groups"); continue; }
                foreach (var group in wave.enemies)
                {
                    if (group == null) { text.AppendLine("  Missing enemy group"); continue; }
                    text.AppendLine("  Group prefab=" + (group.prefab != null) + " active=" +
                        (group.prefab != null && group.prefab.gameObject.activeSelf) + " polygon=" +
                        (group.prefab != null && group.prefab.GetComponent<PolygonCollider2D>() != null) +
                        " data=" + (group.enemyData != null) + " count=" + group.count);
                }
            }
            var boss = stage.Boss;
            if (boss == null) { text.AppendLine("  No boss"); continue; }
            text.AppendLine("  Boss " + boss.Id + " valid=" + boss.Validate() + " prefab=" + (boss.Prefab != null) +
                " active=" + (boss.Prefab != null && boss.Prefab.gameObject.activeSelf) +
                " controller=" + (boss.Prefab != null && boss.Prefab.GetComponent<BossController2D>() != null) +
                " polygon=" + (boss.Prefab != null && boss.Prefab.GetComponent<PolygonCollider2D>() != null) +
                " projectile=" + (boss.ProjectilePrefab != null));
            if (boss.Patterns != null) foreach (var pattern in boss.Patterns)
                text.AppendLine("  Pattern " + (pattern != null ? pattern.kind.ToString() : "NULL"));
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/combat-config-audit.txt", text.ToString());
    }
}
#endif
