using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
#if THROW_ROCK_GAME_0_109
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
#endif
using MegaCrit.Sts2.Core.Unlocks;
using MegaCrit.Sts2.Core.ValueProps;
using ThrowRockIronclad.ThrowRockIroncladCode.CardPools;
using ThrowRockIronclad.ThrowRockIroncladCode.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Characters;
using ThrowRockIronclad.ThrowRockIroncladCode.Core;
using ThrowRockIronclad.ThrowRockIroncladCode.Patches.Presentation;
using ThrowRockIronclad.ThrowRockIroncladCode.Powers;
using ThrowRockIronclad.ThrowRockIroncladCode.RelicPools;
using ThrowRockIronclad.ThrowRockIroncladCode.Relics;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Compatibility;

#if THROW_ROCK_GAME_0_109
[HarmonyPatch(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))]
#else
[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
#endif
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
            ModelDb.Power<CreativeArtificialRockPower>(),
            ModelDb.Power<RockCastlePower>(),
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
            powers.Take(5).Select(power => power.CustomPackedIconPath).Distinct().Count() == 5,
            "the original five Rock powers must retain distinct small icons");
        Require(
            powers.Take(5).Select(power => power.CustomBigIconPath).Distinct().Count() == 5,
            "the original five Rock powers must retain distinct large icons");

        CardModel[] rockCards =
        [
            ModelDb.Card<Rockade>(),
            ModelDb.Card<RockForm>(),
            ModelDb.Card<RockArmor>(),
            ModelDb.Card<AbsoluteRock>(),
            ModelDb.Card<RockSlam>(),
            ModelDb.Card<GiantRock>(),
            ModelDb.Card<HiddenRock>(),
            ModelDb.Card<InevitableRock>(),
            ModelDb.Card<RockFive>(),
            ModelDb.Card<RockCharge>(),
            ModelDb.Card<RockTrap>(),
            ModelDb.Card<AllForRock>(),
            ModelDb.Card<CreativeArtificialRock>(),
            ModelDb.Card<RockCastle>(),
        ];

        Require(RockTags.RockValue == 1_059_034_496, "stable Rock tag value changed");

        foreach (CardModel card in rockCards)
        {
            Require(card.Tags.Contains(RockTags.Rock), $"Rock tag missing from {card.GetType().Name}");
        }

        CardModel[] vanillaCards =
        [
            ModelDb.Card<Barricade>(),
            ModelDb.Card<DemonForm>(),
            ModelDb.Card<StoneArmor>(),
            ModelDb.Card<Juggernaut>(),
            ModelDb.Card<BodySlam>(),
        ];

        foreach (CardModel card in vanillaCards)
        {
            Require(!card.Tags.Contains(RockTags.Rock), $"Vanilla card was modified: {card.GetType().Name}");
        }

        Require(!ModelDb.Card<PrimalForce>().Tags.Contains(RockTags.Rock), "PrimalForce must not have the Rock tag");
        Require(ModelDb.Card<RockSlam>().CanonicalKeywords.Contains(CardKeyword.Exhaust), "Rock Slam must have Exhaust");
        Require(!ModelDb.Card<BodySlam>().CanonicalKeywords.Contains(CardKeyword.Exhaust), "Body Slam must remain vanilla");

        ThrowRockIroncladCard[] rockcladCards =
        [
            ModelDb.Card<Rockade>(),
            ModelDb.Card<RockForm>(),
            ModelDb.Card<RockArmor>(),
            ModelDb.Card<AbsoluteRock>(),
            ModelDb.Card<RockSlam>(),
            ModelDb.Card<HiddenRock>(),
            ModelDb.Card<InevitableRock>(),
            ModelDb.Card<RockFive>(),
            ModelDb.Card<RockCharge>(),
            ModelDb.Card<RockTrap>(),
            ModelDb.Card<AllForRock>(),
            ModelDb.Card<CreativeArtificialRock>(),
            ModelDb.Card<RockCastle>(),
        ];

        RockcladCardPool rockcladCardPool = ModelDb.CardPool<RockcladCardPool>();
        HashSet<ModelId> rockcladCardIds = rockcladCardPool.AllCardIds.ToHashSet();
        HashSet<ModelId> rockcladRewardCardIds = rockcladCardPool
            .GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.None)
            .Select(card => card.Id)
            .ToHashSet();
        HashSet<ModelId> ironcladCardIds = ModelDb.CardPool<IroncladCardPool>().AllCardIds.ToHashSet();
        foreach (ThrowRockIroncladCard card in rockcladCards)
        {
            string expectedCardId = RockPowerModelPatch.GetExpectedEntry(card.GetType());
            Require(
                card.Id.Entry == expectedCardId,
                $"stable card ID changed for {card.GetType().Name}: actual={card.Id.Entry}, expected={expectedCardId}");
            Require(!ironcladCardIds.Contains(card.Id), $"{card.GetType().Name} leaked into the Ironclad card pool");
            Require(rockcladCardIds.Contains(card.Id), $"{card.GetType().Name} is missing from the Rockclad card pool");

            if (card is RockSlam)
            {
                Require(
                    !rockcladRewardCardIds.Contains(card.Id),
                    "Rock Slam must be starter-only and absent from the Rockclad reward pool");
            }
            else
            {
                Require(
                    rockcladRewardCardIds.Contains(card.Id),
                    $"{card.GetType().Name} is missing from the Rockclad reward pool");
            }
        }

        Require(rockcladCards.Length == 13, "Rockclad must have exactly thirteen custom cards");
        Require(
            rockcladCardIds.Count == 69 && rockcladRewardCardIds.Count == 68,
            "Rockclad reward pool must contain twelve custom cards and 56 requested Ironclad cards");
        Require(ModelDb.Card<RockSlam>().Rarity == CardRarity.Common, "RockSlam must be Common");
        Require(ModelDb.Card<RockArmor>().Rarity == CardRarity.Uncommon, "RockArmor must be Uncommon");
        Require(ModelDb.Card<Rockade>().Rarity == CardRarity.Rare, "Rockade must be Rare");
        Require(ModelDb.Card<HiddenRock>().Rarity == CardRarity.Uncommon, "HiddenRock must be Uncommon");
        Require(ModelDb.Card<InevitableRock>().Rarity == CardRarity.Common, "InevitableRock must be Common");
        Require(ModelDb.Card<RockFive>().Rarity == CardRarity.Uncommon, "RockFive must be Uncommon");
        Require(ModelDb.Card<RockCharge>().Rarity == CardRarity.Common, "RockCharge must be Common");
        Require(ModelDb.Card<RockTrap>().Rarity == CardRarity.Rare, "RockTrap must be Rare");
        Require(ModelDb.Card<AllForRock>().Rarity == CardRarity.Rare, "AllForRock must be Rare");
        Require(
            ModelDb.Card<CreativeArtificialRock>().Rarity == CardRarity.Rare,
            "CreativeArtificialRock must be Rare");
        Require(ModelDb.Card<RockCastle>().Rarity == CardRarity.Rare, "RockCastle must be Rare");
        Require(ModelDb.Card<Corruption>().Rarity == CardRarity.Ancient, "Corruption must remain Ancient");
        Require(
            ModelDb.Card<DemonicShield>().MultiplayerConstraint == CardMultiplayerConstraint.MultiplayerOnly
                && ModelDb.Card<Tank>().MultiplayerConstraint == CardMultiplayerConstraint.MultiplayerOnly,
            "Demonic Shield and Tank must remain multiplayer-only");

        HashSet<ModelId> singleplayerCardIds = ModelDb.CardPool<RockcladCardPool>()
            .GetUnlockedCards(UnlockState.all, CardMultiplayerConstraint.SingleplayerOnly)
            .Select(card => card.Id)
            .ToHashSet();
        Require(
            !singleplayerCardIds.Contains(ModelDb.Card<DemonicShield>().Id)
                && !singleplayerCardIds.Contains(ModelDb.Card<Tank>().Id),
            "multiplayer-only cards leaked into Rockclad's singleplayer pool");

        Rockclad rockclad = ModelDb.Character<Rockclad>();
        Ironclad ironclad = ModelDb.Character<Ironclad>();
        Require(
            rockclad.Id.Entry == RockPowerModelPatch.GetExpectedEntry(typeof(Rockclad)),
            $"stable character ID changed: {rockclad.Id.Entry}");
        Require(ModelDb.AllCharacters.Contains(rockclad), "Rockclad is missing from ModelDb.AllCharacters");
        Require(rockclad.CardPool is RockcladCardPool, "Rockclad has the wrong card pool");
        Require(rockclad.RelicPool is RockcladRelicPool, "Rockclad has the wrong relic pool");
        Require(rockclad.PotionPool is IroncladPotionPool, "Rockclad must reuse the Ironclad potion pool");
        CardModel[] rockcladStartingDeck = rockclad.StartingDeck.ToArray();
        Require(
            rockcladStartingDeck.Length == 10
                && rockcladStartingDeck.Count(card => card is StrikeIronclad) == 5
                && rockcladStartingDeck.Count(card => card is DefendIronclad) == 4
                && rockcladStartingDeck.Count(card => card is RockSlam) == 1,
            "Rockclad must start with five Ironclad Strikes, four Ironclad Defends, and one Rock Slam");
        Require(
            rockclad.StartingRelics.Count == 1 && rockclad.StartingRelics[0] is BurningBlood,
            "Rockclad must start with Burning Blood");
        Require(rockclad.Title.Exists(), "Rockclad title localization is missing");
        Require(rockclad.AssetPaths.SequenceEqual(ironclad.AssetPaths), "Rockclad run assets must reuse Ironclad assets");
        Require(
            rockclad.AssetPathsCharacterSelect.SequenceEqual(ironclad.AssetPathsCharacterSelect),
            "Rockclad character-select assets must reuse Ironclad assets");
        Require(
            rockclad.RunWonAchievement == Achievement.IroncladWin,
            "Rockclad must reuse the Ironclad win achievement in the prototype");

        var rockRelic = ModelDb.Relic<Rock>();
        string expectedRelicId = RockPowerModelPatch.GetExpectedEntry(typeof(Rock));
        Require(
            rockRelic.Id.Entry == expectedRelicId,
            $"stable relic ID changed for Rock: actual={rockRelic.Id.Entry}, expected={expectedRelicId}");
        Require(rockRelic.Rarity == RelicRarity.Uncommon, "Rock relic must be Uncommon");
        Require(
            ModelDb.RelicPool<RockcladRelicPool>().AllRelicIds.Contains(rockRelic.Id),
            "Rock relic is missing from the Rockclad relic pool");
        Require(
            !ModelDb.RelicPool<IroncladRelicPool>().AllRelicIds.Contains(rockRelic.Id),
            "Rock relic leaked into the Ironclad relic pool");
        Require(rockRelic.Title.Exists(), "Title localization missing for Rock relic");
        Require(rockRelic.DynamicDescription.Exists(), "Description localization missing for Rock relic");
        Require(rockRelic.Flavor.Exists(), "Flavor localization missing for Rock relic");
        Require(rockRelic.PackedIconPath == rockRelic.CustomPackedIconPath, "Rock relic small icon path changed");
        Require(ResourceLoader.Exists(rockRelic.CustomPackedIconPath), "Rock relic small icon is missing");
        Require(ResourceLoader.Exists(rockRelic.CustomPackedIconOutlinePath), "Rock relic outline icon is missing");
        Require(ResourceLoader.Exists(rockRelic.CustomBigIconPath), "Rock relic large icon is missing");

        CardModel[] portraitCards =
        [
            .. rockcladCards,
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
        ValidateAscensionDeckRules();
        ValidateCardSaveRoundTrips();
        ValidateRelicSaveRoundTrip();

        MainFile.Logger.Info(
            "Runtime content validation passed: standalone Rockclad, 68-card reward pool, starter-only Rock Slam, 5 Strikes + 4 Defends + Rock Slam start, Burning Blood start, "
            + "1 Rockclad relic, Ironclad presentation reuse, 7 powers with both icon sizes, 14 Rock tags, "
            + "localization, isolated two-player hooks, and card/relic save round-trips.");
    }

    private static void ValidateAscensionDeckRules()
    {
        Player ascensionFourPlayer = Player.CreateForNewRun<Rockclad>(UnlockState.all, 40_004UL);
        _ = RunState.CreateForTest(
            players: [ascensionFourPlayer],
            ascensionLevel: 4,
            seed: "ROCKCLAD-A4");
        new AscensionManager(4).ApplyEffectsTo(ascensionFourPlayer);
        Require(
            ascensionFourPlayer.Deck.Cards.Count(card => card is AscendersBane) == 0,
            "Rockclad received Ascender's Bane below Ascension 5");

        Player ascensionFivePlayer = Player.CreateForNewRun<Rockclad>(UnlockState.all, 50_005UL);
        _ = RunState.CreateForTest(
            players: [ascensionFivePlayer],
            ascensionLevel: 5,
            seed: "ROCKCLAD-A5");
        new AscensionManager(5).ApplyEffectsTo(ascensionFivePlayer);
        Require(
            ascensionFivePlayer.Deck.Cards.Count(card => card is AscendersBane) == 1
                && ascensionFivePlayer.Deck.Cards.Count == 11,
            "Rockclad did not receive exactly one Ascender's Bane at Ascension 5");
    }

    private static void ValidateIsolatedCombatHooks()
    {
        Player playerOne = Player.CreateForNewRun<Rockclad>(UnlockState.all, 10_001UL);
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
            ModelDb.Card<Rockade>().ToMutable(),
            ModelDb.Card<RockForm>().ToMutable(),
            ModelDb.Card<RockArmor>().ToMutable(),
            ModelDb.Card<AbsoluteRock>().ToMutable(),
            ModelDb.Card<RockSlam>().ToMutable(),
            ModelDb.Card<HiddenRock>().ToMutable(),
            ModelDb.Card<InevitableRock>().ToMutable(),
            ModelDb.Card<RockFive>().ToMutable(),
            ModelDb.Card<RockCharge>().ToMutable(),
            ModelDb.Card<RockTrap>().ToMutable(),
            ModelDb.Card<AllForRock>().ToMutable(),
            ModelDb.Card<CreativeArtificialRock>().ToMutable(),
            ModelDb.Card<RockCastle>().ToMutable(),
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
