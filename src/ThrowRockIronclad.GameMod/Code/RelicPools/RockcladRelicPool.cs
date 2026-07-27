using MegaCrit.Sts2.Core.Models;
using ThrowRockIronclad.ThrowRockIroncladCode.Relics;

namespace ThrowRockIronclad.ThrowRockIroncladCode.RelicPools;

/// <summary>
/// Rockclad's character-specific reward relic pool.
/// The character starts with Burning Blood, so Rock remains obtainable during a run.
/// </summary>
public sealed class RockcladRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "ironclad";

    protected override IEnumerable<RelicModel> GenerateAllRelics()
        => [ModelDb.Relic<Rock>()];
}
