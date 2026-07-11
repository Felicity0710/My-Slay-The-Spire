using System;
using System.Collections.Generic;

public static class EnemyEncounterBuilder
{
    private static readonly EnemyEncounterCatalog Catalog = EnemyEncounterCatalog.Load();

    /// <summary>
    /// Build an encounter by selecting a random subset of qualified enemies.
    /// Encounters contain 1-3 enemies depending on type and floor.
    /// </summary>
    public static List<EnemyUnit> BuildEncounter(MapNodeType encounterType, int floor, int act, Random rng)
    {
        if (!Catalog.EncounterMembersByType.TryGetValue(encounterType, out var members))
        {
            throw new InvalidOperationException($"No encounter rule configured for encounterType={encounterType}");
        }

        // Collect all eligible enemies for this floor
        var eligible = new List<EnemyEncounterCatalog.EncounterMember>();
        foreach (var member in members)
        {
            if (floor >= member.MinFloor)
            {
                eligible.Add(member);
            }
        }

        if (eligible.Count == 0)
        {
            throw new InvalidOperationException($"No eligible enemies for {encounterType} at floor {floor}");
        }

        // Boss encounters: select the correct boss for the current act
        List<EnemyEncounterCatalog.EncounterMember> selected;
        if (encounterType == MapNodeType.Boss)
        {
            int bossIndex = Math.Clamp(act - 1, 0, eligible.Count - 1);
            selected = new List<EnemyEncounterCatalog.EncounterMember> { eligible[bossIndex] };
        }
        else
        {
            // Determine target enemy count based on encounter type and floor progression
            // Slay the Spire reference: 1-2 early, 2-3 mid, 2-3 late
            int targetCount = encounterType switch
            {
                MapNodeType.EliteBattle => floor >= 6 ? 2 : 1,
                MapNodeType.MerchantFight => 1,
                _ => floor switch // NormalBattle
                {
                    <= 3 => 1,
                    <= 6 => rng.Next(1, 3),
                    _    => rng.Next(2, 4),
                }
            };

            var shuffled = new List<EnemyEncounterCatalog.EncounterMember>(eligible);
            FisherYatesShuffle(shuffled, rng);
            selected = new List<EnemyEncounterCatalog.EncounterMember>();
            for (int i = 0; i < Math.Min(targetCount, shuffled.Count); i++)
            {
                selected.Add(shuffled[i]);
            }
        }

        // Build enemy units
        var enemies = new List<EnemyUnit>();
        foreach (var member in selected)
        {
            enemies.Add(Create(member, floor));
        }

        return enemies;
    }

    private static EnemyUnit Create(EnemyEncounterCatalog.EncounterMember member, int floor)
    {
        if (!Catalog.ArchetypesById.TryGetValue(member.ArchetypeId, out var archetype))
        {
            throw new InvalidOperationException($"Unknown enemy archetype: {member.ArchetypeId}");
        }

        var hp = archetype.BaseHp + floor * archetype.HpPerFloor;
        return new EnemyUnit
        {
            ArchetypeId = archetype.Id,
            Name = Catalog.BuildName(archetype, member.Suffix),
            VisualId = archetype.VisualId,
            Hp = hp,
            MaxHp = hp,
            Strength = Catalog.ResolveStrength(member.Strength, floor)
        };
    }

    private static void FisherYatesShuffle<T>(List<T> list, Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
