using System;
using System.Collections.Generic;

internal static class Program
{
    private static int Main()
    {
        var tests = new List<(string Name, Action Run)>
        {
            ("Vulnerable multiplier rounds up", TestVulnerableRounding),
            ("No vulnerable keeps base damage", TestNoVulnerable),
            ("Block fully absorbs hit", TestFullBlock),
            ("Block partially absorbs hit", TestPartialBlock),
            ("Strength and relic bonus stack", TestStrengthAndFlatBonus),
            ("Damage never drops below zero", TestClampToZero),
            ("Turn-1 relic bonuses apply once", TestTurnOneRelicBonuses),
            ("Non-turn-1 has no opening relic bonus", TestLaterTurnNoOpeningBonus),
            ("End of round decrements statuses", TestEndOfRoundStatusDecay),
            ("Hand cards move to discard", TestMoveHandToDiscard),
            ("Draw stops at hand limit", TestDrawStopsAtHandLimit),
            ("Draw reshuffles discard when needed", TestDrawReshufflesDiscard),
            ("Draw halts when no cards available", TestDrawHaltsWhenNoCards),
            ("Normal intent values stay in expected ranges", TestNormalIntentRanges),
            ("Elite intent values stay in expected ranges", TestEliteIntentRanges),
            ("Elite attack rate is higher than normal", TestEliteAttackRateHigher),
            ("Normal encounter roster scales by floor", TestNormalEncounterRoster),
            ("Elite encounter roster uses sentinel archetype", TestEliteEncounterRoster)
            ("Elite attack rate is higher than normal", TestEliteAttackRateHigher),
            ("Card effects aggregate into legacy fields", TestCardEffectsAggregateLegacyFields),
            ("Card supports complex configurable effects", TestComplexCardEffectConfiguration),
            ("CreateById returns independent card instances", TestCreateByIdReturnsIndependentInstances)
        };

        var failed = 0;
        foreach (var test in tests)
        {
            try
            {
                test.Run();
                Console.WriteLine($"[PASS] {test.Name}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.WriteLine($"[FAIL] {test.Name}: {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Total: {tests.Count}, Failed: {failed}");
        return failed == 0 ? 0 : 1;
    }

    private static void TestVulnerableRounding()
    {
        var result = CombatResolver.ResolveHit(
            baseDamage: 5,
            attackerStrength: 0,
            targetVulnerable: 1,
            targetBlock: 0,
            targetHp: 50);
        ExpectEqual(8, result.FinalDamage, nameof(result.FinalDamage));
        ExpectEqual(42, result.RemainingHp, nameof(result.RemainingHp));
    }

    private static void TestNoVulnerable()
    {
        var result = CombatResolver.ResolveHit(
            baseDamage: 7,
            attackerStrength: 0,
            targetVulnerable: 0,
            targetBlock: 0,
            targetHp: 30);
        ExpectEqual(7, result.FinalDamage, nameof(result.FinalDamage));
        ExpectEqual(23, result.RemainingHp, nameof(result.RemainingHp));
    }

    private static void TestFullBlock()
    {
        var result = CombatResolver.ResolveHit(
            baseDamage: 6,
            attackerStrength: 0,
            targetVulnerable: 0,
            targetBlock: 10,
            targetHp: 25);
        ExpectEqual(6, result.FinalDamage, nameof(result.FinalDamage));
        ExpectEqual(6, result.Blocked, nameof(result.Blocked));
        ExpectEqual(0, result.Taken, nameof(result.Taken));
        ExpectEqual(4, result.RemainingBlock, nameof(result.RemainingBlock));
        ExpectEqual(25, result.RemainingHp, nameof(result.RemainingHp));
    }

    private static void TestPartialBlock()
    {
        var result = CombatResolver.ResolveHit(
            baseDamage: 12,
            attackerStrength: 0,
            targetVulnerable: 0,
            targetBlock: 5,
            targetHp: 40);
        ExpectEqual(12, result.FinalDamage, nameof(result.FinalDamage));
        ExpectEqual(5, result.Blocked, nameof(result.Blocked));
        ExpectEqual(7, result.Taken, nameof(result.Taken));
        ExpectEqual(0, result.RemainingBlock, nameof(result.RemainingBlock));
        ExpectEqual(33, result.RemainingHp, nameof(result.RemainingHp));
    }

    private static void TestStrengthAndFlatBonus()
    {
        var result = CombatResolver.ResolveHit(
            baseDamage: 6,
            attackerStrength: 2,
            targetVulnerable: 1,
            targetBlock: 0,
            targetHp: 40,
            flatBonus: 1);
        ExpectEqual(14, result.FinalDamage, nameof(result.FinalDamage));
        ExpectEqual(26, result.RemainingHp, nameof(result.RemainingHp));
    }

    private static void TestClampToZero()
    {
        var result = CombatResolver.ResolveHit(
            baseDamage: -2,
            attackerStrength: -5,
            targetVulnerable: 0,
            targetBlock: 0,
            targetHp: 10);
        ExpectEqual(0, result.FinalDamage, nameof(result.FinalDamage));
        ExpectEqual(10, result.RemainingHp, nameof(result.RemainingHp));
    }

    private static void TestTurnOneRelicBonuses()
    {
        var result = TurnFlowResolver.ResolvePlayerTurnStart(
            turn: 1,
            maxEnergy: 3,
            hasLantern: true,
            hasAnchor: true);
        ExpectEqual(4, result.Energy, nameof(result.Energy));
        ExpectEqual(8, result.PlayerBlock, nameof(result.PlayerBlock));
    }

    private static void TestLaterTurnNoOpeningBonus()
    {
        var result = TurnFlowResolver.ResolvePlayerTurnStart(
            turn: 2,
            maxEnergy: 3,
            hasLantern: true,
            hasAnchor: true);
        ExpectEqual(3, result.Energy, nameof(result.Energy));
        ExpectEqual(0, result.PlayerBlock, nameof(result.PlayerBlock));
    }

    private static void TestEndOfRoundStatusDecay()
    {
        var result = TurnFlowResolver.ResolveEndOfRoundStatuses(
            playerVulnerable: 2,
            enemyVulnerable: 1);
        ExpectEqual(1, result.PlayerVulnerable, nameof(result.PlayerVulnerable));
        ExpectEqual(0, result.EnemyVulnerable, nameof(result.EnemyVulnerable));
    }

    private static void TestMoveHandToDiscard()
    {
        var hand = new List<string> { "strike", "defend", "bash" };
        var discard = new List<string> { "old" };
        TurnFlowResolver.MoveHandToDiscard(hand, discard);

        ExpectEqual(0, hand.Count, "hand.Count");
        ExpectEqual(4, discard.Count, "discard.Count");
        ExpectEqual("old", discard[0], "discard[0]");
        ExpectEqual("strike", discard[1], "discard[1]");
        ExpectEqual("defend", discard[2], "discard[2]");
        ExpectEqual("bash", discard[3], "discard[3]");
    }

    private static void TestDrawStopsAtHandLimit()
    {
        var rng = new Random(1);
        var drawPile = new List<string> { "a", "b", "c" };
        var discardPile = new List<string>();
        var hand = new List<string> { "h1", "h2" };

        var result = DeckFlowResolver.DrawIntoHand(
            drawPile,
            discardPile,
            hand,
            drawCount: 3,
            handLimit: 3,
            rng);

        ExpectEqual(true, result.HandLimitReached, nameof(result.HandLimitReached));
        ExpectEqual(1, result.DrawnCards.Count, "result.DrawnCards.Count");
        ExpectEqual(3, hand.Count, "hand.Count");
        ExpectEqual(2, drawPile.Count, "drawPile.Count");
    }

    private static void TestDrawReshufflesDiscard()
    {
        var rng = new Random(42);
        var drawPile = new List<string> { "top" };
        var discardPile = new List<string> { "x", "y", "z" };
        var hand = new List<string>();

        var result = DeckFlowResolver.DrawIntoHand(
            drawPile,
            discardPile,
            hand,
            drawCount: 3,
            handLimit: 10,
            rng);

        ExpectEqual(false, result.HandLimitReached, nameof(result.HandLimitReached));
        ExpectEqual(1, result.ReshuffleCount, nameof(result.ReshuffleCount));
        ExpectEqual(3, result.DrawnCards.Count, "result.DrawnCards.Count");
        ExpectEqual(3, hand.Count, "hand.Count");
        ExpectEqual(1, drawPile.Count, "drawPile.Count");
        ExpectEqual(0, discardPile.Count, "discardPile.Count");
        ExpectEqual("top", hand[0], "hand[0]");
    }

    private static void TestDrawHaltsWhenNoCards()
    {
        var rng = new Random(7);
        var drawPile = new List<string>();
        var discardPile = new List<string>();
        var hand = new List<string>();

        var result = DeckFlowResolver.DrawIntoHand(
            drawPile,
            discardPile,
            hand,
            drawCount: 5,
            handLimit: 10,
            rng);

        ExpectEqual(0, result.DrawnCards.Count, "result.DrawnCards.Count");
        ExpectEqual(0, result.ReshuffleCount, nameof(result.ReshuffleCount));
        ExpectEqual(false, result.HandLimitReached, nameof(result.HandLimitReached));
    }

    private static void TestNormalIntentRanges()
    {
        var rng = new Random(1337);
        for (var i = 0; i < 5000; i++)
        {
            var intent = IntentResolver.RollEnemyIntent(isElite: false, rng);
            switch (intent.Type)
            {
                case EnemyIntentType.Attack:
                    ExpectInRange(intent.Value, 6, 10, "normal attack value");
                    break;
                case EnemyIntentType.Defend:
                    ExpectEqual(6, intent.Value, "normal defend value");
                    break;
                case EnemyIntentType.Buff:
                    ExpectEqual(2, intent.Value, "normal buff value");
                    break;
            }
        }
    }

    private static void TestEliteIntentRanges()
    {
        var rng = new Random(2025);
        for (var i = 0; i < 5000; i++)
        {
            var intent = IntentResolver.RollEnemyIntent(isElite: true, rng);
            switch (intent.Type)
            {
                case EnemyIntentType.Attack:
                    ExpectInRange(intent.Value, 9, 14, "elite attack value");
                    break;
                case EnemyIntentType.Defend:
                    ExpectEqual(10, intent.Value, "elite defend value");
                    break;
                case EnemyIntentType.Buff:
                    ExpectEqual(3, intent.Value, "elite buff value");
                    break;
            }
        }
    }

    private static void TestCardEffectsAggregateLegacyFields()
    {
        var card = CardData.CreateById("quick_slash");

        ExpectEqual(2, card.Effects.Count, "card.Effects.Count");
        ExpectEqual(7, card.Damage, nameof(card.Damage));
        ExpectEqual(0, card.Block, nameof(card.Block));
        ExpectEqual(0, card.ApplyVulnerable, nameof(card.ApplyVulnerable));
        ExpectEqual(1, card.DrawCount, nameof(card.DrawCount));
        ExpectEqual(true, card.HasEffect(CardEffectType.Damage), "card.HasEffect(Damage)");
        ExpectEqual(true, card.HasEffect(CardEffectType.DrawCards), "card.HasEffect(DrawCards)");
    }

    private static void TestComplexCardEffectConfiguration()
    {
        var card = CardData.CreateById("whirlwind");

        ExpectEqual(CardKind.Attack, card.Kind, nameof(card.Kind));
        ExpectEqual(1, card.Effects.Count, "card.Effects.Count");
        var effect = card.Effects[0];
        ExpectEqual(CardEffectType.Damage, effect.Type, nameof(effect.Type));
        ExpectEqual(CardEffectTarget.AllEnemies, effect.Target, nameof(effect.Target));
        ExpectEqual(4, effect.Amount, nameof(effect.Amount));
        ExpectEqual(2, effect.Repeat, nameof(effect.Repeat));
    }

    private static void TestCreateByIdReturnsIndependentInstances()
    {
        var a = CardData.CreateById("strike");
        var b = CardData.CreateById("strike");

        if (ReferenceEquals(a, b))
        {
            throw new InvalidOperationException("CreateById should return independent instances for duplicate cards in deck/hand.");
        }
    }

    private static void TestEliteAttackRateHigher()
    {
        const int sampleSize = 20000;
        var normalRng = new Random(11);
        var eliteRng = new Random(11);
        var normalAttack = 0;
        var eliteAttack = 0;

        for (var i = 0; i < sampleSize; i++)
        {
            if (IntentResolver.RollEnemyIntent(isElite: false, normalRng).Type == EnemyIntentType.Attack)
            {
                normalAttack++;
            }

            if (IntentResolver.RollEnemyIntent(isElite: true, eliteRng).Type == EnemyIntentType.Attack)
            {
                eliteAttack++;
            }
        }

        var normalRate = normalAttack / (double)sampleSize;
        var eliteRate = eliteAttack / (double)sampleSize;
        ExpectInRange(normalRate, 0.55, 0.65, "normal attack rate");
        ExpectInRange(eliteRate, 0.65, 0.75, "elite attack rate");
        if (eliteRate <= normalRate)
        {
            throw new InvalidOperationException(
                $"elite attack rate should be higher than normal: elite={eliteRate:F3}, normal={normalRate:F3}");
        }
    }

    private static void TestNormalEncounterRoster()
    {
        var early = EnemyEncounterBuilder.BuildEncounter(MapNodeType.NormalBattle, floor: 1);
        ExpectEqual(2, early.Count, "early.Count");
        ExpectEqual("Cultist A", early[0].Name, "early[0].Name");
        ExpectEqual(43, early[0].Hp, "early[0].Hp");

        var later = EnemyEncounterBuilder.BuildEncounter(MapNodeType.NormalBattle, floor: 3);
        ExpectEqual(3, later.Count, "later.Count");
        ExpectEqual("Cultist C", later[2].Name, "later[2].Name");
        ExpectEqual(50, later[2].Hp, "later[2].Hp");
    }

    private static void TestEliteEncounterRoster()
    {
        var roster = EnemyEncounterBuilder.BuildEncounter(MapNodeType.EliteBattle, floor: 4);
        ExpectEqual(2, roster.Count, "roster.Count");
        ExpectEqual("Elite Sentinel A", roster[0].Name, "roster[0].Name");
        ExpectEqual("elite_sentinel", roster[0].VisualId, "roster[0].VisualId");
        ExpectEqual(118, roster[0].Hp, "roster[0].Hp");
        ExpectEqual(2, roster[0].Strength, "roster[0].Strength");
    }

    private static void ExpectEqual(int expected, int actual, string label)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
        }
    }

    private static void ExpectEqual(string expected, string actual, string label)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
        }
    }

    private static void ExpectEqual(bool expected, bool actual, string label)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException($"{label}: expected {expected}, got {actual}");
        }
    }

    private static void ExpectInRange(int value, int min, int max, string label)
    {
        if (value < min || value > max)
        {
            throw new InvalidOperationException($"{label}: expected in [{min}, {max}], got {value}");
        }
    }

    private static void ExpectInRange(double value, double min, double max, string label)
    {
        if (value < min || value > max)
        {
            throw new InvalidOperationException($"{label}: expected in [{min}, {max}], got {value:F4}");
        }
    }
}
