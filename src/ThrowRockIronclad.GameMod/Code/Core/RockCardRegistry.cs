using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Cards;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Core;

public static class RockCardRegistry
{
    private static readonly HashSet<Type> RockCardTypes =
    [
        typeof(Barricade),
        typeof(DemonForm),
        typeof(StoneArmor),
        typeof(Juggernaut),
        typeof(BodySlam),
        typeof(GiantRock),
        typeof(HiddenRock),
        typeof(InevitableRock),
        typeof(RockFive),
        typeof(RockCharge),
    ];

    public static bool ShouldHaveRockTag(CardModel card) => RockCardTypes.Contains(card.GetType());
}
