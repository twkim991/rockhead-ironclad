namespace ThrowRockIronclad.ThrowRockIroncladCode.Core;

public static class RockRules
{
    public const int RockadeBlock = 2;
    public const int RockadeBlockUpgrade = 1;
    public const int RockArmorBlock = 4;
    public const int RockArmorBlockUpgrade = 2;
    public const int AbsoluteRockDamage = 6;
    public const int RockSlamDamage = 5;

    public static int CalculateRockadeBlock(int finishedGiantRockPlays, int blockPerRock)
        => Math.Max(0, finishedGiantRockPlays) * Math.Max(0, blockPerRock);

    public static decimal ReduceRockCost(decimal originalCost, int reduction)
        => Math.Max(0m, originalCost - Math.Max(0, reduction));
}
