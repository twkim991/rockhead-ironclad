using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Utilities;

public static class GiantRockCreation
{
    public static async Task<CardModel> AddToCombat(
        ICombatState combatState,
        Player owner,
        PileType destination,
        bool upgraded)
    {
        CardModel rock = combatState.CreateCard<GiantRock>(owner);
        if (upgraded)
        {
            CardCmd.Upgrade(rock);
        }

        await CardPileCmd.AddGeneratedCardToCombat(rock, destination, owner);
        return rock;
    }
}
