using System;

public readonly struct DamageResolution
{
    public int FinalDamage { get; }
    public int Blocked { get; }
    public int Taken { get; }
    public int RemainingBlock { get; }
    public int RemainingHp { get; }

    public DamageResolution(int finalDamage, int blocked, int taken, int remainingBlock, int remainingHp)
    {
        FinalDamage = finalDamage;
        Blocked = blocked;
        Taken = taken;
        RemainingBlock = remainingBlock;
        RemainingHp = remainingHp;
    }
}

public static class CombatResolver
{
    public static int ApplyVulnerableMultiplier(int baseDamage, int vulnerableTurns)
    {
        if (vulnerableTurns <= 0)
        {
            return baseDamage;
        }

        return (int)Math.Ceiling(baseDamage * 1.5);
    }

    public static DamageResolution ResolveHit(
        int baseDamage,
        int attackerStrength,
        int targetVulnerable,
        int targetBlock,
        int targetHp,
        int flatBonus = 0)
    {
        var rawDamage = Math.Max(baseDamage + attackerStrength + flatBonus, 0);
        var finalDamage = ApplyVulnerableMultiplier(rawDamage, targetVulnerable);
        var blocked = Math.Min(Math.Max(targetBlock, 0), finalDamage);
        var taken = Math.Max(finalDamage - blocked, 0);
        var remainingBlock = Math.Max(targetBlock - finalDamage, 0);
        var remainingHp = Math.Max(targetHp - taken, 0);
        return new DamageResolution(finalDamage, blocked, taken, remainingBlock, remainingHp);
    }
}
