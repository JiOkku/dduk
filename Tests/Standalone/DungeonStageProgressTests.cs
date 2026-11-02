using System;

public static class DungeonStageProgressTests
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    public static int Main()
    {
        try
        {
            CompletionAndRepeatedButtons();
            DeathWins();
            NewRunStartsAtOne();
            SelectedEntryStartsAtChosenStage();
            BossBlocksCompletionAndDeathWins();
            Console.WriteLine("PASS 5 stage progression scenarios (production state logic; no Unity scene/physics execution).");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static void CompletionAndRepeatedButtons()
    {
        var run = new DungeonStageProgress2D(6);
        Check(!run.Next() && !run.Complete(false), "Cannot skip preparation.");
        Check(run.BeginFighting(), "Initial stage starts.");
        int awards = 0;
        for (int index = 0; index < 6; index++)
        {
            Check(run.Index == index && !run.BeginFighting(), "Do not start a fighting stage twice.");
            if (run.Complete(false)) awards++;
            Check(!run.Complete(false), "Repeated completion must not award twice.");
            if (index < 5)
            {
                Check(run.State == DungeonStageState2D.StageCleared, "Intermediate clear must not finish the dungeon.");
                Check(run.Next() && !run.Next(), "Repeated button must not skip a stage.");
                Check(!run.Complete(false) && run.BeginFighting(), "Cannot complete while transitioning.");
            }
        }
        Check(awards == 6 && run.State == DungeonStageState2D.DungeonCleared && !run.Next(), "Exactly six awards and final clear.");
    }
    private static void DeathWins()
    {
        var run = new DungeonStageProgress2D(1); run.BeginFighting();
        Check(!run.Complete(true) && run.State == DungeonStageState2D.GameOver, "Simultaneous death beats last kill.");
        Check(!run.Next() && !run.BeginFighting() && !run.Complete(false), "Cannot revive or grant after death.");
        var transition = new DungeonStageProgress2D(2); transition.BeginFighting(); transition.Complete(false); transition.Next(); transition.Die();
        Check(!transition.BeginFighting() && !transition.Complete(false), "Death during transition prevents combat restart.");
    }
    private static void NewRunStartsAtOne()
    {
        var previous = new DungeonStageProgress2D(6); previous.BeginFighting(); previous.Complete(false); previous.Next();
        var fresh = new DungeonStageProgress2D(6);
        Check(fresh.Index == 0 && fresh.State == DungeonStageState2D.Preparing && !fresh.Next(), "No previous run state leaks.");
    }
    private static void SelectedEntryStartsAtChosenStage()
    {
        var run = new DungeonStageProgress2D(6, 3);
        Check(run.Index == 3 && run.BeginFighting(), "Selected Stage 4 must be the first fighting stage.");
        int completions = 0;
        for (int index = 3; index < 6; index++)
        {
            Check(run.Index == index && run.Complete(false), "Advance only through the remaining stages.");
            completions++;
            if (index < 5) Check(run.Next() && run.BeginFighting(), "Selected entry preserves normal progression.");
        }
        Check(completions == 3 && run.State == DungeonStageState2D.DungeonCleared, "Skipped stages must not be completed or rewarded.");
        var restarted = new DungeonStageProgress2D(6, 3);
        Check(restarted.Index == 3 && restarted.State == DungeonStageState2D.Preparing, "Restart uses selected entry without retaining run progress.");
        Check(new DungeonStageProgress2D(6, -10).Index == 0 && new DungeonStageProgress2D(6, 99).Index == 5,
            "Out of range constructor values must be bounded.");
    }
    private static void BossBlocksCompletionAndDeathWins()
    {
        var run = new DungeonStageProgress2D(6, 2); run.BeginFighting();
        Check(run.BeginBoss() && !run.BeginBoss(), "Only one boss phase may start.");
        Check(!run.Complete(false) && !run.Next(), "Living boss blocks clear and next stage.");
        Check(run.Complete(false, true) && !run.Complete(false, true), "Boss death completes exactly once.");
        Check(run.Next() && run.Index == 3, "Boss clear preserves selected-stage progression.");
        var final = new DungeonStageProgress2D(6, 5); final.BeginFighting(); final.BeginBoss();
        Check(!final.Complete(true, true) && final.State == DungeonStageState2D.GameOver,
            "Player death beats simultaneous final boss death.");
        Check(!final.Complete(false, true), "Death must not permit later settlement.");
    }
}
