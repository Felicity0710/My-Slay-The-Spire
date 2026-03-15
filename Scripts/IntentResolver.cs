using System;

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
    public static EnemyIntentRoll RollEnemyIntent(bool isElite, Random rng)
    {
        var roll = rng.Next(100);
        if (roll < (isElite ? 70 : 60))
        {
            return new EnemyIntentRoll(
                EnemyIntentType.Attack,
                isElite ? rng.Next(9, 15) : rng.Next(6, 11));
        }

        if (roll < 85)
        {
            return new EnemyIntentRoll(
                EnemyIntentType.Defend,
                isElite ? 10 : 6);
        }

        return new EnemyIntentRoll(
            EnemyIntentType.Buff,
            isElite ? 3 : 2);
    }
}
