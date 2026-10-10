/// <summary>
/// Script for the department boss encounter. Phase order is intentional:
/// Mobile -> Web -> AI. Each department offers four attack patterns.
/// </summary>
public static class DemoEncounterFactory
{
    public static EncounterDefinition CreateDepartmentEncounter()
    {
        EncounterDefinition encounter = new()
        {
            Name = "The Department Gauntlet"
        };

        // ------------------------------------------------------------
        // PHASE 1 — MOBILE
        // ------------------------------------------------------------
        EncounterPhase mobile = new()
        {
            Name = "Mobile",
            BattleWaveCount = 4,
            PlayBetweenWaveDialogAfterFinalWave = true
        };

        mobile.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "Mobile first.\nEverything wants your attention at once.",
            2.8f));
        mobile.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "Tap, swipe, survive.\nAnd please accept all permissions.",
            2.8f,
            shakeAmount: 0.5f));

        mobile.BattleWavePool.Add(() => new NotificationStormWave());
        mobile.BattleWavePool.Add(() => new MultiTouchRippleWave());
        mobile.BattleWavePool.Add(() => new LowBatterySurgeWave());
        mobile.BattleWavePool.Add(() => new PermissionDeniedWave());

        mobile.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS", "Notification delivered.\nRead receipt not required.", 2.2f));
        mobile.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS", "Your battery is fine.\nYour odds are not.", 2.2f));
        mobile.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS", "Swipe left. No, your other left.", 2.2f));
        mobile.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS", "Access denied. Try dodging instead.", 2.2f));

        encounter.Phases.Add(mobile);

        // ------------------------------------------------------------
        // PHASE 2 — WEB
        // ------------------------------------------------------------
        EncounterPhase web = new()
        {
            Name = "Web",
            BattleWaveCount = 4,
            PlayBetweenWaveDialogAfterFinalWave = true
        };

        web.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "Mobile survived? Fine.         \nWelcome to the front end.",
            2.8f));
        web.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "The layout is responsive.       \nThe projectiles are not.",
            2.8f,
            shakeAmount: 0.9f));

        web.BattleWavePool.Add(() => new CssGunWave());
        web.BattleWavePool.Add(() => new DomTreeCollapseWave());
        web.BattleWavePool.Add(() => new Http404RedirectWave());
        web.BattleWavePool.Add(() => new PopupHellWave());

        web.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS", "That was not a bug.        \nIt was a feature.", 2.2f));
        web.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS", "I would fix the layout,       \nbut it works on my machine.", 2.4f));
        web.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS", "404: safe space not found.", 2.0f, shakeAmount: 1.2f));
        web.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS", "Close one popup and two more will appear.", 2.2f));

        encounter.Phases.Add(web);

        // ------------------------------------------------------------
        // PHASE 3 — AI
        // ------------------------------------------------------------
        EncounterPhase ai = new()
        {
            Name = "AI",
            BattleWaveCount = 4,
            PlayBetweenWaveDialogAfterFinalWave = true
        };

        ai.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "Last department.      \nI have learned every move you make.",
            2.8f));
        ai.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "Probably. My confidence score is 99.8 percent.",
            2.8f,
            shakeAmount: 0.5f));

        ai.BattleWavePool.Add(() => new NeuralNetworkWave());
        ai.BattleWavePool.Add(() => new HallucinationWave());
        ai.BattleWavePool.Add(() => new PredictiveModelWave());
        ai.BattleWavePool.Add(() => new OverfittingWave());

        ai.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS", "The model is certain. The model is also wrong.", 2.3f));
        ai.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS", "I predicted that dodge.\nI simply chose not to believe it.", 2.5f));
        ai.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS", "Your movement is outside\nmy training distribution.", 2.4f));
        ai.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS", "Fine. I will stop learning and start guessing.", 2.3f));
        
        encounter.Phases.Add(ai);

        // Ending 

        return encounter;
    }
}
