using UnityEngine;

[CreateAssetMenu(fileName = "DungeonStages", menuName = "Prototype/Dungeon Stage Catalog")]
public sealed class DungeonStageCatalog2D : ScriptableObject
{
    [SerializeField] private DungeonStageData2D[] stages;
    public DungeonStageData2D[] Stages => stages;
    public int Count => stages != null ? stages.Length : 0;
    public int FindIndex(string id)
    {
        for (int i = 0; i < Count; i++) if (stages[i] != null && stages[i].Id == id) return i;
        return 0;
    }
}
