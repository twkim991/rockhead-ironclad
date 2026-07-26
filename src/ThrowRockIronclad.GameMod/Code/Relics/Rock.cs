using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Utilities;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Relics;

public sealed class Rock : ThrowRockIroncladRelic
{
    public const string IconFile = "rock.png";

    public override string IconFileName => IconFile;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
        => [HoverTipFactory.FromCard<GiantRock>(false)];

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        if (player != Owner || Owner.PlayerCombatState?.TurnNumber is not 1)
        {
            return;
        }

        Flash();
        await GiantRockCreation.AddToCombat(combatState, Owner, PileType.Hand, upgraded: false);
    }
}
