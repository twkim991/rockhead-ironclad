using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Core;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Powers;

public sealed class CreativeArtificialRockPower : ThrowRockIroncladPower
{
    public override string IconFileName => "rock_form_power.png";
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task BeforeHandDraw(
        Player player,
        PlayerChoiceContext choiceContext,
        ICombatState combatState)
    {
        int cardsToCreate = AmountOnTurnStart;
        if (player != Owner.Player || cardsToCreate < 1)
        {
            return;
        }

        Flash();
        IEnumerable<CardModel> rockCards = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(card => card.Tags.Contains(RockTags.Rock))
            .Concat([ModelDb.Card<GiantRock>()])
            .Distinct();

        IEnumerable<CardModel> generatedCards = CardFactory.GetDistinctForCombat(
            player,
            rockCards,
            cardsToCreate,
            player.RunState.Rng.CombatCardGeneration);
        await CardPileCmd.AddGeneratedCardsToCombat(
            generatedCards,
            PileType.Hand,
            player);
    }
}
