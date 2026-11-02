using System;

public enum DungeonStageState2D { Preparing, Fighting, StageCleared, Transitioning, DungeonCleared, GameOver, BossFighting }

// Pure run-local state. Completing a stage can succeed only once; death always wins.
public sealed class DungeonStageProgress2D
{
    public int Index { get; private set; }
    public int Count { get; }
    public DungeonStageState2D State { get; private set; } = DungeonStageState2D.Preparing;
    public DungeonStageProgress2D(int count, int startIndex = 0)
    {
        Count = Math.Max(1, count);
        Index = Math.Max(0, Math.Min(startIndex, Count - 1));
    }
    public bool BeginFighting()
    {
        if (State != DungeonStageState2D.Preparing && State != DungeonStageState2D.Transitioning) return false;
        State = DungeonStageState2D.Fighting;
        return true;
    }
    public bool BeginBoss()
    {
        if (State != DungeonStageState2D.Fighting) return false;
        State = DungeonStageState2D.BossFighting;
        return true;
    }
    public bool Complete(bool playerDead, bool bossDefeated = false)
    {
        if (playerDead) { Die(); return false; }
        if (State != DungeonStageState2D.Fighting && (State != DungeonStageState2D.BossFighting || !bossDefeated)) return false;
        State = Index + 1 == Count ? DungeonStageState2D.DungeonCleared : DungeonStageState2D.StageCleared;
        return true;
    }
    public bool Next()
    {
        if (State != DungeonStageState2D.StageCleared || Index + 1 >= Count) return false;
        State = DungeonStageState2D.Transitioning;
        Index++;
        return true;
    }
    public void Die() { State = DungeonStageState2D.GameOver; }
}
