using MegaCrit.Sts2.Core.Entities.Creatures;
#if THROW_ROCK_GAME_0_109
using MegaCrit.Sts2.Core.Entities.Cards;
#endif
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Powers;

public sealed class AbsoluteRockPower : ThrowRockIroncladPower
{
    public override string IconFileName => "absolute_rock_power.png";
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
#if THROW_ROCK_GAME_0_109
        , CardPlay? cardPlay = null
#endif
        )
        => dealer == Owner && cardSource is GiantRock ? Amount : 0m;
}
