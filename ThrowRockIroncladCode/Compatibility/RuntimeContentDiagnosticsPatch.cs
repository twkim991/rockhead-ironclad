using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using ThrowRockIronclad.ThrowRockIroncladCode.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Core;
using ThrowRockIronclad.ThrowRockIroncladCode.Patches.Presentation;
using ThrowRockIronclad.ThrowRockIroncladCode.Powers;
using ThrowRockIronclad.ThrowRockIroncladCode.Relics;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Compatibility;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
public static class RuntimeContentDiagnosticsPatch
{
    [HarmonyPostfix]
    private static void ValidateRegisteredContent()
    {
        ThrowRockIroncladPower[] powers =
        [
            ModelDb.Power<RockadePower>(),
            ModelDb.Power<RockFormPower>(),
            ModelDb.Power<RockArmorPower>(),
            ModelDb.Power<AbsoluteRockPower>(),
            ModelDb.Power<RockChargeNextTurnPower>(),
        ];

        foreach (ThrowRockIroncladPower power in powers)
        {
            string actualPowerId = ModelDb.GetEntry(power.GetType());
            string expectedPowerId = RockPowerModelPatch.GetExpectedEntry(power.GetType());
            Require(
                actualPowerId == expectedPowerId,
                $"stable power ID changed for {power.GetType().Name}: actual={actualPowerId}, expected={expectedPowerId}");
            Require(power.Title.Exists(), $"Title localization missing for {power.GetType().Name}");
            Require(power.Description.Exists(), $"Description localization missing for {power.GetType().Name}");
            Require(power.PackedIconPath == power.CustomPackedIconPath, $"small icon path was not replaced for {power.GetType().Name}");
            Require(power.ResolvedBigIconPath == power.CustomBigIconPath, $"large icon path was not replaced for {power.GetType().Name}");
            Require(
                ResourceLoader.Exists(power.CustomPackedIconPath),
                $"small icon missing for {power.GetType().Name}: {power.CustomPackedIconPath}");
            Require(
                ResourceLoader.Exists(power.CustomBigIconPath),
                $"large icon missing for {power.GetType().Name}: {power.CustomBigIconPath}");
        }

        Require(
            powers.Select(power => power.CustomPackedIconPath).Distinct().Count() == powers.Length,
            "each Rock power must have a distinct small icon");
        Require(
            powers.Select(power => power.CustomBigIconPath).Distinct().Count() == powers.Length,
            "each Rock power must have a distinct large icon");

        CardModel[] rockCards =
        [
            ModelDb.Card<Barricade>(),
            ModelDb.Card<DemonForm>(),
            ModelDb.Card<StoneArmor>(),
            ModelDb.Card<Juggernaut>(),
            ModelDb.Card<BodySlam>(),
            ModelDb.Card<GiantRock>(),
            ModelDb.Card<HiddenRock>(),
            ModelDb.Card<InevitableRock>(),
            ModelDb.Card<RockFive>(),
            ModelDb.Card<RockCharge>(),
        ];

        Require(RockTags.RockValue == 1_059_034_496, "stable Rock tag value changed");

        foreach (CardModel card in rockCards)
        {
            Require(card.Tags.Contains(RockTags.Rock), $"Rock tag missing from {card.GetType().Name}");
        }

        Require(!ModelDb.Card<PrimalForce>().Tags.Contains(RockTags.Rock), "PrimalForce must not have the Rock tag");
        Require(ModelDb.Card<BodySlam>().CanonicalKeywords.Contains(CardKeyword.Exhaust), "Rock Slam must have Exhaust");

        ThrowRockIroncladCard[] newCards =
        [
            ModelDb.Card<HiddenRock>(),
            ModelDb.Card<InevitableRock>(),
            ModelDb.Card<RockFive>(),
            ModelDb.Card<RockCharge>(),
        ];

        HashSet<ModelId> ironcladCardIds = ModelDb.CardPool<IroncladCardPool>().AllCardIds.ToHashSet();
        foreach (ThrowRockIroncladCard card in newCards)
        {
            string expectedCardId = RockPowerModelPatch.GetExpectedEntry(card.GetType());
            Require(
                card.Id.Entry == expectedCardId,
                $"stable card ID changed for {card.GetType().Name}: actual={card.Id.Entry}, expected={expectedCardId}");
            Require(ironcladCardIds.Contains(card.Id), $"{card.GetType().Name} is missing from the Ironclad card pool");
        }

        Require(ModelDb.Card<HiddenRock>().Rarity == CardRarity.Uncommon, "HiddenRock must be Uncommon");
        Require(ModelDb.Card<InevitableRock>().Rarity == CardRarity.Common, "InevitableRock must be Common");
        Require(ModelDb.Card<RockFive>().Rarity == CardRarity.Uncommon, "RockFive must be Uncommon");
        Require(ModelDb.Card<RockCharge>().Rarity == CardRarity.Common, "RockCharge must be Common");

        var rockRelic = ModelDb.Relic<Rock>();
        string expectedRelicId = RockPowerModelPatch.GetExpectedEntry(typeof(Rock));
        Require(
            rockRelic.Id.Entry == expectedRelicId,
            $"stable relic ID changed for Rock: actual={rockRelic.Id.Entry}, expected={expectedRelicId}");
        Require(rockRelic.Rarity == RelicRarity.Uncommon, "Rock relic must be Uncommon");
        Require(
            ModelDb.RelicPool<IroncladRelicPool>().AllRelicIds.Contains(rockRelic.Id),
            "Rock relic is missing from the Ironclad relic pool");
        Require(rockRelic.Title.Exists(), "Title localization missing for Rock relic");
        Require(rockRelic.DynamicDescription.Exists(), "Description localization missing for Rock relic");
        Require(rockRelic.Flavor.Exists(), "Flavor localization missing for Rock relic");
        Require(rockRelic.PackedIconPath == rockRelic.CustomPackedIconPath, "Rock relic small icon path changed");
        Require(ResourceLoader.Exists(rockRelic.CustomPackedIconPath), "Rock relic small icon is missing");
        Require(ResourceLoader.Exists(rockRelic.CustomPackedIconOutlinePath), "Rock relic outline icon is missing");
        Require(ResourceLoader.Exists(rockRelic.CustomBigIconPath), "Rock relic large icon is missing");

        CardModel[] portraitCards =
        [
            .. rockCards.Take(5),
            .. newCards,
        ];

        foreach (CardModel card in portraitCards)
        {
            Require(card.TitleLocString.Exists(), $"Title localization missing for {card.GetType().Name}");
            Require(card.Description.Exists(), $"Description localization missing for {card.GetType().Name}");
            string expectedPortraitPath = CardPortraitPatch.GetPortraitPath(card)
                ?? throw new InvalidOperationException($"No custom portrait mapping for {card.GetType().Name}");
            Require(card.PortraitPath == expectedPortraitPath, $"portrait path was not replaced for {card.GetType().Name}");
            Require(ResourceLoader.Exists(expectedPortraitPath), $"portrait missing for {card.GetType().Name}: {expectedPortraitPath}");
        }

        ValidateIsolatedCombatHooks();
        ValidateCardSaveRoundTrips();
        ValidateRelicSaveRoundTrip();

        MainFile.Logger.Info(
            "Runtime content validation passed: 4 original Ironclad cards, 9 custom card portraits, "
            + "5 powers with both icon sizes, 1 Ironclad relic, 10 Rock tags, localization, Exhaust, "
            + "isolated two-player hooks, and card/relic save round-trips.");
    }

    private static void ValidateIsolatedCombatHooks()
    {
        Player playerOne = Player.CreateForNewRun<Ironclad>(UnlockState.all, 10_001UL);
        Player playerTwo = Player.CreateForNewRun<Ironclad>(UnlockState.all, 10_002UL);
        playerOne.ResetCombatState();
        playerTwo.ResetCombatState();

        var combatState = new CombatState();
        combatState.AddPlayer(playerOne);
        combatState.AddPlayer(playerTwo);

        GiantRock ownedRock = combatState.CreateCard<GiantRock>(playerOne);
        GiantRock otherPlayersRock = combatState.CreateCard<GiantRock>(playerTwo);
        StrikeIronclad ordinaryAttack = combatState.CreateCard<StrikeIronclad>(playerOne);
        HiddenRock hiddenRock = combatState.CreateCard<HiddenRock>(playerOne);

        int mixedAmount = RockFormPower.ApplicationAmount(upgraded: false)
            + RockFormPower.ApplicationAmount(upgraded: true);
        var rockForm = (RockFormPower)ModelDb.Power<RockFormPower>().ToMutable();
        rockForm.ApplyInternal(playerOne.Creature, mixedAmount, silent: true);

        Require(rockForm.DisplayAmount == 2, "mixed Rock Form sources must display as 2 stacks");
        Require(
            rockForm.TryModifyEnergyCostInCombat(ownedRock, 2m, out decimal reducedCost)
                && reducedCost == 0m,
            "two Rock Form sources must reduce an owned Rock card by 2 and floor at zero");
        Require(
            !rockForm.TryModifyEnergyCostInCombat(ordinaryAttack, 2m, out decimal ordinaryCost)
                && ordinaryCost == 2m,
            "Rock Form must not reduce non-Rock cards");
        Require(
            !rockForm.TryModifyEnergyCostInCombat(otherPlayersRock, 2m, out decimal otherPlayerCost)
                && otherPlayerCost == 2m,
            "Rock Form must not reduce another player's Rock cards");
        Require(
            rockForm.TryModifyEnergyCostInCombat(hiddenRock, 1m, out decimal hiddenRockCost)
                && hiddenRockCost == 0m,
            "Rock Form must reduce an original Rock card's cost");

        var absoluteRock = (AbsoluteRockPower)ModelDb.Power<AbsoluteRockPower>().ToMutable();
        absoluteRock.ApplyInternal(playerOne.Creature, 12, silent: true);
        Require(
            absoluteRock.ModifyDamageAdditive(null, 0m, ValueProp.Move, playerOne.Creature, ownedRock) == 12m,
            "two Absolute Rock applications must add 12 Giant Rock damage");
        Require(
            absoluteRock.ModifyDamageAdditive(null, 0m, ValueProp.Move, playerOne.Creature, ordinaryAttack) == 0m,
            "Absolute Rock must not increase a non-GiantRock attack");
        Require(
            absoluteRock.ModifyDamageAdditive(null, 0m, ValueProp.Move, playerTwo.Creature, ownedRock) == 0m,
            "Absolute Rock must not increase another player's damage");

        int mixedChargeAmount = RockChargeNextTurnPower.ApplicationAmount(upgraded: false)
            + RockChargeNextTurnPower.ApplicationAmount(upgraded: true);
        var rockCharge = (RockChargeNextTurnPower)ModelDb.Power<RockChargeNextTurnPower>().ToMutable();
        rockCharge.ApplyInternal(playerOne.Creature, mixedChargeAmount, silent: true);
        Require(rockCharge.DisplayAmount == 2, "mixed Rock Charge sources must display as 2 stacks");
        Require(
            RockChargeNextTurnPower.NormalSources(rockCharge.Amount) == 1
                && RockChargeNextTurnPower.UpgradedSources(rockCharge.Amount) == 1,
            "Rock Charge must preserve one normal and one upgraded source");
    }

    private static void ValidateCardSaveRoundTrips()
    {
        CardModel[] mutableCards =
        [
            ModelDb.Card<Barricade>().ToMutable(),
            ModelDb.Card<DemonForm>().ToMutable(),
            ModelDb.Card<StoneArmor>().ToMutable(),
            ModelDb.Card<Juggernaut>().ToMutable(),
            ModelDb.Card<BodySlam>().ToMutable(),
            ModelDb.Card<HiddenRock>().ToMutable(),
            ModelDb.Card<InevitableRock>().ToMutable(),
            ModelDb.Card<RockFive>().ToMutable(),
            ModelDb.Card<RockCharge>().ToMutable(),
        ];

        foreach (CardModel card in mutableCards)
        {
            CardModel restored = CardModel.FromSerializable(card.ToSerializable());
            Require(
                restored.GetType() == card.GetType(),
                $"save round-trip changed {card.GetType().Name} into {restored.GetType().Name}");
        }
    }

    private static void ValidateRelicSaveRoundTrip()
    {
        RelicModel relic = ModelDb.Relic<Rock>().ToMutable();
        RelicModel restored = RelicModel.FromSerializable(relic.ToSerializable());
        Require(restored is Rock, "save round-trip changed the Rock relic type");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"ThrowRockIronclad content validation failed: {message}");
        }
    }
}
