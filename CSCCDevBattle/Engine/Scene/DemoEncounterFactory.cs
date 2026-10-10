/// <summary>
/// Sample encounter content. Add/replace phases and wave factories here;
/// keep BattleScene free of encounter script details.
/// </summary>
public static class DemoEncounterFactory
{
    public static EncounterDefinition CreateDepartmentEncounter()
    {
        EncounterDefinition encounter = new()
        {
            Name = "Department Encounter"
        };

        EncounterPhase webPhase = new()
        {
            Name = "Web",
            BattleWaveCount = 2,
            PlayBetweenWaveDialogAfterFinalWave = true
        };

        webPhase.OpeningDialogs.Add(
            () => new DialogWave(
                "SYSTEM",
                "A web request has been detected.",
                2.5f));

        webPhase.BattleWavePool.Add(() => new TestWave());
        webPhase.BattleWavePool.Add(() => new TestWave());

        webPhase.BetweenWaveDialogPool.Add(
            () => new DialogWave(
                "SYSTEM",
                "The request has been processed.",
                1.8f));

        encounter.Phases.Add(webPhase);

        EncounterPhase aiPhase = new()
        {
            Name = "AI",
            BattleWaveCount = 2,
            PlayBetweenWaveDialogAfterFinalWave = true
        };

        aiPhase.OpeningDialogs.Add(
            () => new DialogWave(
                "SYSTEM",
                "A new process is starting.",
                2f,
                shakeAmount: 1.5f));

        aiPhase.BattleWavePool.Add(
            () => new TestWave { Duration = 3f });
        aiPhase.BattleWavePool.Add(
            () => new TestWave { Duration = 5f });

        aiPhase.BetweenWaveDialogPool.Add(
            () => new DialogWave(
                "SYSTEM",
                "Processing continues...",
                1.5f));

        encounter.Phases.Add(aiPhase);
        return encounter;
    }
}
