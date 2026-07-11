using System;
using System.Collections.Generic;

public readonly struct EnemyIntentRoll
{
    public EnemyIntentType Type { get; }
    public int Value { get; }

    public EnemyIntentRoll(EnemyIntentType type, int value)
    {
        Type = type;
        Value = value;
    }
}

public static class IntentResolver
{
    public static EnemyIntentRoll RollEnemyIntent(EnemyUnit enemy, IReadOnlyList<EnemyUnit> encounter, bool isElite, int turn, Random rng)
    {
        var alliesAlive = 0;
        for (var i = 0; i < encounter.Count; i++)
        {
            if (encounter[i].IsAlive)
            {
                alliesAlive++;
            }
        }

        return enemy.ArchetypeId switch
        {
            // === Act 1: Cultist Outpost (minFloor 1-5) ===
            "cultist" => RollCultistIntent(turn, rng),
            "cultist_scout" => RollScoutIntent(turn, rng),
            "cultist_guard" => RollGuardIntent(turn, alliesAlive, rng),
            "cultist_shaman" => RollShamanIntent(turn, alliesAlive, rng),
            "cultist_acolyte" => RollAcolyteIntent(turn, alliesAlive, rng),
            "cultist_brute" => RollBruteIntent(turn, rng),

            // === Act 2: Shadow Bazaar (minFloor 2-5) ===
            "thief_cutpurse" => RollCutpurseIntent(rng),
            "thief_assassin" => RollAssassinIntent(turn, rng),
            "thief_fencer" => RollFencerIntent(turn, rng),
            "slaver_red" => RollRedSlaverIntent(alliesAlive, rng),
            "slaver_blue" => RollBlueSlaverIntent(alliesAlive, rng),
            "plague_rat" => RollPlagueRatIntent(turn, rng),
            "mercenary" => RollMercenaryIntent(rng),

            // === Act 3: Ancient Sanctum (minFloor 5-7) ===
            "corrupted_guardian" => RollCorruptedGuardianIntent(turn, rng),
            "dark_oracle" => RollDarkOracleIntent(turn, rng),
            "void_beast" => RollVoidBeastIntent(turn, rng),
            "ancient_automaton" => RollAncientAutomatonIntent(turn, rng),
            "temple_knight" => RollTempleKnightIntent(turn, rng),
            "flame_wraith" => RollFlameWraithIntent(alliesAlive, rng),

            // === Elites ===
            "elite_sentinel" => RollEliteSentinelIntent(turn, rng),
            "elite_inquisitor" => RollInquisitorIntent(turn, rng),
            "elite_assassin_guild" => RollAssassinGuildIntent(turn, rng),
            "elite_slaver_boss" => RollSlaverBossIntent(alliesAlive, rng),
            "elite_corrupted_golem" => RollCorruptedGolemIntent(turn, rng),
            "elite_ancient_dragon" => RollAncientDragonIntent(turn, rng),

            // === Bosses ===
            "boss_high_priest" => RollHighPriestIntent(turn, rng),
            "boss_master_thief" => RollMasterThiefIntent(turn, rng),
            "boss_corrupted_heart" => RollCorruptedHeartIntent(turn, rng),

            _ => RollDefaultIntent(isElite, rng)
        };
    }

    // ═══════════════════════════════════════════════════════
    // Act 1 — Cultist Outpost
    // ═══════════════════════════════════════════════════════

    /// <summary>Cultist: Basic melee fighter. Sometimes buffs itself. StS parallel: Cultist.</summary>
    private static EnemyIntentRoll RollCultistIntent(int turn, Random rng)
    {
        if (rng.Next(100) < 40)
        {
            return new EnemyIntentRoll(EnemyIntentType.Buff, 2); // Ritual: gain 2 Strength
        }
        return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(6, 9));
    }

    /// <summary>Scout: Fast and fragile. Attacks almost every turn with low damage.</summary>
    private static EnemyIntentRoll RollScoutIntent(int turn, Random rng)
    {
        if (rng.Next(100) < 20)
        {
            return new EnemyIntentRoll(EnemyIntentType.Defend, 4);
        }
        return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(5, 8));
    }

    /// <summary>Guard: Defensive frontline. Opens with shield, then mixes defense and attack.</summary>
    private static EnemyIntentRoll RollGuardIntent(int turn, int alliesAlive, Random rng)
    {
        if (turn == 1)
        {
            return new EnemyIntentRoll(EnemyIntentType.Defend, alliesAlive >= 2 ? 10 : 8);
        }
        var roll = rng.Next(100);
        if (roll < 50) return new EnemyIntentRoll(EnemyIntentType.Defend, 6);
        if (roll < 85) return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(7, 10));
        return new EnemyIntentRoll(EnemyIntentType.Buff, 1);
    }

    /// <summary>Shaman: Team buffer. Buffs allies when present; weak attacks alone.</summary>
    private static EnemyIntentRoll RollShamanIntent(int turn, int alliesAlive, Random rng)
    {
        if (alliesAlive >= 2)
        {
            var roll = rng.Next(100);
            if (roll < 55) return new EnemyIntentRoll(EnemyIntentType.Buff, 2); // Buff all allies
            if (roll < 75) return new EnemyIntentRoll(EnemyIntentType.Defend, 5);
            return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(4, 7));
        }
        // Alone: less effective
        return rng.Next(100) < 50
            ? new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(6, 9))
            : new EnemyIntentRoll(EnemyIntentType.Defend, 5);
    }

    /// <summary>Acolyte: Squishy buffer. High buff uptime, very low HP — kill first!</summary>
    private static EnemyIntentRoll RollAcolyteIntent(int turn, int alliesAlive, Random rng)
    {
        if (alliesAlive >= 2 && rng.Next(100) < 60)
        {
            return new EnemyIntentRoll(EnemyIntentType.Buff, 2); // Empower allies
        }
        return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(4, 7));
    }

    /// <summary>Brute: Heavy hitter. Big damage, periodic rage buff.</summary>
    private static EnemyIntentRoll RollBruteIntent(int turn, Random rng)
    {
        if (turn % 3 == 0)
        {
            return new EnemyIntentRoll(EnemyIntentType.Buff, 3); // Rage
        }
        if (rng.Next(100) < 75)
        {
            return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(10, 14));
        }
        return new EnemyIntentRoll(EnemyIntentType.Defend, 5);
    }

    // ═══════════════════════════════════════════════════════
    // Act 2 — Shadow Bazaar
    // ═══════════════════════════════════════════════════════

    /// <summary>Cutpurse: Aggressive thief. High attack frequency, low defense.</summary>
    private static EnemyIntentRoll RollCutpurseIntent(Random rng)
    {
        if (rng.Next(100) < 80)
        {
            return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(8, 11));
        }
        return new EnemyIntentRoll(EnemyIntentType.Defend, 4);
    }

    /// <summary>Assassin: First-turn ambush buff, then heavy attacks.</summary>
    private static EnemyIntentRoll RollAssassinIntent(int turn, Random rng)
    {
        if (turn == 1)
        {
            return new EnemyIntentRoll(EnemyIntentType.Buff, 2); // Mark Target: buff self
        }
        return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(10, 13));
    }

    /// <summary>Fencer: Balanced duelist. Predictable crit every 4th turn.</summary>
    private static EnemyIntentRoll RollFencerIntent(int turn, Random rng)
    {
        if (turn % 4 == 0)
        {
            return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(14, 17)); // Riposte crit
        }
        return rng.Next(100) < 50
            ? new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(8, 11))
            : new EnemyIntentRoll(EnemyIntentType.Defend, 6);
    }

    /// <summary>Red Slaver: Aggressive. Buffs team between attacks.</summary>
    private static EnemyIntentRoll RollRedSlaverIntent(int alliesAlive, Random rng)
    {
        if (alliesAlive >= 2 && rng.Next(100) < 30)
        {
            return new EnemyIntentRoll(EnemyIntentType.Buff, 1); // Rally team
        }
        return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(10, 13));
    }

    /// <summary>Blue Slaver: Defensive support. Shields allies.</summary>
    private static EnemyIntentRoll RollBlueSlaverIntent(int alliesAlive, Random rng)
    {
        var roll = rng.Next(100);
        if (roll < 60) return new EnemyIntentRoll(EnemyIntentType.Defend, 8);
        if (roll < 85) return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(6, 9));
        return new EnemyIntentRoll(EnemyIntentType.Buff, 1); // Bolster team
    }

    /// <summary>Plague Rat: Debuffer. Alternates between bite and disease.</summary>
    private static EnemyIntentRoll RollPlagueRatIntent(int turn, Random rng)
    {
        if (turn % 2 == 0)
        {
            return new EnemyIntentRoll(EnemyIntentType.Buff, 1); // Plague: debuff player
        }
        return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(6, 9));
    }

    /// <summary>Mercenary: Versatile veteran. Unpredictable pattern.</summary>
    private static EnemyIntentRoll RollMercenaryIntent(Random rng)
    {
        var roll = rng.Next(100);
        if (roll < 45) return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(10, 14));
        if (roll < 75) return new EnemyIntentRoll(EnemyIntentType.Defend, 8);
        return new EnemyIntentRoll(EnemyIntentType.Buff, 2);
    }

    // ═══════════════════════════════════════════════════════
    // Act 3 — Ancient Sanctum
    // ═══════════════════════════════════════════════════════

    /// <summary>Corrupted Guardian: Tough tank. Opens with strong shield.</summary>
    private static EnemyIntentRoll RollCorruptedGuardianIntent(int turn, Random rng)
    {
        if (turn == 1)
        {
            return new EnemyIntentRoll(EnemyIntentType.Defend, 14); // Fortify
        }
        return rng.Next(100) < 65
            ? new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(11, 15))
            : new EnemyIntentRoll(EnemyIntentType.Defend, 8);
    }

    /// <summary>Dark Oracle: Curse caster. Applies stacking debuff every 2 turns.</summary>
    private static EnemyIntentRoll RollDarkOracleIntent(int turn, Random rng)
    {
        if (turn % 2 == 0)
        {
            return new EnemyIntentRoll(EnemyIntentType.Buff, 3); // Curse: debuff player
        }
        return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(9, 12));
    }

    /// <summary>Void Beast: Aggressive berserker. High attack uptime.</summary>
    private static EnemyIntentRoll RollVoidBeastIntent(int turn, Random rng)
    {
        if (rng.Next(100) < 75)
        {
            return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(11, 15));
        }
        return new EnemyIntentRoll(EnemyIntentType.Buff, 2);
    }

    /// <summary>Ancient Automaton: Charge-up burst. 2 turns defense → 1 huge attack.</summary>
    private static EnemyIntentRoll RollAncientAutomatonIntent(int turn, Random rng)
    {
        return (turn % 3) switch
        {
            1 => new EnemyIntentRoll(EnemyIntentType.Defend, 12),              // Charging...
            2 => new EnemyIntentRoll(EnemyIntentType.Defend, 12),              // Almost ready...
            _ => new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(18, 23)) // HYPER BEAM
        };
    }

    /// <summary>Temple Knight: Disciplined 3-turn cycle.</summary>
    private static EnemyIntentRoll RollTempleKnightIntent(int turn, Random rng)
    {
        return (turn % 3) switch
        {
            1 => new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(12, 16)),
            2 => new EnemyIntentRoll(EnemyIntentType.Defend, 10),
            _ => new EnemyIntentRoll(EnemyIntentType.Buff, 2)  // +2 Str + 6 Block
        };
    }

    /// <summary>Flame Wraith: Explosive spirit. Buffs team, explodes on death.</summary>
    private static EnemyIntentRoll RollFlameWraithIntent(int alliesAlive, Random rng)
    {
        if (alliesAlive >= 2 && rng.Next(100) < 40)
        {
            return new EnemyIntentRoll(EnemyIntentType.Buff, 1); // Fan flames: buff all
        }
        return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(8, 11));
    }

    // ═══════════════════════════════════════════════════════
    // Elites
    // ═══════════════════════════════════════════════════════

    /// <summary>Elite Sentinel: Predictable 3-turn cycle. Telegraph everything.</summary>
    private static EnemyIntentRoll RollEliteSentinelIntent(int turn, Random rng)
    {
        return (turn % 3) switch
        {
            1 => new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(10, 14)),
            2 => new EnemyIntentRoll(EnemyIntentType.Defend, 12),
            _ => new EnemyIntentRoll(EnemyIntentType.Buff, 3)
        };
    }

    /// <summary>Inquisitor: Opens with debuff, then aggressive attacks.</summary>
    private static EnemyIntentRoll RollInquisitorIntent(int turn, Random rng)
    {
        if (turn == 1)
        {
            return new EnemyIntentRoll(EnemyIntentType.Buff, 3); // Mark Heretic
        }
        return rng.Next(100) < 65
            ? new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(10, 14))
            : new EnemyIntentRoll(EnemyIntentType.Defend, 7);
    }

    /// <summary>Assassin Lord: Efficient multi-action turns. StS parallel: Gremlin Leader.</summary>
    private static EnemyIntentRoll RollAssassinGuildIntent(int turn, Random rng)
    {
        return (turn % 3) switch
        {
            1 => new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(13, 17)),
            2 => new EnemyIntentRoll(EnemyIntentType.Defend, 8),
            _ => new EnemyIntentRoll(EnemyIntentType.Buff, 2)  // Prep: buff + light attack
        };
    }

    /// <summary>Slaver Chief: Team commander. Defensive when alone.</summary>
    private static EnemyIntentRoll RollSlaverBossIntent(int alliesAlive, Random rng)
    {
        if (alliesAlive <= 1)
        {
            return new EnemyIntentRoll(EnemyIntentType.Defend, 12); // Desperate guard
        }
        if (rng.Next(3) == 0)
        {
            return new EnemyIntentRoll(EnemyIntentType.Buff, 2); // Whip Crack: buff team
        }
        return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(12, 16));
    }

    /// <summary>Corrupted Golem: Gains strength when hit. Rages harder as fight goes on.</summary>
    private static EnemyIntentRoll RollCorruptedGolemIntent(int turn, Random rng)
    {
        if (rng.Next(100) < 75)
        {
            return new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(15, 21));
        }
        return new EnemyIntentRoll(EnemyIntentType.Defend, 12);
    }

    /// <summary>Ancient Wyrm: 3-turn elemental cycle with AoE.</summary>
    private static EnemyIntentRoll RollAncientDragonIntent(int turn, Random rng)
    {
        return (turn % 3) switch
        {
            1 => new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(12, 15)),  // Fire breath (AoE marker)
            2 => new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(16, 19)),  // Claw
            _ => new EnemyIntentRoll(EnemyIntentType.Defend, 14)                  // Take Flight
        };
    }

    // ═══════════════════════════════════════════════════════
    // Bosses
    // ═══════════════════════════════════════════════════════

    /// <summary>Act 1 Boss: High Priest Morg. 4-turn cycle. Phase 2 at 50% HP (handled externally).</summary>
    private static EnemyIntentRoll RollHighPriestIntent(int turn, Random rng)
    {
        return (turn % 4) switch
        {
            1 => new EnemyIntentRoll(EnemyIntentType.Buff, 2),                // Dark Prayer
            2 => new EnemyIntentRoll(EnemyIntentType.Defend, 12),             // Barrier of Faith
            3 => new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(14, 18)), // Smite
            _ => new EnemyIntentRoll(EnemyIntentType.Defend, 8)               // Heal Ward
        };
    }

    /// <summary>Act 2 Boss: Master Thief Rane. 5-turn cycle. Agile and unpredictable.</summary>
    private static EnemyIntentRoll RollMasterThiefIntent(int turn, Random rng)
    {
        return (turn % 5) switch
        {
            1 => new EnemyIntentRoll(EnemyIntentType.Defend, 18),               // Smoke Bomb
            2 => new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(14, 18)), // Backstab
            3 => new EnemyIntentRoll(EnemyIntentType.Defend, 10),               // Shadow Step
            4 => new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(10, 13)), // Dagger Flurry
            _ => new EnemyIntentRoll(EnemyIntentType.Defend, 22)                // Vanish
        };
    }

    /// <summary>Act 3 Boss: Corrupted Heart. 5-turn P1, 3-turn P2. Ultimate challenge.</summary>
    private static EnemyIntentRoll RollCorruptedHeartIntent(int turn, Random rng)
    {
        // Phase 1 (turns 1-10): 5-turn cycle
        if (turn <= 10)
        {
            return (turn % 5) switch
            {
                1 => new EnemyIntentRoll(EnemyIntentType.Buff, 5),                // Dark Will
                2 => new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(8, 13)), // Corruption Strike
                3 => new EnemyIntentRoll(EnemyIntentType.Defend, 28),              // Wall of Despair
                4 => new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(20, 26)),// Void Devour
                _ => new EnemyIntentRoll(EnemyIntentType.Buff, 2)                  // Spread Corruption
            };
        }
        // Phase 2 (turns 11+): 3-turn aggressive cycle
        return (turn % 3) switch
        {
            1 => new EnemyIntentRoll(EnemyIntentType.Attack, 14),                // Despair
            2 => new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(12, 17)),  // Madness
            _ => new EnemyIntentRoll(EnemyIntentType.Attack, rng.Next(22, 28))   // Annihilate
        };
    }

    // ═══════════════════════════════════════════════════════
    // Default
    // ═══════════════════════════════════════════════════════

    private static EnemyIntentRoll RollDefaultIntent(bool isElite, Random rng)
    {
        var roll = rng.Next(100);
        if (roll < (isElite ? 70 : 60))
        {
            return new EnemyIntentRoll(EnemyIntentType.Attack,
                isElite ? rng.Next(9, 14) : rng.Next(6, 10));
        }
        if (roll < 85)
        {
            return new EnemyIntentRoll(EnemyIntentType.Defend, isElite ? 10 : 6);
        }
        return new EnemyIntentRoll(EnemyIntentType.Buff, isElite ? 3 : 2);
    }
}
