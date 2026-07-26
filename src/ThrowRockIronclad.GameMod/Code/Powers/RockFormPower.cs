using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Core;
using ThrowRockIronclad.ThrowRockIroncladCode.Utilities;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Powers;

public sealed class RockFormPower : ThrowRockIroncladPower
{
    // Amount is the only custom Power state synchronized by the game. The high digits therefore
    // preserve upgraded sources across full-state multiplayer recovery and combat save/load.
    private const int UpgradedSourceUnit = 1_000_000;

    public override string IconFileName => "rock_form_power.png";
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => NormalSources + UpgradedSources;

    private int NormalSources => Amount % UpgradedSourceUnit;
    private int UpgradedSources => Amount / UpgradedSourceUnit;
    private int TotalSources => NormalSources + UpgradedSources;

    public static int ApplicationAmount(bool upgraded) => upgraded ? UpgradedSourceUnit : 1;

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (!participants.Contains(Owner))
        {
            return;
        }

        Flash();
        for (int i = 0; i < NormalSources; i++)
        {
            await GiantRockCreation.AddToCombat(combatState, Owner.Player!, PileType.Hand, upgraded: false);
        }

        for (int i = 0; i < UpgradedSources; i++)
        {
            await GiantRockCreation.AddToCombat(combatState, Owner.Player!, PileType.Hand, upgraded: true);
        }
    }

    public override bool TryModifyEnergyCostInCombat(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        if (card.Owner.Creature == Owner && card.Tags.Contains(RockTags.Rock))
        {
            modifiedCost = RockRules.ReduceRockCost(originalCost, TotalSources);
            return true;
        }

        modifiedCost = originalCost;
        return false;
    }
}
