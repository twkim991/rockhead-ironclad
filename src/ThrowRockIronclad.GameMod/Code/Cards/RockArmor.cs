using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using ThrowRockIronclad.ThrowRockIroncladCode.Core;
using ThrowRockIronclad.ThrowRockIroncladCode.Powers;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Cards;

public sealed class RockArmor : ThrowRockIroncladCard
{
    public RockArmor()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(RockRules.RockArmorBlock, ValueProp.Unpowered),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Block),
        HoverTipFactory.FromCard<GiantRock>(false),
    ];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return PowerCmd.Apply<RockArmorPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["Block"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Block"].UpgradeValueBy(RockRules.RockArmorBlockUpgrade);
    }
}
