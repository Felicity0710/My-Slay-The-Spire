using System;
using System.Collections.Generic;

public static class EnemyEncounterBuilder
{
    private sealed record EnemyArchetype(string Id, string DisplayName, string VisualId, int BaseHp, int HpPerFloor);

    private static readonly Dictionary<string, EnemyArchetype> Archetypes = new()
    {
        ["cultist"] = new EnemyArchetype("cultist", "Cultist", "cultist", 36, 7),
        ["cultist_scout"] = new EnemyArchetype("cultist_scout", "Cultist", "cultist", 32, 6),
        ["elite_sentinel"] = new EnemyArchetype("elite_sentinel", "Elite Sentinel", "elite_sentinel", 78, 10)
    };

    public static List<EnemyUnit> BuildEncounter(MapNodeType encounterType, int floor)
    {
        var enemies = new List<EnemyUnit>();
        if (encounterType == MapNodeType.EliteBattle)
        {
            enemies.Add(Create("elite_sentinel", floor, "A", strength: Math.Max(floor / 2, 1)));
            enemies.Add(Create("elite_sentinel", floor, "B", strength: Math.Max(floor / 2, 1)));
            return enemies;
        }

        enemies.Add(Create("cultist", floor, "A", strength: Math.Max(floor - 1, 0)));
        enemies.Add(Create("cultist", floor, "B", strength: Math.Max(floor - 1, 0)));
        if (floor >= 3)
        {
            enemies.Add(Create("cultist_scout", floor, "C", strength: Math.Max(floor - 1, 0)));
        }

        return enemies;
    }

    private static EnemyUnit Create(string archetypeId, int floor, string suffix, int strength)
    {
        if (!Archetypes.TryGetValue(archetypeId, out var archetype))
        {
            throw new InvalidOperationException($"Unknown enemy archetype: {archetypeId}");
        }

        var hp = archetype.BaseHp + floor * archetype.HpPerFloor;
        return new EnemyUnit
        {
            Name = $"{archetype.DisplayName} {suffix}",
            VisualId = archetype.VisualId,
            Hp = hp,
            MaxHp = hp,
            Strength = strength
        };
    }
}
