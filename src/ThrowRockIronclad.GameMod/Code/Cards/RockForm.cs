using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Powers;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Cards;

public sealed class RockForm : ThrowRockIroncladCard
{
    public RockForm()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<GiantRock>(IsUpgraded),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<RockFormPower>(
            choiceContext,
            Owner.Creature,
            RockFormPower.ApplicationAmount(IsUpgraded),
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
    }
}
