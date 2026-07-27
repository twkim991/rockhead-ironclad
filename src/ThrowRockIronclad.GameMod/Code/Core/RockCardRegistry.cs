using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Cards;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Core;

public static class RockCardRegistry
{
    private static readonly HashSet<Type> RockCardTypes =
    [
        typeof(Rockade),
        typeof(RockForm),
        typeof(RockArmor),
        typeof(AbsoluteRock),
        typeof(RockSlam),
        typeof(GiantRock),
        typeof(HiddenRock),
        typeof(InevitableRock),
        typeof(RockFive),
        typeof(RockCharge),
        typeof(RockTrap),
        typeof(AllForRock),
        typeof(CreativeArtificialRock),
        typeof(RockCastle),
    ];

    public static bool ShouldHaveRockTag(CardModel card) => RockCardTypes.Contains(card.GetType());
}
