#if THROW_ROCK_SELF_TEST
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Encounters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using ThrowRockIronclad.ThrowRockIroncladCode.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Core;
using ThrowRockIronclad.ThrowRockIroncladCode.Patches.Presentation;
using ThrowRockIronclad.ThrowRockIroncladCode.Powers;
using ThrowRockIronclad.ThrowRockIroncladCode.Relics;
using ThrowRockIronclad.ThrowRockIroncladCode.Utilities;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Compatibility;

/// <summary>
/// Compile-time-only full-game test harness. Build with THROW_ROCK_SELF_TEST and launch with
/// THROW_ROCK_SELF_TEST=1. Normal Release builds contain none of this code.
/// </summary>
[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
internal static class FullGameIntegrationSelfTest
{
    private const string EnabledEnvironmentVariable = "THROW_ROCK_SELF_TEST";
    private static bool _started;
    private static NGame Game => NGame.Instance
        ?? throw new InvalidOperationException("NGame is not initialized.");

    [HarmonyPostfix]
    private static void StartAfterMainMenuReady()
    {
        if (_started || System.Environment.GetEnvironmentVariable(EnabledEnvironmentVariable) != "1")
        {
            return;
        }

        _started = true;
        TaskHelper.RunSafely(Run());
    }

    private static async Task Run()
    {
        int exitCode = 1;
        Func<bool> previousNonInteractiveCheck = NonInteractiveMode.AutoSlayerCheck;
        try
        {
            NonInteractiveMode.AutoSlayerCheck = static () => true;
            await Game.AwaitProcessFrame();
            await Execute();
            MainFile.Logger.Info("FULL GAME INTEGRATION SELF-TEST PASSED");
            exitCode = 0;
        }
        catch (Exception exception)
        {
            MainFile.Logger.Error($"FULL GAME INTEGRATION SELF-TEST FAILED: {exception}");
        }
        finally
        {
            NonInteractiveMode.AutoSlayerCheck = previousNonInteractiveCheck;
            Game.GetTree().Quit(exitCode);
        }
    }

    private static async Task Execute()
    {
        CharacterModel ironclad = ModelDb.Character<Ironclad>();
        Player playerOne = Player.CreateForNewRun<Ironclad>(UnlockState.all, 1UL);
        Player playerTwo = Player.CreateForNewRun<Ironclad>(UnlockState.all, 2UL);
        List<ActModel> acts = ActModel.GetDefaultList().Select(act => act.ToMutable()).ToList();
        RunState runState = RunState.CreateForNewRun(
            [playerOne, playerTwo],
            acts,
            [],
            GameMode.Standard,
            ascensionLevel: 0,
            seed: "THROWROCK1");

        playerOne.AddRelicInternal(ModelDb.Relic<Rock>().ToMutable(), silent: true);
        AddTestCardsToDeck(runState, playerOne);
        RunManager.Instance.SetUpNewSingleplayer(runState, shouldSave: false);
        ValidateRunSaveRoundTrip(runState, playerOne);

        await PreloadManager.LoadRunAssets([ironclad]);
        var rockRelic = playerOne.GetRelic<Rock>()
            ?? throw new InvalidOperationException("Rock relic was not added to the test player.");
        Require(
            PreloadManager.Cache.ContainsKey(rockRelic.CustomBigIconPath),
            "Rock relic large icon was not preloaded");
        Require(rockRelic.Icon != null, "Rock relic small icon failed to load");
        Require(rockRelic.IconOutline != null, "Rock relic outline icon failed to load");
        Require(rockRelic.BigIcon != null, "Rock relic large icon failed to load");
        RunManager.Instance.Launch();
        Game.RootSceneContainer.SetCurrentScene(NRun.Create(runState));
        await RunManager.Instance.SetActInternal(0);
        RunManager.Instance.RunLocationTargetedBuffer.OnLocationChanged(runState.RunLocation);
        RunManager.Instance.MapSelectionSynchronizer.OnLocationChanged(runState.MapLocation);
        await RunManager.Instance.EnterRoomDebug(
            RoomType.Monster,
            MapPointType.Monster,
            ModelDb.Encounter<CultistsNormal>().ToMutable(),
            showTransition: false);

        await WaitForCombatPlayPhase(playerOne);
        CombatState combatState = CombatManager.Instance.DebugOnlyGetState()
            ?? throw new InvalidOperationException("Combat state was not created.");
        Require(combatState.Players.Count == 2, "fake multiplayer combat must contain two players");
        IReadOnlyList<GiantRock> openingRocks = playerOne.PlayerCombatState!.Hand.Cards
            .OfType<GiantRock>()
            .ToArray();
        Require(openingRocks.Count == 1, "Rock relic must add exactly one Giant Rock to the opening hand");
        Require(!openingRocks[0].IsUpgraded, "Rock relic must add a normal Giant Rock");
        Require(
            !playerTwo.PlayerCombatState!.Hand.Cards.OfType<GiantRock>().Any(),
            "Rock relic must not add a Giant Rock to another player's hand");

        await RemoveAllCombatCards(playerOne);
        await RemoveAllCombatCards(playerTwo);

        Creature target = combatState.Enemies.First();
        target.SetMaxHpInternal(999);
        target.SetCurrentHpInternal(999);
        var choiceContext = new ThrowingPlayerChoiceContext();

        int hpBeforeSlam = target.CurrentHp;
        BodySlam rockSlam = await CreateInHand<BodySlam>(combatState, playerOne, upgraded: false);
        await Play(rockSlam, choiceContext, target);
        Require(target.CurrentHp == hpBeforeSlam - 5, "Rock Slam must deal exactly 5 damage");
        Require(playerOne.PlayerCombatState!.ExhaustPile.Cards.Contains(rockSlam), "Rock Slam must Exhaust");
        Require(
            playerOne.PlayerCombatState.DiscardPile.Cards.Count(card => card is GiantRock && !card.IsUpgraded) == 1,
            "Rock Slam must create one normal Giant Rock in the discard pile");
        Require(
            GiantRockHistory.CountFinishedPlaysThisCombat(playerOne.Creature, combatState) == 0,
            "creating a Giant Rock must not count as playing it");

        await CreateInHand<StrikeIronclad>(combatState, playerOne, upgraded: false);
        PrimalForce primalForce = await CreateInHand<PrimalForce>(combatState, playerOne, upgraded: false);
        await Play(primalForce, choiceContext, target: null);
        GiantRock baselineRock = playerOne.PlayerCombatState.Hand.Cards.OfType<GiantRock>().SingleOrDefault()
            ?? throw new InvalidOperationException("Primal Force did not transform the hand attack into a Giant Rock.");
        Require(baselineRock.Tags.Contains(RockTags.Rock), "the Giant Rock created by Primal Force must have the Rock tag");
        int hpBeforeBaselineRock = target.CurrentHp;
        await Play(baselineRock, choiceContext, target);
        Require(target.CurrentHp == hpBeforeBaselineRock - 16, "a Primal Force Giant Rock without Absolute Rock must deal 16 damage");
        Require(GiantRockHistory.CountFinishedPlaysThisCombat(playerOne.Creature, combatState) == 1, "pre-Power Giant Rock history count must be one");

        // Keep each test section isolated before applying the power cards.
        await RemoveAllCombatCards(playerOne);

        StrikeIronclad hiddenRockFuel = await CreateInHand<StrikeIronclad>(combatState, playerOne, upgraded: false);
        HiddenRock hiddenRock = await CreateInHand<HiddenRock>(combatState, playerOne, upgraded: false);
        await Play(hiddenRock, choiceContext, target: null);
        Require(
            playerOne.PlayerCombatState!.ExhaustPile.Cards.Contains(hiddenRockFuel),
            "Hidden Rock must Exhaust the selected hand card");
        GiantRock hiddenRockResult = playerOne.PlayerCombatState.Hand.Cards.OfType<GiantRock>().Single();
        Require(!hiddenRockResult.IsUpgraded, "normal Hidden Rock must create a normal Giant Rock");
        await RemoveAllCombatCards(playerOne);

        StrikeIronclad upgradedHiddenRockFuel = await CreateInHand<StrikeIronclad>(combatState, playerOne, upgraded: false);
        HiddenRock upgradedHiddenRock = await CreateInHand<HiddenRock>(combatState, playerOne, upgraded: true);
        await Play(upgradedHiddenRock, choiceContext, target: null);
        Require(
            playerOne.PlayerCombatState!.ExhaustPile.Cards.Contains(upgradedHiddenRockFuel),
            "Hidden Rock+ must Exhaust the selected hand card");
        GiantRock upgradedHiddenRockResult = playerOne.PlayerCombatState.Hand.Cards.OfType<GiantRock>().Single();
        Require(upgradedHiddenRockResult.IsUpgraded, "Hidden Rock+ must create Giant Rock+");
        await RemoveAllCombatCards(playerOne);

        int hpBeforeInevitableRock = playerOne.Creature.CurrentHp;
        InevitableRock inevitableRock = await CreateInHand<InevitableRock>(combatState, playerOne, upgraded: false);
        await Play(inevitableRock, choiceContext, target: null);
        Require(
            playerOne.Creature.CurrentHp == hpBeforeInevitableRock - RockRules.InevitableRockHpLoss,
            "Inevitable Rock must lose exactly 2 HP");
        Require(
            playerOne.PlayerCombatState!.Hand.Cards.OfType<GiantRock>().Single().IsUpgraded == false,
            "normal Inevitable Rock must create a normal Giant Rock");
        await RemoveAllCombatCards(playerOne);

        int hpBeforeUpgradedInevitableRock = playerOne.Creature.CurrentHp;
        InevitableRock upgradedInevitableRock = await CreateInHand<InevitableRock>(combatState, playerOne, upgraded: true);
        await Play(upgradedInevitableRock, choiceContext, target: null);
        Require(
            playerOne.Creature.CurrentHp == hpBeforeUpgradedInevitableRock - RockRules.InevitableRockHpLoss,
            "Inevitable Rock+ must still lose exactly 2 HP");
        Require(
            playerOne.PlayerCombatState!.Hand.Cards.OfType<GiantRock>().Single().IsUpgraded,
            "Inevitable Rock+ must create Giant Rock+");
        await RemoveAllCombatCards(playerOne);

        int hpBeforeRockFive = target.CurrentHp;
        RockFive rockFive = await CreateInHand<RockFive>(combatState, playerOne, upgraded: false);
        await Play(rockFive, choiceContext, target: null);
        Require(target.CurrentHp == hpBeforeRockFive - RockRules.RockFiveDamage, "Rock Five must deal exactly 5 damage");
        Require(target.GetPowerAmount<VulnerablePower>() == 2, "Rock Five must apply exactly 2 Vulnerable");
        Require(
            playerOne.PlayerCombatState!.Hand.Cards.OfType<GiantRock>().Single().IsUpgraded == false,
            "normal Rock Five must create a normal Giant Rock");
        await PowerCmd.Remove<VulnerablePower>(target);
        await RemoveAllCombatCards(playerOne);

        int hpBeforeUpgradedRockFive = target.CurrentHp;
        RockFive upgradedRockFive = await CreateInHand<RockFive>(combatState, playerOne, upgraded: true);
        await Play(upgradedRockFive, choiceContext, target: null);
        Require(
            target.CurrentHp == hpBeforeUpgradedRockFive - RockRules.RockFiveDamage,
            "Rock Five+ must still deal exactly 5 damage");
        Require(target.GetPowerAmount<VulnerablePower>() == 2, "Rock Five+ must still apply exactly 2 Vulnerable");
        Require(
            playerOne.PlayerCombatState!.Hand.Cards.OfType<GiantRock>().Single().IsUpgraded,
            "Rock Five+ must create Giant Rock+");
        await PowerCmd.Remove<VulnerablePower>(target);
        await RemoveAllCombatCards(playerOne);

        int blockBeforeRockCharge = playerOne.Creature.Block;
        RockCharge rockChargeCard = await CreateInHand<RockCharge>(combatState, playerOne, upgraded: false);
        RockCharge upgradedRockChargeCard = await CreateInHand<RockCharge>(combatState, playerOne, upgraded: true);
        await Play(rockChargeCard, choiceContext, target: null);
        await Play(upgradedRockChargeCard, choiceContext, target: null);
        Require(
            playerOne.Creature.Block == blockBeforeRockCharge + RockRules.RockChargeBlock * 2,
            "normal and upgraded Rock Charge must each grant 7 Block");
        RockChargeNextTurnPower rockChargePower = RequirePower<RockChargeNextTurnPower>(
            playerOne,
            RockChargeNextTurnPower.ApplicationAmount(false)
                + RockChargeNextTurnPower.ApplicationAmount(true));
        Require(rockChargePower.DisplayAmount == 2, "mixed Rock Charge sources must display as two stacks");
        rockChargePower.AmountOnTurnStart = rockChargePower.Amount;
        int handBeforeRockCharge = playerOne.PlayerCombatState!.Hand.Cards.Count;
        await Hook.AfterSideTurnStart(combatState, CombatSide.Player, [playerOne.Creature, playerTwo.Creature]);
        IReadOnlyList<GiantRock> chargedRocks = playerOne.PlayerCombatState.Hand.Cards
            .Skip(handBeforeRockCharge)
            .OfType<GiantRock>()
            .ToList();
        Require(chargedRocks.Count == 2, "mixed Rock Charge must generate two Giant Rocks next turn");
        Require(chargedRocks.Count(card => card.IsUpgraded) == 1, "mixed Rock Charge must generate one Giant Rock+");
        Require(chargedRocks.Count(card => !card.IsUpgraded) == 1, "mixed Rock Charge must generate one normal Giant Rock");
        Require(
            playerOne.Creature.GetPower<RockChargeNextTurnPower>() is null,
            "Rock Charge next-turn Power must remove itself after triggering");
        await RemoveAllCombatCards(playerOne);

        await PlayPowerCard<StoneArmor>(combatState, playerOne, choiceContext, upgraded: false);
        await PlayPowerCard<StoneArmor>(combatState, playerOne, choiceContext, upgraded: true);
        await PlayPowerCard<Barricade>(combatState, playerOne, choiceContext, upgraded: false);
        await PlayPowerCard<Barricade>(combatState, playerOne, choiceContext, upgraded: true);
        await PlayPowerCard<Juggernaut>(combatState, playerOne, choiceContext, upgraded: false);
        await PlayPowerCard<Juggernaut>(combatState, playerOne, choiceContext, upgraded: true);
        await PlayPowerCard<DemonForm>(combatState, playerOne, choiceContext, upgraded: false);
        await PlayPowerCard<DemonForm>(combatState, playerOne, choiceContext, upgraded: true);
        await RemoveAllCombatCards(playerOne);

        RockArmorPower rockArmor = RequirePower<RockArmorPower>(playerOne, 10);
        RockadePower rockade = RequirePower<RockadePower>(playerOne, 5);
        AbsoluteRockPower absoluteRock = RequirePower<AbsoluteRockPower>(playerOne, 12);
        RockFormPower rockForm = RequirePower<RockFormPower>(playerOne, RockFormPower.ApplicationAmount(false) + RockFormPower.ApplicationAmount(true));
        Require(rockForm.DisplayAmount == 2, "mixed Rock Form sources must display as two stacks");
        Require(playerOne.Creature.Powers.All(power => power is not BarricadePower), "vanilla BarricadePower remained");
        Require(playerOne.Creature.Powers.All(power => power is not DemonFormPower), "vanilla DemonFormPower remained");
        Require(playerOne.Creature.Powers.All(power => power is not PlatingPower), "vanilla PlatingPower remained");
        Require(playerOne.Creature.Powers.All(power => power is not JuggernautPower), "vanilla JuggernautPower remained");

        int hpBeforeOwnedRock = target.CurrentHp;
        int blockBeforeOwnedRock = playerOne.Creature.Block;
        GiantRock ownedRock = await CreateInHand<GiantRock>(combatState, playerOne, upgraded: false);
        await Play(ownedRock, choiceContext, target);
        Require(target.CurrentHp == hpBeforeOwnedRock - 28, "two Absolute Rock stacks must make a normal Giant Rock deal 28 damage");
        Require(playerOne.Creature.Block == blockBeforeOwnedRock + 10, "mixed Rock Armor must grant 10 Block per Giant Rock");
        Require(GiantRockHistory.CountFinishedPlaysThisCombat(playerOne.Creature, combatState) == 2, "owned Giant Rock history count must include the pre-Power play");

        int hpBeforeUpgradedRock = target.CurrentHp;
        int blockBeforeUpgradedRock = playerOne.Creature.Block;
        GiantRock upgradedRock = await CreateInHand<GiantRock>(combatState, playerOne, upgraded: true);
        await Play(upgradedRock, choiceContext, target);
        Require(target.CurrentHp == hpBeforeUpgradedRock - 32, "two Absolute Rock stacks must make Giant Rock+ deal 32 damage");
        Require(playerOne.Creature.Block == blockBeforeUpgradedRock + 10, "mixed Rock Armor must trigger for Giant Rock+");
        Require(GiantRockHistory.CountFinishedPlaysThisCombat(playerOne.Creature, combatState) == 3, "Giant Rock+ must count in Giant Rock history");

        int blockBeforeSecondSlam = playerOne.Creature.Block;
        int hpBeforeSecondSlam = target.CurrentHp;
        BodySlam secondRockSlam = await CreateInHand<BodySlam>(combatState, playerOne, upgraded: true);
        await Play(secondRockSlam, choiceContext, target);
        Require(target.CurrentHp == hpBeforeSecondSlam - 5, "upgraded Rock Slam must still deal exactly 5 damage");
        Require(playerOne.Creature.Block == blockBeforeSecondSlam, "Rock Armor must not trigger for Rock Slam");
        Require(GiantRockHistory.CountFinishedPlaysThisCombat(playerOne.Creature, combatState) == 3, "Rock Slam must not enter Giant Rock history");
        Require(playerOne.PlayerCombatState.ExhaustPile.Cards.Contains(secondRockSlam), "upgraded Rock Slam must Exhaust");

        int blockBeforeRockade = playerOne.Creature.Block;
        await Hook.BeforeTurnEnd(combatState, CombatSide.Player, [playerOne.Creature, playerTwo.Creature]);
        Require(playerOne.Creature.Block == blockBeforeRockade + 15, "mixed Rockade must grant 15 Block for three finished Giant Rock plays, including the pre-Power play");

        GiantRock cumulativeRock = await CreateInHand<GiantRock>(combatState, playerOne, upgraded: false);
        await Play(cumulativeRock, choiceContext, target);
        int blockBeforeSecondRockade = playerOne.Creature.Block;
        await Hook.BeforeTurnEnd(combatState, CombatSide.Player, [playerOne.Creature, playerTwo.Creature]);
        Require(playerOne.Creature.Block == blockBeforeSecondRockade + 20, "Rockade must use the updated whole-combat total on a later turn end");

        int handBeforeRockForm = playerOne.PlayerCombatState.Hand.Cards.Count;
        await Hook.AfterSideTurnStart(combatState, CombatSide.Player, [playerOne.Creature, playerTwo.Creature]);
        IReadOnlyList<CardModel> generatedRocks = playerOne.PlayerCombatState.Hand.Cards
            .Skip(handBeforeRockForm)
            .Where(card => card is GiantRock)
            .ToList();
        Require(generatedRocks.Count == 2, "mixed Rock Form must generate two Giant Rocks");
        Require(generatedRocks.Count(card => card.IsUpgraded) == 1, "mixed Rock Form must generate one Giant Rock+");
        Require(generatedRocks.Count(card => !card.IsUpgraded) == 1, "mixed Rock Form must generate one normal Giant Rock");

        BodySlam discountedRock = await CreateInHand<BodySlam>(combatState, playerOne, upgraded: false);
        BodySlam otherPlayersRock = await CreateInHand<BodySlam>(combatState, playerTwo, upgraded: false);
        Require(
            discountedRock.EnergyCost.GetWithModifiers(CostModifiers.All) == 0,
            "two Rock Form stacks must reduce an owned Rock card to zero");
        Require(
            otherPlayersRock.EnergyCost.GetWithModifiers(CostModifiers.All) == 1,
            "Rock Form must not reduce another player's Rock card");

        int ownerBlockBeforeRemoteRock = playerOne.Creature.Block;
        int hpBeforeRemoteRock = target.CurrentHp;
        GiantRock remoteRock = await CreateInHand<GiantRock>(combatState, playerTwo, upgraded: false);
        await Play(remoteRock, choiceContext, target);
        Require(target.CurrentHp == hpBeforeRemoteRock - 16, "Absolute Rock must not modify another player's Giant Rock");
        Require(playerOne.Creature.Block == ownerBlockBeforeRemoteRock, "Rock Armor must not trigger for another player");
        Require(GiantRockHistory.CountFinishedPlaysThisCombat(playerOne.Creature, combatState) == 4, "Rockade history must exclude another player's Giant Rock");

        await PowerCmd.Apply<RockChargeNextTurnPower>(
            choiceContext,
            playerOne.Creature,
            RockChargeNextTurnPower.ApplicationAmount(false)
                + RockChargeNextTurnPower.ApplicationAmount(true),
            playerOne.Creature,
            null);
        RockChargeNextTurnPower pendingRockCharge = RequirePower<RockChargeNextTurnPower>(
            playerOne,
            RockChargeNextTurnPower.ApplicationAmount(false)
                + RockChargeNextTurnPower.ApplicationAmount(true));

        ValidateNetworkSnapshot(runState, playerOne, rockForm.Amount, pendingRockCharge.Amount);
        await ValidateRenderedUi(combatState, playerOne, rockArmor, rockade, absoluteRock, rockForm, pendingRockCharge);
    }

    private static void AddTestCardsToDeck(RunState runState, Player player)
    {
        player.Deck.AddInternal(runState.CreateCard<Barricade>(player));
        player.Deck.AddInternal(runState.CreateCard<DemonForm>(player));
        player.Deck.AddInternal(runState.CreateCard<StoneArmor>(player));
        player.Deck.AddInternal(runState.CreateCard<Juggernaut>(player));
        player.Deck.AddInternal(runState.CreateCard<BodySlam>(player));
        player.Deck.AddInternal(runState.CreateCard<HiddenRock>(player));
        player.Deck.AddInternal(runState.CreateCard<InevitableRock>(player));
        player.Deck.AddInternal(runState.CreateCard<RockFive>(player));
        player.Deck.AddInternal(runState.CreateCard<RockCharge>(player));
    }

    private static void ValidateRunSaveRoundTrip(RunState runState, Player player)
    {
        var save = RunManager.Instance.ToSave(null);
        RunState restored = RunState.FromSerializable(save);
        Player restoredPlayer = restored.Players.Single(candidate => candidate.NetId == player.NetId);
        Type[] cardTypes =
        [
            typeof(Barricade),
            typeof(DemonForm),
            typeof(StoneArmor),
            typeof(Juggernaut),
            typeof(BodySlam),
            typeof(HiddenRock),
            typeof(InevitableRock),
            typeof(RockFive),
            typeof(RockCharge),
        ];
        foreach (Type cardType in cardTypes)
        {
            Require(
                restoredPlayer.Deck.Cards.Any(card => card.GetType() == cardType),
                $"run save round-trip lost {cardType.Name}");
        }

        Require(restoredPlayer.GetRelic<Rock>() is not null, "run save round-trip lost the Rock relic");
    }

    private static async Task WaitForCombatPlayPhase(Player player)
    {
        for (int frame = 0; frame < 600; frame++)
        {
            if (CombatManager.Instance.IsInProgress
                && player.PlayerCombatState?.Phase == PlayerTurnPhase.Play)
            {
                return;
            }

            await Game.AwaitProcessFrame();
        }

        throw new TimeoutException("Combat did not reach the player Play phase.");
    }

    private static async Task RemoveAllCombatCards(Player player)
    {
        CardModel[] cards = player.PlayerCombatState!.AllPiles.SelectMany(pile => pile.Cards).ToArray();
        if (cards.Length > 0)
        {
            await CardPileCmd.RemoveFromCombat(cards);
        }
    }

    private static async Task PlayPowerCard<T>(
        CombatState combatState,
        Player player,
        PlayerChoiceContext choiceContext,
        bool upgraded)
        where T : CardModel
    {
        T card = await CreateInHand<T>(combatState, player, upgraded);
        await Play(card, choiceContext, target: null);
    }

    private static async Task<T> CreateInHand<T>(CombatState combatState, Player player, bool upgraded)
        where T : CardModel
    {
        T card = combatState.CreateCard<T>(player);
        if (upgraded)
        {
            CardCmd.Upgrade(card);
        }

        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        return card;
    }

    private static Task Play(CardModel card, PlayerChoiceContext choiceContext, Creature? target)
        => card.OnPlayWrapper(
            choiceContext,
            target,
            isAutoPlay: true,
            new ResourceInfo
            {
                EnergySpent = 0,
                EnergyValue = card.EnergyCost.GetWithModifiers(CostModifiers.All),
                StarsSpent = 0,
                StarValue = 0,
            },
            skipCardPileVisuals: false);

    private static T RequirePower<T>(Player player, int expectedAmount) where T : PowerModel
    {
        T power = player.Creature.Powers.OfType<T>().SingleOrDefault()
            ?? throw new InvalidOperationException($"{typeof(T).Name} was not applied.");
        Require(power.Amount == expectedAmount, $"{typeof(T).Name} amount must be {expectedAmount}, got {power.Amount}");
        return power;
    }

    private static void ValidateNetworkSnapshot(
        RunState runState,
        Player player,
        int expectedRockFormAmount,
        int expectedRockChargeAmount)
    {
        NetFullCombatState snapshot = NetFullCombatState.FromRun(runState, justFinishedAction: null);
        var writer = new PacketWriter();
        snapshot.Serialize(writer);
        writer.ZeroByteRemainder();
        byte[] payload = writer.Buffer[..writer.BytePosition];
        var reader = new PacketReader();
        reader.Reset(payload);
        var restored = new NetFullCombatState();
        restored.Deserialize(reader);

        NetFullCombatState.CreatureState playerState = restored.Creatures.Single(state => state.playerId == player.NetId);
        NetFullCombatState.PowerState rockFormState = playerState.powers.Single(state => state.id == ModelDb.Power<RockFormPower>().Id);
        Require(
            rockFormState.amount == expectedRockFormAmount,
            "network packet round-trip must preserve Rock Form's mixed-source Amount");
        NetFullCombatState.PowerState rockChargeState = playerState.powers.Single(
            state => state.id == ModelDb.Power<RockChargeNextTurnPower>().Id);
        Require(
            rockChargeState.amount == expectedRockChargeAmount,
            "network packet round-trip must preserve Rock Charge's mixed-source Amount");
    }

    private static async Task ValidateRenderedUi(
        CombatState combatState,
        Player player,
        params PowerModel[] powers)
    {
        await AwaitFrames(4);
        foreach (PowerModel power in powers)
        {
            Require(power.Icon != null, $"small UI texture is null for {power.GetType().Name}");
            Require(
                PreloadManager.Cache.ContainsKey(power.ResolvedBigIconPath),
                $"large UI texture was not preloaded for {power.GetType().Name}: {power.ResolvedBigIconPath}");
            Require(power.BigIcon != null, $"large UI texture is null for {power.GetType().Name}");
            NPower powerNode = Descendants(NCombatRoom.Instance).OfType<NPower>().FirstOrDefault(node => node.Model == power)
                ?? throw new InvalidOperationException($"combat UI did not create an NPower for {power.GetType().Name}");
            Require(powerNode.GetNode<TextureRect>("%Icon").Texture != null, $"combat UI icon is null for {power.GetType().Name}");
        }

        await RemoveAllCombatCards(player);
        CardModel[] cards =
        [
            await CreateInHand<Barricade>(combatState, player, upgraded: false),
            await CreateInHand<DemonForm>(combatState, player, upgraded: false),
            await CreateInHand<StoneArmor>(combatState, player, upgraded: false),
            await CreateInHand<Juggernaut>(combatState, player, upgraded: false),
            await CreateInHand<BodySlam>(combatState, player, upgraded: false),
            await CreateInHand<HiddenRock>(combatState, player, upgraded: false),
            await CreateInHand<InevitableRock>(combatState, player, upgraded: false),
            await CreateInHand<RockFive>(combatState, player, upgraded: false),
            await CreateInHand<RockCharge>(combatState, player, upgraded: false),
        ];
        await AwaitFrames(4);

        string[] expectedTitles =
        [
            "바위케이드",
            "바위의 형상",
            "바위 갑옷",
            "절대적인 바위",
            "바위 강타",
            "숨겨진 바위",
            "불가피한 바위",
            "바위파이브",
            "바위 충전",
        ];
        var cardNodes = new List<NCard>();
        for (int index = 0; index < cards.Length; index++)
        {
            NCard cardNode = NCard.FindOnTable(cards[index])
                ?? throw new InvalidOperationException($"combat UI did not create an NCard for {cards[index].GetType().Name}");
            cardNodes.Add(cardNode);
            string renderedTitle = cardNode.GetNode<Label>("%TitleLabel").Text;
            string renderedDescription = cardNode.GetNode<RichTextLabel>("%DescriptionLabel").Text;
            Texture2D renderedPortrait = cardNode.GetNode<TextureRect>("%Portrait").Texture
                ?? throw new InvalidOperationException($"rendered portrait is null for {renderedTitle}");
            string expectedPortraitPath = CardPortraitPatch.GetPortraitPath(cards[index])
                ?? throw new InvalidOperationException($"custom portrait mapping is missing for {cards[index].GetType().Name}");
            Require(renderedTitle == expectedTitles[index], $"rendered card title mismatch: {renderedTitle}");
            Require(!string.IsNullOrWhiteSpace(renderedDescription), $"rendered description is empty for {renderedTitle}");
            Require(cards[index].PortraitPath == expectedPortraitPath, $"card portrait path mismatch for {renderedTitle}");
            Require(renderedPortrait.ResourcePath == expectedPortraitPath, $"rendered portrait resource mismatch for {renderedTitle}");
        }

        if (!string.Equals(DisplayServer.GetName(), "headless", StringComparison.OrdinalIgnoreCase))
        {
            string artifactsPath = ProjectSettings.GlobalizePath("res://tests/artifacts");
            DirAccess.MakeDirRecursiveAbsolute(artifactsPath);
            Image image = Game.GetViewport().GetTexture().GetImage()
                ?? throw new InvalidOperationException("viewport did not provide a screenshot image");
            Error saveResult = image.SavePng(Path.Join(artifactsPath, "full-game-integration.png"));
            Require(saveResult == Error.Ok, $"failed to save rendered UI screenshot: {saveResult}");
        }

        // Mirror NUpgradePreview's upgraded clone: render upgraded cards after removing the cost-reduction power,
        // so the UI shows each card's own upgraded numbers and energy cost.
        powers.OfType<RockFormPower>().Single().RemoveInternal();
        await RemoveAllCombatCards(player);
        CardModel[] upgradedCards =
        [
            await CreateInHand<Barricade>(combatState, player, upgraded: true),
            await CreateInHand<DemonForm>(combatState, player, upgraded: true),
            await CreateInHand<StoneArmor>(combatState, player, upgraded: true),
            await CreateInHand<Juggernaut>(combatState, player, upgraded: true),
            await CreateInHand<BodySlam>(combatState, player, upgraded: true),
            await CreateInHand<HiddenRock>(combatState, player, upgraded: true),
            await CreateInHand<InevitableRock>(combatState, player, upgraded: true),
            await CreateInHand<RockFive>(combatState, player, upgraded: true),
            await CreateInHand<RockCharge>(combatState, player, upgraded: true),
        ];
        await AwaitFrames(4);
        cardNodes = upgradedCards.Select(card => NCard.FindOnTable(card)
            ?? throw new InvalidOperationException($"combat UI did not create an upgraded NCard for {card.GetType().Name}"))
            .ToList();
        await AwaitFrames(2);
        foreach (NCard cardNode in cardNodes)
        {
            cardNode.ShowUpgradePreview();
        }
        await AwaitFrames(2);

        string rockadePreview = cardNodes[0].GetNode<RichTextLabel>("%DescriptionLabel").Text;
        string rockFormPreview = cardNodes[1].GetNode<RichTextLabel>("%DescriptionLabel").Text;
        string rockArmorPreview = cardNodes[2].GetNode<RichTextLabel>("%DescriptionLabel").Text;
        string absoluteRockCostPreview = cardNodes[3].GetNode<Label>("%EnergyLabel").Text;
        string rockSlamCostPreview = cardNodes[4].GetNode<Label>("%EnergyLabel").Text;
        string hiddenRockPreview = cardNodes[5].GetNode<RichTextLabel>("%DescriptionLabel").Text;
        string inevitableRockPreview = cardNodes[6].GetNode<RichTextLabel>("%DescriptionLabel").Text;
        string rockFivePreview = cardNodes[7].GetNode<RichTextLabel>("%DescriptionLabel").Text;
        string rockChargePreview = cardNodes[8].GetNode<RichTextLabel>("%DescriptionLabel").Text;
        Require(
            rockadePreview.Contains('3'),
            $"Rockade upgrade preview must render Block coefficient 3; rendered='{rockadePreview}'");
        Require(
            rockFormPreview.Contains("거대한 바위+"),
            $"Rock Form upgrade preview must render Giant Rock+; rendered='{rockFormPreview}'");
        Require(
            rockArmorPreview.Contains('6'),
            $"Rock Armor upgrade preview must render Block 6; rendered='{rockArmorPreview}'");
        Require(absoluteRockCostPreview == "1", $"Absolute Rock upgrade preview must render cost 1; rendered='{absoluteRockCostPreview}'");
        Require(rockSlamCostPreview == "0", $"Rock Slam upgrade preview must render cost 0; rendered='{rockSlamCostPreview}'");
        Require(hiddenRockPreview.Contains("거대한 바위+"), $"Hidden Rock+ preview is wrong: rendered='{hiddenRockPreview}'");
        Require(inevitableRockPreview.Contains("거대한 바위+"), $"Inevitable Rock+ preview is wrong: rendered='{inevitableRockPreview}'");
        Require(rockFivePreview.Contains("거대한 바위+"), $"Rock Five+ preview is wrong: rendered='{rockFivePreview}'");
        Require(rockChargePreview.Contains("거대한 바위+"), $"Rock Charge+ preview is wrong: rendered='{rockChargePreview}'");
    }

    private static async Task AwaitFrames(int count)
    {
        for (int frame = 0; frame < count; frame++)
        {
            await Game.AwaitProcessFrame();
        }
    }

    private static IEnumerable<Node> Descendants(Node? root)
    {
        if (root == null)
        {
            yield break;
        }

        foreach (Node child in root.GetChildren())
        {
            yield return child;
            foreach (Node descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
#endif
