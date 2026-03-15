using System;
using System.Collections.Generic;

public readonly struct PlayerTurnStartState
{
    public int Energy { get; }
    public int PlayerBlock { get; }

    public PlayerTurnStartState(int energy, int playerBlock)
    {
        Energy = energy;
        PlayerBlock = playerBlock;
    }
}

public readonly struct EndOfRoundStatusState
{
    public int PlayerVulnerable { get; }
    public int EnemyVulnerable { get; }

    public EndOfRoundStatusState(int playerVulnerable, int enemyVulnerable)
    {
        PlayerVulnerable = playerVulnerable;
        EnemyVulnerable = enemyVulnerable;
    }
}

public static class TurnFlowResolver
{
    public static PlayerTurnStartState ResolvePlayerTurnStart(
        int turn,
        int maxEnergy,
        bool hasLantern,
        bool hasAnchor)
    {
        var energy = maxEnergy;
        var playerBlock = 0;

        if (turn == 1 && hasLantern)
        {
            energy += 1;
        }

        if (turn == 1 && hasAnchor)
        {
            playerBlock += 8;
        }

        return new PlayerTurnStartState(energy, playerBlock);
    }

    public static int ResolveEnemyTurnStartBlock(int enemyBlock)
    {
        return 0;
    }

    public static EndOfRoundStatusState ResolveEndOfRoundStatuses(int playerVulnerable, int enemyVulnerable)
    {
        return new EndOfRoundStatusState(
            Math.Max(playerVulnerable - 1, 0),
            Math.Max(enemyVulnerable - 1, 0));
    }

    public static void MoveHandToDiscard<T>(IList<T> hand, IList<T> discard)
    {
        for (var i = 0; i < hand.Count; i++)
        {
            discard.Add(hand[i]);
        }
        hand.Clear();
    }
}
