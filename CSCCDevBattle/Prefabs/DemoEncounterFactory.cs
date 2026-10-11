/// <summary>
/// Department encounter progression: Mobile -> Web -> AI.
/// Each department's four authored attacks are shuffled without replacement.
/// One dialogue introduces each department; no inter-wave dialogue is queued.
/// </summary>
public static class DemoEncounterFactory
{
    public static EncounterDefinition CreateDepartmentEncounter()
    {
        EncounterDefinition encounter = new()
        {
            Name = "The Department Gauntlet"
        };

        EncounterPhase mobile = CreatePhase("Mobile");
        mobile.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "Mobile first. Swipe, scroll, and find a way through the noise.",
            3.0f,
            secondsPerCharacter: 0.028f,
            shakeAmount: 0.35f));
        mobile.BattleWavePool.Add(() => new MobileScrollerWave());
        mobile.BattleWavePool.Add(() => new LocationLaserWave());
        mobile.BattleWavePool.Add(() => new CityMazeChaseWave());
        mobile.BattleWavePool.Add(() => new DeliveryTrafficWave());
        encounter.Phases.Add(mobile);

        EncounterPhase web = CreatePhase("Web");
        web.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "Survived mobile? Welcome to the Web. The DOM is not your friend.",
            3.0f,
            secondsPerCharacter: 0.028f,
            shakeAmount: 0.55f));
        web.BattleWavePool.Add(() => new DomSplitWave());
        web.BattleWavePool.Add(() => new HttpClientTrafficWave());
        web.BattleWavePool.Add(() => new CssJsGunWave());
        web.BattleWavePool.Add(() => new LoadingCircleWave());
        encounter.Phases.Add(web);

        EncounterPhase ai = CreatePhase("AI");
        ai.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "Final department: AI. My predictions are confident. Yours should be careful.",
            3.1f,
            secondsPerCharacter: 0.028f,
            shakeAmount: 0.7f));
        ai.BattleWavePool.Add(() => new NeuralNetworkWave());
        ai.BattleWavePool.Add(() => new NumericWarningWave());
        ai.BattleWavePool.Add(() => new VisionPursuitWave());
        ai.BattleWavePool.Add(() => new ReactiveGridWave());
        encounter.Phases.Add(ai);

        return encounter;
    }

    private static EncounterPhase CreatePhase(string name) => new()
    {
        Name = name,
        BattleWaveCount = 4,
        ShuffleBattleWavesWithoutReplacement = true,
        PlayBetweenWaveDialogAfterFinalWave = false
    };
}
