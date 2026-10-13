
/// <summary>
/// Department encounter progression: Mobile -> Web -> AI -> Outro.
/// Each department's four authored attacks are shuffled without replacement.
/// Dialogues play between waves within their own department.
/// The outro contains dialogue only.
/// </summary>
public static class DemoEncounterFactory
{
    public static EncounterDefinition CreateDepartmentEncounter()
    {
        EncounterDefinition encounter = new()
        {
            Name = "The Department Gauntlet"
        };


        // =========================================================
        // INTRO — THE CSCC DEVELOPER CHALLENGE
        // =========================================================

        EncounterPhase intro = new()
        {
            Name = "Intro",
            BattleWaveCount = 0,
            ShuffleBattleWavesWithoutReplacement = false,
            PlayBetweenWaveDialogAfterFinalWave = false
        };

        intro.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "Welcome, developer.",
            2.2f,
            secondsPerCharacter: 0.035f,
            shakeAmount: 0.1f));

        intro.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "This is the CSCC Developer Challenge.\nThree departments. Twelve battles. One final evaluation.",
            3.5f,
            secondsPerCharacter: 0.035f,
            shakeAmount: 0.2f));

        intro.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "Mobile. Web. Artificial Intelligence.\nLet's see how you handle the world of computer science.",
            3.5f,
            secondsPerCharacter: 0.035f,
            shakeAmount: 0.25f));

        intro.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "Don't expect me to go easy on you.\nYour evaluation begins... NOW.",
            3.0f,
            secondsPerCharacter: 0.1f,
            shakeAmount: 0.7f));

        encounter.Phases.Add(intro);

        // =========================================================
        // MOBILE
        // =========================================================

        EncounterPhase mobile = CreatePhase("Mobile");

        mobile.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "Mobile first.\nSwipe, scroll, and find a way through the noise.",
            3.0f,
            secondsPerCharacter: 0.028f,
            shakeAmount: 0.35f));

        mobile.BattleWavePool.Add(() => new MobileScrollerWave());
        mobile.BattleWavePool.Add(() => new LocationLaserWave());
        mobile.BattleWavePool.Add(() => new CityMazeChaseWave());
        mobile.BattleWavePool.Add(() => new DeliveryTrafficWave());

        mobile.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS",
            "Still standing? Impressive.\nMost people get lost before the first screen loads.",
            3.0f,
            secondsPerCharacter: 0.028f,
            shakeAmount: 0.35f));

        mobile.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS",
            "GPS, traffic, endless scrolling...\nFunny how a little screen can cause so much trouble.",
            3.2f,
            secondsPerCharacter: 0.028f,
            shakeAmount: 0.4f));

        mobile.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS",
            "That's enough swiping.\nLet's see how you handle something a little more connected.",
            3.0f,
            secondsPerCharacter: 0.028f,
            shakeAmount: 0.45f));

        encounter.Phases.Add(mobile);


        // =========================================================
        // WEB
        // =========================================================

        EncounterPhase web = CreatePhase("Web");

        web.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "Survived Mobile? Welcome to the Web.\nThe DOM is not your friend.",
            3.0f,
            secondsPerCharacter: 0.028f,
            shakeAmount: 0.55f));

        web.BattleWavePool.Add(() => new DomSplitWave());
        web.BattleWavePool.Add(() => new HttpClientTrafficWave());
        web.BattleWavePool.Add(() => new CssJsGunWave());
        web.BattleWavePool.Add(() => new LoadingCircleWave());

        web.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS",
            "Elements everywhere.\nAnd you thought arranging a webpage was difficult?",
            3.0f,
            secondsPerCharacter: 0.028f,
            shakeAmount: 0.5f));

        web.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS",
            "Requests sent. Responses received.\nSomewhere in that mess, something always breaks.",
            3.1f,
            secondsPerCharacter: 0.028f,
            shakeAmount: 0.6f));

        web.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS",
            "CSS can change appearances.\nJavaScript can change everything.\nLet's see what you learned.",
            3.2f,
            secondsPerCharacter: 0.028f,
            shakeAmount: 0.65f));

        encounter.Phases.Add(web);


        // =========================================================
        // AI
        // =========================================================

        EncounterPhase ai = CreatePhase("AI");

        ai.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "Final department: AI.\nMy predictions are confident.\nYours should be careful.",
            3.1f,
            secondsPerCharacter: 0.028f,
            shakeAmount: 0.7f));

        ai.BattleWavePool.Add(() => new NeuralNetworkWave());
        ai.BattleWavePool.Add(() => new NumericWarningWave());
        ai.BattleWavePool.Add(() => new VisionPursuitWave());
        ai.BattleWavePool.Add(() => new ReactiveGridWave());

        ai.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS",
            "Patterns detected.\nProbability of your survival: still inconveniently high.",
            3.0f,
            secondsPerCharacter: 0.028f,
            shakeAmount: 0.65f));

        ai.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS",
            "Numbers don't lie.\nAlthough, admittedly, they can be rather unpleasant.",
            3.0f,
            secondsPerCharacter: 0.028f,
            shakeAmount: 0.75f));

        ai.BetweenWaveDialogPool.Add(() => new DialogWave(
            "BOSS",
            "You've dodged every prediction I've thrown at you.\nLet's finish this.",
            3.2f,
            secondsPerCharacter: 0.028f,
            shakeAmount: 0.8f));

        encounter.Phases.Add(ai);


        
        // =========================================================
        // OUTRO — DIALOGUE ONLY
        // =========================================================

        EncounterPhase outro = new()
        {
            Name = "Outro",
            BattleWaveCount = 0,
            ShuffleBattleWavesWithoutReplacement = false,
            PlayBetweenWaveDialogAfterFinalWave = false
        };

        outro.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "...Evaluation complete.",
            2.2f,
            secondsPerCharacter: 0.035f,
            shakeAmount: 0.1f));

        outro.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "Mobile. Web. Artificial Intelligence.\nYou faced every challenge and adapted to them all.",
            3.5f,
            secondsPerCharacter: 0.035f,
            shakeAmount: 0.1f));

        outro.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "You are a worthy developer opponent.\nCSCC would be proud to have you among its ranks.",
            3.5f,
            secondsPerCharacter: 0.035f,
            shakeAmount: 0.05f));

        outro.OpeningDialogs.Add(() => new DialogWave(
            "BOSS",
            "Challenge cleared.\nUntil our next encounter, developer.",
            3.0f,
            secondsPerCharacter: 0.035f,
            shakeAmount: 0.0f));

        encounter.Phases.Add(outro);


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
