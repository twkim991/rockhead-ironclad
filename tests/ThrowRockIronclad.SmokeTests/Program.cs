// See https://aka.ms/new-console-template for more information
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using ThrowRockIronclad.ThrowRockIroncladCode;
using ThrowRockIronclad.ThrowRockIroncladCode.CardPools;
using ThrowRockIronclad.ThrowRockIroncladCode.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Characters;
using ThrowRockIronclad.ThrowRockIroncladCode.Core;
using ThrowRockIronclad.ThrowRockIroncladCode.Patches.Presentation;
using ThrowRockIronclad.ThrowRockIroncladCode.Powers;
using ThrowRockIronclad.ThrowRockIroncladCode.RelicPools;
using ThrowRockIronclad.ThrowRockIroncladCode.Relics;

static void Check(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static int BaseCost(CardModel card) => card.EnergyCost.GetWithModifiers(CostModifiers.None);

static T Upgrade<T>(T card) where T : CardModel
{
    MakeMutable(card);
    card.UpgradeInternal();
    card.FinalizeUpgradeInternal();
    return card;
}

static void MakeMutable(AbstractModel model)
{
    typeof(AbstractModel)
        .GetProperty(nameof(AbstractModel.IsMutable), BindingFlags.Instance | BindingFlags.Public)!
        .SetValue(model, true);
}

static (Player Player, Creature Creature) CreateTestPlayer()
{
    var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));
    var creature = new Creature(player, currentHp: 80, maxHp: 80);
    typeof(Player)
        .GetField("<Creature>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
        .SetValue(player, creature);
    return (player, creature);
}

static T BindPower<T>(T power, Creature owner, int amount) where T : PowerModel
{
    MakeMutable(power);
    typeof(PowerModel)
        .GetField("_owner", BindingFlags.Instance | BindingFlags.NonPublic)!
        .SetValue(power, owner);
    typeof(PowerModel)
        .GetField("_amount", BindingFlags.Instance | BindingFlags.NonPublic)!
        .SetValue(power, amount);
    return power;
}

static T OwnCard<T>(T card, Player owner) where T : CardModel
{
    MakeMutable(card);
    card.Owner = owner;
    return card;
}

var harmony = new Harmony("ThrowRockIronclad.SmokeTests");
foreach (Type patchType in typeof(MainFile).Assembly.GetTypes())
{
    try
    {
        harmony.CreateClassProcessor(patchType).Patch();
    }
    catch (Exception exception)
    {
        throw new InvalidOperationException($"Harmony patch failed: {patchType.FullName}", exception);
    }
}

foreach (string methodName in new[]
         {
             "ObtainCharUnlockEpoch",
             "CheckFifteenElitesDefeatedEpoch",
             "CheckFifteenBossesDefeatedEpoch",
         })
{
    MethodInfo method = AccessTools.DeclaredMethod(
        typeof(MegaCrit.Sts2.Core.Saves.Managers.ProgressSaveManager),
        methodName);
    Patches? patchInfo = Harmony.GetPatchInfo(method);
    Check(
        patchInfo?.Prefixes.Any(patch => patch.owner == "ThrowRockIronclad.SmokeTests") == true,
        $"Rockclad epoch guard was not patched: {methodName}");
}

Check(
    !typeof(MainFile).Assembly.GetReferencedAssemblies()
        .Any(assembly => string.Equals(assembly.Name, "BaseLib", StringComparison.OrdinalIgnoreCase)),
    "Production assembly must not reference BaseLib.");
Check(RockTags.RockValue == 1_059_034_496, "The stable Rock tag value changed.");
Check(RockTags.Rock == (CardTag)RockTags.RockValue, "Rock tag mapping is inconsistent.");

CardModel[] rockCards =
[
    new Rockade(),
    new RockForm(),
    new RockArmor(),
    new AbsoluteRock(),
    new RockSlam(),
    new GiantRock(),
    new HiddenRock(),
    new InevitableRock(),
    new RockFive(),
    new RockCharge(),
    new RockTrap(),
    new AllForRock(),
    new CreativeArtificialRock(),
    new RockCastle(),
];

foreach (CardModel card in rockCards)
{
    Check(card.Tags.Contains(RockTags.Rock), $"Rock tag missing: {card.GetType().Name}");
}

foreach (CardModel vanillaCard in new CardModel[]
         {
             new Barricade(),
             new DemonForm(),
             new StoneArmor(),
             new Juggernaut(),
             new BodySlam(),
         })
{
    Check(!vanillaCard.Tags.Contains(RockTags.Rock), $"Vanilla card was modified: {vanillaCard.GetType().Name}");
}

Check(!new PrimalForce().Tags.Contains(RockTags.Rock), "PrimalForce must not have the Rock tag.");

var expectedLocKeys = new Dictionary<Type, string>
{
    [typeof(Rockade)] = "THROWROCKIRONCLAD-ROCKADE",
    [typeof(RockForm)] = "THROWROCKIRONCLAD-ROCK_FORM",
    [typeof(RockArmor)] = "THROWROCKIRONCLAD-ROCK_ARMOR",
    [typeof(AbsoluteRock)] = "THROWROCKIRONCLAD-ABSOLUTE_ROCK",
    [typeof(RockSlam)] = "THROWROCKIRONCLAD-ROCK_SLAM",
    [typeof(HiddenRock)] = "THROWROCKIRONCLAD-HIDDEN_ROCK",
    [typeof(InevitableRock)] = "THROWROCKIRONCLAD-INEVITABLE_ROCK",
    [typeof(RockFive)] = "THROWROCKIRONCLAD-ROCK_FIVE",
    [typeof(RockCharge)] = "THROWROCKIRONCLAD-ROCK_CHARGE",
    [typeof(RockTrap)] = "THROWROCKIRONCLAD-ROCK_TRAP",
    [typeof(AllForRock)] = "THROWROCKIRONCLAD-ALL_FOR_ROCK",
    [typeof(CreativeArtificialRock)] = "THROWROCKIRONCLAD-CREATIVE_ARTIFICIAL_ROCK",
    [typeof(RockCastle)] = "THROWROCKIRONCLAD-ROCK_CASTLE",
};

foreach ((Type type, string key) in expectedLocKeys)
{
    CardModel card = rockCards.Single(card => card.GetType() == type);
    Check(card.TitleLocString.LocEntryKey == $"{key}.title", $"Wrong title key: {type.Name}");
    Check(card.Description.LocEntryKey == $"{key}.description", $"Wrong description key: {type.Name}");
}

var expectedPortraitFiles = new Dictionary<Type, string>
{
    [typeof(Rockade)] = "rockade.png",
    [typeof(RockForm)] = "rock_form.png",
    [typeof(RockArmor)] = "rock_armor.png",
    [typeof(AbsoluteRock)] = "absolute_rock.png",
    [typeof(RockSlam)] = "rock_slam.png",
    [typeof(HiddenRock)] = "hidden_rock.png",
    [typeof(InevitableRock)] = "inevitable_rock.png",
    [typeof(RockFive)] = "rock_five.png",
    [typeof(RockCharge)] = "rock_charge.png",
    [typeof(RockTrap)] = "hidden_rock.png",
    [typeof(AllForRock)] = "rock_five.png",
    [typeof(CreativeArtificialRock)] = "rock_form.png",
    [typeof(RockCastle)] = "rockade.png",
};

foreach ((Type type, string fileName) in expectedPortraitFiles)
{
    CardModel card = rockCards.Single(card => card.GetType() == type);
    Check(CardPortraitPatch.GetPortraitFileName(card) == fileName, $"Wrong portrait mapping: {type.Name}");
}

var expectedPowerIconFiles = new Dictionary<Type, string>
{
    [typeof(RockadePower)] = "rockade_power.png",
    [typeof(RockFormPower)] = "rock_form_power.png",
    [typeof(RockArmorPower)] = "rock_armor_power.png",
    [typeof(AbsoluteRockPower)] = "absolute_rock_power.png",
    [typeof(RockChargeNextTurnPower)] = "rock_charge_power.png",
    [typeof(CreativeArtificialRockPower)] = "rock_form_power.png",
    [typeof(RockCastlePower)] = "rockade_power.png",
};

ThrowRockIroncladPower[] powerIcons =
[
    new RockadePower(),
    new RockFormPower(),
    new RockArmorPower(),
    new AbsoluteRockPower(),
    new RockChargeNextTurnPower(),
    new CreativeArtificialRockPower(),
    new RockCastlePower(),
];

foreach (ThrowRockIroncladPower power in powerIcons)
{
    Check(
        power.IconFileName == expectedPowerIconFiles[power.GetType()],
        $"Wrong power icon mapping: {power.GetType().Name}");
    Check(
        ModelDb.GetEntry(power.GetType()) == RockPowerModelPatch.GetExpectedEntry(power.GetType()),
        $"Wrong stable power ID: {power.GetType().Name}");
}

var rockRelic = new Rock();
Check(rockRelic.Rarity == RelicRarity.Uncommon, "Rock relic must be Uncommon.");
Check(
    ModelDb.GetEntry(typeof(Rock)) == RockPowerModelPatch.GetExpectedEntry(typeof(Rock)),
    "Wrong stable relic ID: Rock.");
Check(rockRelic.IconFileName == "rock.png", "Wrong Rock relic icon file.");
Check(rockRelic.CustomPackedIconPath.EndsWith("/images/relics/rock.png"), "Wrong Rock relic small icon path.");
Check(
    rockRelic.CustomPackedIconOutlinePath.EndsWith("/images/relics/rock_outline.png"),
    "Wrong Rock relic outline icon path.");
Check(rockRelic.CustomBigIconPath.EndsWith("/images/relics/big/rock.png"), "Wrong Rock relic large icon path.");

var rockade = Upgrade(new Rockade());
Check(BaseCost(rockade) == 3, "Rockade+ must cost 3.");
Check(rockade.DynamicVars["Block"].BaseValue == 3m, "Rockade+ Block must be 3.");

var rockForm = Upgrade(new RockForm());
Check(BaseCost(rockForm) == 3, "Rock Form+ must cost 3.");
Check(!rockForm.DynamicVars.Any(), "Rock Form must not retain StrengthPower vars.");

var rockArmor = Upgrade(new RockArmor());
Check(BaseCost(rockArmor) == 1, "Rock Armor+ must cost 1.");
Check(rockArmor.DynamicVars["Block"].BaseValue == 6m, "Rock Armor+ Block must be 6.");

var absoluteRock = Upgrade(new AbsoluteRock());
Check(BaseCost(absoluteRock) == 1, "Absolute Rock+ must cost 1.");
Check(absoluteRock.DynamicVars["ExtraDamage"].BaseValue == 6m, "Absolute Rock+ damage bonus must stay 6.");

var rockSlam = Upgrade(new RockSlam());
Check(BaseCost(rockSlam) == 0, "Rock Slam+ must cost 0.");
Check(rockSlam.DynamicVars.Damage.BaseValue == 5m, "Rock Slam+ damage must stay 5.");
Check(rockSlam.CanonicalKeywords.Contains(CardKeyword.Exhaust), "Rock Slam must have Exhaust.");

var hiddenRock = Upgrade(new HiddenRock());
Check(BaseCost(hiddenRock) == 1, "Hidden Rock+ must cost 1.");
Check(hiddenRock.Rarity == CardRarity.Uncommon, "Hidden Rock must be Uncommon.");

var inevitableRock = Upgrade(new InevitableRock());
Check(BaseCost(inevitableRock) == 1, "Inevitable Rock+ must cost 1.");
Check(inevitableRock.Rarity == CardRarity.Common, "Inevitable Rock must be Common.");
Check(inevitableRock.DynamicVars.HpLoss.BaseValue == 2m, "Inevitable Rock+ must still lose 2 HP.");

var rockFive = Upgrade(new RockFive());
Check(BaseCost(rockFive) == 2, "Rock Five+ must cost 2.");
Check(rockFive.Rarity == CardRarity.Uncommon, "Rock Five must be Uncommon.");
Check(rockFive.DynamicVars.Damage.BaseValue == 5m, "Rock Five+ must still deal 5 damage.");
Check(rockFive.DynamicVars.Vulnerable.BaseValue == 2m, "Rock Five+ must still apply 2 Vulnerable.");

var rockCharge = Upgrade(new RockCharge());
Check(BaseCost(rockCharge) == 1, "Rock Charge+ must cost 1.");
Check(rockCharge.Rarity == CardRarity.Common, "Rock Charge must be Common.");
Check(rockCharge.DynamicVars.Block.BaseValue == 7m, "Rock Charge+ must still grant 7 Block.");
Check(rockCharge.GainsBlock, "Rock Charge must advertise that it gains Block.");

var rockTrap = Upgrade(new RockTrap());
Check(BaseCost(rockTrap) == 2, "Rock Trap+ must cost 2.");
Check(rockTrap.Rarity == CardRarity.Rare, "Rock Trap must be Rare.");

var allForRock = Upgrade(new AllForRock());
Check(BaseCost(allForRock) == 2, "All for Rock+ must cost 2.");
Check(allForRock.Rarity == CardRarity.Rare, "All for Rock must be Rare.");
Check(allForRock.DynamicVars.Damage.BaseValue == 14m, "All for Rock+ must deal 14 damage.");

var creativeArtificialRock = Upgrade(new CreativeArtificialRock());
Check(BaseCost(creativeArtificialRock) == 2, "Creative Artificial Rock+ must cost 2.");
Check(creativeArtificialRock.Rarity == CardRarity.Rare, "Creative Artificial Rock must be Rare.");

var rockCastle = Upgrade(new RockCastle());
Check(BaseCost(rockCastle) == 1, "Rock Castle+ must cost 1.");
Check(rockCastle.Rarity == CardRarity.Rare, "Rock Castle must be Rare.");

foreach (Type rockcladCardType in rockCards
             .OfType<ThrowRockIroncladCard>()
             .Select(card => card.GetType()))
{
    Check(
        ModelDb.GetEntry(rockcladCardType) == RockPowerModelPatch.GetExpectedEntry(rockcladCardType),
        $"Wrong stable card ID: {rockcladCardType.Name}");
}

Check(RockRules.CalculateRockadeBlock(3, 2) == 6, "Rockade calculation failed.");
Check(RockRules.ReduceRockCost(1m, 2) == 0m, "Rock cost must floor at zero.");
int mixedRockChargeAmount = RockChargeNextTurnPower.ApplicationAmount(upgraded: false)
    + RockChargeNextTurnPower.ApplicationAmount(upgraded: true);
Check(RockChargeNextTurnPower.CountSources(mixedRockChargeAmount) == 2, "Mixed Rock Charge count failed.");
Check(RockChargeNextTurnPower.NormalSources(mixedRockChargeAmount) == 1, "Normal Rock Charge source was lost.");
Check(RockChargeNextTurnPower.UpgradedSources(mixedRockChargeAmount) == 1, "Upgraded Rock Charge source was lost.");

var (playerOne, creatureOne) = CreateTestPlayer();
var (playerTwo, creatureTwo) = CreateTestPlayer();
var ownedRock = OwnCard(new GiantRock(), playerOne);
var otherPlayersRock = OwnCard(new GiantRock(), playerTwo);
var ordinaryAttack = OwnCard(new StrikeIronclad(), playerOne);

int mixedRockFormAmount = RockFormPower.ApplicationAmount(upgraded: false)
    + RockFormPower.ApplicationAmount(upgraded: true);
var rockFormPower = BindPower(new RockFormPower(), creatureOne, mixedRockFormAmount);
Check(rockFormPower.DisplayAmount == 2, "Mixed Rock Form sources must display as 2 stacks.");
Check(
    rockFormPower.TryModifyEnergyCostInCombat(ownedRock, 2m, out decimal reducedCost)
        && reducedCost == 0m,
    "Two Rock Form sources must reduce an owned Rock card by 2 and floor at zero.");
Check(
    !rockFormPower.TryModifyEnergyCostInCombat(ordinaryAttack, 2m, out decimal ordinaryCost)
        && ordinaryCost == 2m,
    "Rock Form must not reduce non-Rock cards.");
Check(
    !rockFormPower.TryModifyEnergyCostInCombat(otherPlayersRock, 2m, out decimal otherPlayerCost)
        && otherPlayerCost == 2m,
    "Rock Form must not reduce another player's Rock cards.");

var absoluteRockPower = BindPower(new AbsoluteRockPower(), creatureOne, 12);
Check(
    absoluteRockPower.ModifyDamageAdditive(null, 0m, ValueProp.Move, creatureOne, ownedRock) == 12m,
    "Two Absolute Rock applications must add 12 Giant Rock damage.");
Check(
    absoluteRockPower.ModifyDamageAdditive(null, 0m, ValueProp.Move, creatureOne, ordinaryAttack) == 0m,
    "Absolute Rock must not increase another attack's damage.");
Check(
    absoluteRockPower.ModifyDamageAdditive(null, 0m, ValueProp.Move, creatureTwo, ownedRock) == 0m,
    "Absolute Rock must not increase another player's damage.");

var rockCastlePower = BindPower(new RockCastlePower(), creatureOne, 2);
await rockCastlePower.AfterCardEnteredCombat(ownedRock);
await rockCastlePower.AfterCardEnteredCombat(otherPlayersRock);
Check(ownedRock.BaseReplayCount == 2, "Two Rock Castle stacks must grant Giant Rock Replay 2.");
Check(otherPlayersRock.BaseReplayCount == 0, "Rock Castle must not grant Replay to another player's Giant Rock.");

Type[] expectedRockcladRewardCardTypes =
[
    typeof(InevitableRock),
    typeof(RockCharge),
    typeof(RockArmor),
    typeof(HiddenRock),
    typeof(RockFive),
    typeof(Rockade),
    typeof(RockForm),
    typeof(AbsoluteRock),
    typeof(RockTrap),
    typeof(AllForRock),
    typeof(CreativeArtificialRock),
    typeof(RockCastle),
    typeof(MoltenFist),
    typeof(SetupStrike),
    typeof(Breakthrough),
    typeof(Cinder),
    typeof(Bloodletting),
    typeof(Tremble),
    typeof(Armaments),
    typeof(TrueGrit),
    typeof(ShrugItOff),
    typeof(BloodWall),
    typeof(Pillage),
    typeof(AshenStrike),
    typeof(Hemokinesis),
    typeof(Unrelenting),
    typeof(Uppercut),
    typeof(HowlFromBeyond),
    typeof(Stomp),
    typeof(Rage),
    typeof(BattleTrance),
    typeof(Colossus),
    typeof(SecondWind),
    typeof(Taunt),
    typeof(BurningPact),
    typeof(EvilEye),
    typeof(DrumOfBattle),
    typeof(ExpectAFight),
    typeof(FlameBarrier),
    typeof(FeelNoPain),
    typeof(Inflame),
    typeof(Inferno),
    typeof(Juggling),
    typeof(Rupture),
    typeof(Vicious),
    typeof(PactsEnd),
    typeof(Thrash),
    typeof(Conflagration),
    typeof(Feed),
    typeof(TearAsunder),
    typeof(Headbutt),
    typeof(Brand),
    typeof(Cascade),
    typeof(PrimalForce),
    typeof(Offering),
    typeof(OneTwoPunch),
    typeof(Stoke),
    typeof(Impervious),
    typeof(NotYet),
    typeof(Aggression),
    typeof(Cruelty),
    typeof(CrimsonMantle),
    typeof(Pyre),
    typeof(DarkEmbrace),
    typeof(Unmovable),
    typeof(Corruption),
    typeof(DemonicShield),
    typeof(Tank),
];

Type[] prototypeModelTypes =
[
    typeof(Ironclad),
    typeof(Silent),
    typeof(Regent),
    typeof(Necrobinder),
    typeof(Defect),
    typeof(IroncladPotionPool),
    typeof(BurningBlood),
    typeof(StrikeIronclad),
    typeof(DefendIronclad),
    typeof(RockSlam),
    typeof(Rockclad),
    typeof(RockcladCardPool),
    typeof(RockcladRelicPool),
    typeof(Rock),
    .. expectedRockcladRewardCardTypes,
];

foreach (Type modelType in prototypeModelTypes.Distinct())
{
    ModelDb.Inject(modelType);
}

Rockclad rockcladCharacter = ModelDb.Character<Rockclad>();
Ironclad ironcladCharacter = ModelDb.Character<Ironclad>();
HashSet<ModelId> rockcladCardIds = rockcladCharacter.CardPool.AllCardIds.ToHashSet();
HashSet<Type> actualRockcladPoolCardTypes = rockcladCharacter.CardPool.AllCards
    .Select(card => card.GetType())
    .ToHashSet();
Check(ModelDb.AllCharacters.Contains(rockcladCharacter), "Rockclad must be registered as a playable character.");
Check(rockcladCharacter.CardPool is RockcladCardPool, "Rockclad must use its dedicated card pool.");
Check(
    actualRockcladPoolCardTypes.SetEquals(expectedRockcladRewardCardTypes.Append(typeof(RockSlam)))
        && rockcladCardIds.Count == expectedRockcladRewardCardTypes.Length + 1,
    "Rockclad pool must contain thirteen custom cards and 56 requested Ironclad cards.");
Check(
    AccessTools.DeclaredMethod(typeof(RockcladCardPool), "FilterThroughEpochs") is not null,
    "Rockclad must filter starter-only Rock Slam and preserve Ironclad epoch unlocks.");
CardModel[] rockcladStartingDeck = rockcladCharacter.StartingDeck.ToArray();
Check(
    rockcladStartingDeck.Length == 10
        && rockcladStartingDeck.Count(card => card is StrikeIronclad) == 5
        && rockcladStartingDeck.Count(card => card is DefendIronclad) == 4
        && rockcladStartingDeck.Count(card => card is RockSlam) == 1,
    "Rockclad starting deck must contain five Ironclad Strikes, four Ironclad Defends, and one Rock Slam.");
Check(
    rockcladCharacter.StartingRelics.Count == 1
        && rockcladCharacter.StartingRelics[0] is BurningBlood,
    "Rockclad must start with Burning Blood.");
Check(
    rockcladCharacter.RelicPool.AllRelicIds.SetEquals([ModelDb.Relic<Rock>().Id]),
    "Rockclad relic pool must contain only the Rock relic.");
Check(rockcladCharacter.PotionPool is IroncladPotionPool, "Rockclad must reuse the Ironclad potion pool.");
Check(
    rockcladCharacter.AssetPaths.SequenceEqual(ironcladCharacter.AssetPaths),
    "Rockclad must reuse Ironclad run assets.");
Check(
    rockcladCharacter.AssetPathsCharacterSelect.SequenceEqual(ironcladCharacter.AssetPathsCharacterSelect),
    "Rockclad must reuse Ironclad character-select assets.");
Check(
    rockcladCharacter.RunWonAchievement == Achievement.IroncladWin,
    "Rockclad must reuse the Ironclad win achievement in the prototype.");
Check(ModelDb.Card<Corruption>().Rarity == CardRarity.Ancient, "Corruption must remain an Ancient card.");
Check(
    ModelDb.Card<DemonicShield>().MultiplayerConstraint == CardMultiplayerConstraint.MultiplayerOnly
        && ModelDb.Card<Tank>().MultiplayerConstraint == CardMultiplayerConstraint.MultiplayerOnly,
    "Demonic Shield and Tank must remain multiplayer-only.");

Check(
    !new AscensionManager(4).HasLevel(AscensionLevel.AscendersBane)
        && new AscensionManager(5).HasLevel(AscensionLevel.AscendersBane),
    "Ascender's Bane must activate at Ascension 5.");

Console.WriteLine("All ThrowRockIronclad smoke checks passed.");
