using System;

// Immutable summary, with no Unity scene-object references and no persistent rewards.
public sealed class DungeonRunResult2D
{
    public int Kills { get; }
    public int ReachedWave { get; }
    public int EarnedGold { get; }
    public bool Cleared { get; }
    public int ReachedStage { get; }
    public DungeonRunResult2D(int kills, int reachedWave, int earnedGold, bool cleared, int reachedStage = 1)
    {
        Kills = Math.Max(0, kills);
        ReachedWave = Math.Max(0, reachedWave);
        EarnedGold = Math.Max(0, earnedGold);
        Cleared = cleared;
        ReachedStage = Math.Max(1, reachedStage);
    }
}
