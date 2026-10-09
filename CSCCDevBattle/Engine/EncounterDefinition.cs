using System.Collections.Generic;

public sealed class EncounterDefinition
{
    public string Name { get; set; } = "Encounter";

    public List<EncounterPhase> Phases { get; } = [];
}