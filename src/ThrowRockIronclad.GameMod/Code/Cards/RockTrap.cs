using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Cards;

public sealed class RockTrap : ThrowRockIroncladCard
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<GiantRock>(IsUpgraded),
    ];

    public RockTrap()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        GiantRock[] rocks = PileType.Discard.GetPile(Owner).Cards
            .OfType<GiantRock>()
            .ToArray();

        bool showPileVisuals = true;
        foreach (GiantRock rock in rocks)
        {
            if (IsUpgraded)
            {
                CardCmd.Upgrade(rock, CardPreviewStyle.None);
            }

            await CardCmd.AutoPlay(
                choiceContext,
                rock,
                cardPlay.Target,
                AutoPlayType.Default,
                skipXCapture: false,
                skipCardPileVisuals: !showPileVisuals);
            showPileVisuals = false;
        }
    }
}
