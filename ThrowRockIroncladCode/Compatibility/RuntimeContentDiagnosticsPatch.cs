using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using ThrowRockIronclad.ThrowRockIroncladCode.Core;
using ThrowRockIronclad.ThrowRockIroncladCode.Patches.Presentation;
using ThrowRockIronclad.ThrowRockIroncladCode.Powers;

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
        ];

        Require(RockTags.RockValue == 1_059_034_496, "stable Rock tag value changed");

        foreach (CardModel card in rockCards)
        {
            Require(card.Tags.Contains(RockTags.Rock), $"Rock tag missing from {card.GetType().Name}");
        }

        Require(!ModelDb.Card<PrimalForce>().Tags.Contains(RockTags.Rock), "PrimalForce must not have the Rock tag");
        Require(ModelDb.Card<BodySlam>().CanonicalKeywords.Contains(CardKeyword.Exhaust), "Rock Slam must have Exhaust");

        foreach (CardModel card in rockCards.Take(5))
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

        MainFile.Logger.Info(
            "Runtime content validation passed: 5 custom card portraits, 4 powers with both icon sizes, 6 Rock tags, "
            + "localization, Exhaust, isolated two-player hooks, and card save round-trips.");
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
        ];

        foreach (CardModel card in mutableCards)
        {
            CardModel restored = CardModel.FromSerializable(card.ToSerializable());
            Require(
                restored.GetType() == card.GetType(),
                $"save round-trip changed {card.GetType().Name} into {restored.GetType().Name}");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"ThrowRockIronclad content validation failed: {message}");
        }
    }
}
