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

        CardPileAddResult addResult = await CardPileCmd.AddGeneratedCardToCombat(rock, destination, owner);
        if (destination != PileType.Hand)
        {
            // Non-hand combat piles update their visible count when the add-preview
            // animation raises CardAddFinished, not when the model enters the pile.
            CardCmd.PreviewCardPileAdd(addResult, 1.2f);
        }

        return rock;
    }
}
