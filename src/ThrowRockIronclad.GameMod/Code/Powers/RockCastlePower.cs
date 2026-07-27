using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Powers;

public sealed class RockCastlePower : ThrowRockIroncladPower
{
    public override string IconFileName => "rockade_power.png";
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        .. HoverTipFactory.FromCardWithCardHoverTips<GiantRock>(),
        HoverTipFactory.Static(StaticHoverTip.ReplayStatic),
    ];

    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is RockCastlePower && power.Owner == Owner)
        {
            foreach (CardModel card in Owner.Player?.PlayerCombatState?.AllCards ?? [])
            {
                TryAddReplay(card, (int)amount);
            }
        }

        return Task.CompletedTask;
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (!card.IsClone)
        {
            TryAddReplay(card, Amount);
        }

        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        foreach (CardModel card in oldOwner.Player?.PlayerCombatState?.AllCards ?? [])
        {
            if (card is GiantRock)
            {
                card.BaseReplayCount -= Amount;
            }
        }

        return Task.CompletedTask;
    }

    private bool TryAddReplay(CardModel card, int amount)
    {
        if (card.Owner != Owner.Player || card is not GiantRock)
        {
            return false;
        }

        card.BaseReplayCount += amount;
        return true;
    }
}
