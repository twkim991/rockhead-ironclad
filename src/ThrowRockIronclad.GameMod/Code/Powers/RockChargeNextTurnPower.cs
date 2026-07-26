using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using ThrowRockIronclad.ThrowRockIroncladCode.Utilities;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Powers;

public sealed class RockChargeNextTurnPower : ThrowRockIroncladPower
{
    // Power Amount is synchronized and saved by the game. Encoding upgraded sources in the
    // high digits preserves an exact normal/upgraded mix when multiple copies are played.
    private const int UpgradedSourceUnit = 1_000_000;

    public override string IconFileName => "rock_charge_power.png";
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => CountSources(Amount);

    public static int ApplicationAmount(bool upgraded) => upgraded ? UpgradedSourceUnit : 1;

    public static int CountSources(int amount)
        => NormalSources(amount) + UpgradedSources(amount);

    public static int NormalSources(int amount) => amount % UpgradedSourceUnit;

    public static int UpgradedSources(int amount) => amount / UpgradedSourceUnit;

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        int scheduledAmount = AmountOnTurnStart;
        if (!participants.Contains(Owner) || scheduledAmount == 0)
        {
            return;
        }

        Flash();
        for (int i = 0; i < NormalSources(scheduledAmount); i++)
        {
            await GiantRockCreation.AddToCombat(combatState, Owner.Player!, PileType.Hand, upgraded: false);
        }

        for (int i = 0; i < UpgradedSources(scheduledAmount); i++)
        {
            await GiantRockCreation.AddToCombat(combatState, Owner.Player!, PileType.Hand, upgraded: true);
        }

        await PowerCmd.Remove(this);
    }
}
