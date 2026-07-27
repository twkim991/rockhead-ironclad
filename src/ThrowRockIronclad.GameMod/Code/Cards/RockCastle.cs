using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Powers;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Cards;

public sealed class RockCastle : ThrowRockIroncladCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<RockCastlePower>(1m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        .. HoverTipFactory.FromCardWithCardHoverTips<GiantRock>(),
        HoverTipFactory.Static(StaticHoverTip.ReplayStatic),
    ];

    public RockCastle()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<RockCastlePower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["RockCastlePower"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
