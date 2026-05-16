public static class MapProgressionRules
{
    public const int MaxActs = 3;
    // 10 rows per act: 1 intro (row 0) + 8 content rows (1..8) + 1 boss (row 9).
    // Row 8 (the row directly before the boss) is always Rest.
    public const int RowsPerAct = 10;

    public static double UpgradeChanceForAct(int act)
    {
        if (act <= 1)
        {
            return 0.10;
        }

        if (act == 2)
        {
            return 0.25;
        }

        return 0.40;
    }
}
