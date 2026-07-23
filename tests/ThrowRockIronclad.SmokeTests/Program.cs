// See https://aka.ms/new-console-template for more information
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using ThrowRockIronclad.ThrowRockIroncladCode;
using ThrowRockIronclad.ThrowRockIroncladCode.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Core;
using ThrowRockIronclad.ThrowRockIroncladCode.Patches.Presentation;
using ThrowRockIronclad.ThrowRockIroncladCode.Powers;
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

Check(
    !typeof(MainFile).Assembly.GetReferencedAssemblies()
        .Any(assembly => string.Equals(assembly.Name, "BaseLib", StringComparison.OrdinalIgnoreCase)),
    "Production assembly must not reference BaseLib.");
Check(RockTags.RockValue == 1_059_034_496, "The stable Rock tag value changed.");
Check(RockTags.Rock == (CardTag)RockTags.RockValue, "Rock tag mapping is inconsistent.");

CardModel[] rockCards =
[
    new Barricade(),
    new DemonForm(),
    new StoneArmor(),
    new Juggernaut(),
    new BodySlam(),
    new GiantRock(),
    new HiddenRock(),
    new InevitableRock(),
    new RockFive(),
    new RockCharge(),
];

foreach (CardModel card in rockCards)
{
    Check(card.Tags.Contains(RockTags.Rock), $"Rock tag missing: {card.GetType().Name}");
}

Check(!new PrimalForce().Tags.Contains(RockTags.Rock), "PrimalForce must not have the Rock tag.");

var expectedLocKeys = new Dictionary<Type, string>
{
    [typeof(Barricade)] = "THROW_ROCK_IRONCLAD_CARD_ROCKADE",
    [typeof(DemonForm)] = "THROW_ROCK_IRONCLAD_CARD_ROCK_FORM",
    [typeof(StoneArmor)] = "THROW_ROCK_IRONCLAD_CARD_ROCK_ARMOR",
    [typeof(Juggernaut)] = "THROW_ROCK_IRONCLAD_CARD_ABSOLUTE_ROCK",
    [typeof(BodySlam)] = "THROW_ROCK_IRONCLAD_CARD_ROCK_SLAM",
    [typeof(HiddenRock)] = "THROWROCKIRONCLAD-HIDDEN_ROCK",
    [typeof(InevitableRock)] = "THROWROCKIRONCLAD-INEVITABLE_ROCK",
    [typeof(RockFive)] = "THROWROCKIRONCLAD-ROCK_FIVE",
    [typeof(RockCharge)] = "THROWROCKIRONCLAD-ROCK_CHARGE",
};

foreach ((Type type, string key) in expectedLocKeys)
{
    CardModel card = rockCards.Single(card => card.GetType() == type);
    Check(card.TitleLocString.LocEntryKey == $"{key}.title", $"Wrong title key: {type.Name}");
    Check(card.Description.LocEntryKey == $"{key}.description", $"Wrong description key: {type.Name}");
}

var expectedPortraitFiles = new Dictionary<Type, string>
{
    [typeof(Barricade)] = "rockade.png",
    [typeof(DemonForm)] = "rock_form.png",
    [typeof(StoneArmor)] = "rock_armor.png",
    [typeof(Juggernaut)] = "absolute_rock.png",
    [typeof(BodySlam)] = "rock_slam.png",
    [typeof(HiddenRock)] = "hidden_rock.png",
    [typeof(InevitableRock)] = "inevitable_rock.png",
    [typeof(RockFive)] = "rock_five.png",
    [typeof(RockCharge)] = "rock_charge.png",
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
};

ThrowRockIroncladPower[] powerIcons =
[
    new RockadePower(),
    new RockFormPower(),
    new RockArmorPower(),
    new AbsoluteRockPower(),
    new RockChargeNextTurnPower(),
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

var rockade = Upgrade(new Barricade());
Check(BaseCost(rockade) == 3, "Rockade+ must cost 3.");
Check(rockade.DynamicVars["Block"].BaseValue == 3m, "Rockade+ Block must be 3.");

var rockForm = Upgrade(new DemonForm());
Check(BaseCost(rockForm) == 3, "Rock Form+ must cost 3.");
Check(!rockForm.DynamicVars.Any(), "Rock Form must not retain StrengthPower vars.");

var rockArmor = Upgrade(new StoneArmor());
Check(BaseCost(rockArmor) == 1, "Rock Armor+ must cost 1.");
Check(rockArmor.DynamicVars["Block"].BaseValue == 6m, "Rock Armor+ Block must be 6.");

var absoluteRock = Upgrade(new Juggernaut());
Check(BaseCost(absoluteRock) == 1, "Absolute Rock+ must cost 1.");
Check(absoluteRock.DynamicVars["ExtraDamage"].BaseValue == 6m, "Absolute Rock+ damage bonus must stay 6.");

var rockSlam = Upgrade(new BodySlam());
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

foreach (Type originalCardType in new[] { typeof(HiddenRock), typeof(InevitableRock), typeof(RockFive), typeof(RockCharge) })
{
    Check(
        ModelDb.GetEntry(originalCardType) == RockPowerModelPatch.GetExpectedEntry(originalCardType),
        $"Wrong stable card ID: {originalCardType.Name}");
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

Console.WriteLine("All ThrowRockIronclad smoke checks passed.");
