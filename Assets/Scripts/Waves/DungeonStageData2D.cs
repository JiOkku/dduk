using System;
using UnityEngine;

[Serializable]
public sealed class StageEnemyGroup2D
{
    public EnemyChase2D prefab;
    public EnemyElementData2D enemyData;
    [Min(1)] public int count = 1;
}

[Serializable]
public sealed class StageWave2D
{
    public StageEnemyGroup2D[] enemies;
    public int Count
    {
        get
        {
            int total = 0;
            if (enemies != null) foreach (var group in enemies) total += group != null ? Mathf.Max(0, group.count) : 0;
            return total;
        }
    }
    public StageEnemyGroup2D GroupAt(int spawnIndex)
    {
        if (spawnIndex < 0 || enemies == null) return null;
        foreach (var group in enemies)
        {
            if (group == null) continue;
            if (spawnIndex < group.count) return group;
            spawnIndex -= Mathf.Max(0, group.count);
        }
        return null;
    }
}

[CreateAssetMenu(fileName = "Stage", menuName = "Prototype/Dungeon Stage")]
public sealed class DungeonStageData2D : ScriptableObject
{
    [SerializeField] private string stageId;
    [SerializeField] private string stageName;
    [SerializeField] private MagicElement2D element;
    [SerializeField] private StageWave2D[] waves;
    [SerializeField, Min(0.01f)] private float healthMultiplier = 1f;
    [SerializeField, Min(0.01f)] private float damageMultiplier = 1f;
    [SerializeField, Min(0.01f)] private float speedMultiplier = 1f;
    [SerializeField, Min(0)] private int clearGold = 10;
    [SerializeField] private Color environmentColor = Color.white;
    [SerializeField] private BossData2D boss;
    [SerializeField, Min(0.01f)] private float bossHealthMultiplier = 1f;
    [SerializeField, Min(0.01f)] private float bossDamageMultiplier = 1f;
    [SerializeField, Min(0f)] private float bossIntroDuration = 1.5f;
    public string Id => stageId;
    public string Name => stageName;
    public MagicElement2D Element => element;
    public StageWave2D[] Waves => waves;
    public float HealthMultiplier => Mathf.Max(0.01f, healthMultiplier);
    public float DamageMultiplier => Mathf.Max(0.01f, damageMultiplier);
    public float SpeedMultiplier => Mathf.Max(0.01f, speedMultiplier);
    public int ClearGold => Mathf.Max(0, clearGold);
    public Color EnvironmentColor => environmentColor;
    public BossData2D Boss => boss;
    public float BossHealthMultiplier => Mathf.Max(0.01f, bossHealthMultiplier);
    public float BossDamageMultiplier => Mathf.Max(0.01f, bossDamageMultiplier);
    public float BossIntroDuration => Mathf.Max(0f, bossIntroDuration);
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(stageId) || waves == null || waves.Length == 0) return false;
        if (boss != null && !boss.Validate()) return false;
        foreach (var wave in waves)
        {
            if (wave == null || wave.Count <= 0 || wave.enemies == null) return false;
            foreach (var group in wave.enemies)
                if (group == null || group.count <= 0 || group.enemyData == null || group.prefab == null ||
                    !group.prefab.gameObject.activeSelf || group.prefab.GetComponent<PolygonCollider2D>() == null) return false;
        }
        return true;
    }
}
