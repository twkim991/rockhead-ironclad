using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Timeline.Epochs;
using MegaCrit.Sts2.Core.Unlocks;
using ThrowRockIronclad.ThrowRockIroncladCode.Cards;

namespace ThrowRockIronclad.ThrowRockIroncladCode.CardPools;

public sealed class RockcladCardPool : CardPoolModel
{
    public override string Title => "ironclad";

    public override string EnergyColorName => "ironclad";

    public override string CardFrameMaterialPath => "card_frame_red";

    public override Color DeckEntryCardColor => new("D62000");

    public override Color EnergyOutlineColor => new("802020");

    public override bool IsColorless => false;

    protected override CardModel[] GenerateAllCards() =>
    [
        ModelDb.Card<RockSlam>(),
        ModelDb.Card<InevitableRock>(),
        ModelDb.Card<RockCharge>(),
        ModelDb.Card<RockArmor>(),
        ModelDb.Card<HiddenRock>(),
        ModelDb.Card<RockFive>(),
        ModelDb.Card<Rockade>(),
        ModelDb.Card<RockForm>(),
        ModelDb.Card<AbsoluteRock>(),
        ModelDb.Card<RockTrap>(),
        ModelDb.Card<AllForRock>(),
        ModelDb.Card<CreativeArtificialRock>(),
        ModelDb.Card<RockCastle>(),
        ModelDb.Card<MoltenFist>(),
        ModelDb.Card<SetupStrike>(),
        ModelDb.Card<Breakthrough>(),
        ModelDb.Card<Cinder>(),
        ModelDb.Card<Bloodletting>(),
        ModelDb.Card<Tremble>(),
        ModelDb.Card<Armaments>(),
        ModelDb.Card<TrueGrit>(),
        ModelDb.Card<ShrugItOff>(),
        ModelDb.Card<BloodWall>(),
        ModelDb.Card<Pillage>(),
        ModelDb.Card<AshenStrike>(),
        ModelDb.Card<Hemokinesis>(),
        ModelDb.Card<Unrelenting>(),
        ModelDb.Card<Uppercut>(),
        ModelDb.Card<HowlFromBeyond>(),
        ModelDb.Card<Stomp>(),
        ModelDb.Card<Rage>(),
        ModelDb.Card<BattleTrance>(),
        ModelDb.Card<Colossus>(),
        ModelDb.Card<SecondWind>(),
        ModelDb.Card<Taunt>(),
        ModelDb.Card<BurningPact>(),
        ModelDb.Card<EvilEye>(),
        ModelDb.Card<DrumOfBattle>(),
        ModelDb.Card<ExpectAFight>(),
        ModelDb.Card<FlameBarrier>(),
        ModelDb.Card<FeelNoPain>(),
        ModelDb.Card<Inflame>(),
        ModelDb.Card<Inferno>(),
        ModelDb.Card<Juggling>(),
        ModelDb.Card<Rupture>(),
        ModelDb.Card<Vicious>(),
        ModelDb.Card<PactsEnd>(),
        ModelDb.Card<Thrash>(),
        ModelDb.Card<Conflagration>(),
        ModelDb.Card<Feed>(),
        ModelDb.Card<TearAsunder>(),
        ModelDb.Card<Headbutt>(),
        ModelDb.Card<Brand>(),
        ModelDb.Card<Cascade>(),
        ModelDb.Card<PrimalForce>(),
        ModelDb.Card<Offering>(),
        ModelDb.Card<OneTwoPunch>(),
        ModelDb.Card<Stoke>(),
        ModelDb.Card<Impervious>(),
        ModelDb.Card<NotYet>(),
        ModelDb.Card<Aggression>(),
        ModelDb.Card<Cruelty>(),
        ModelDb.Card<CrimsonMantle>(),
        ModelDb.Card<Pyre>(),
        ModelDb.Card<DarkEmbrace>(),
        ModelDb.Card<Unmovable>(),
        ModelDb.Card<Corruption>(),
        ModelDb.Card<DemonicShield>(),
        ModelDb.Card<Tank>(),
    ];

    protected override IEnumerable<CardModel> FilterThroughEpochs(
        UnlockState unlockState,
        IEnumerable<CardModel> cards)
    {
        List<CardModel> unlockedCards = cards.ToList();
        unlockedCards.RemoveAll(card => card is RockSlam);

        if (!unlockState.IsEpochRevealed<Ironclad2Epoch>())
        {
            unlockedCards.RemoveAll(card => Ironclad2Epoch.Cards.Any(epochCard => epochCard.Id == card.Id));
        }

        if (!unlockState.IsEpochRevealed<Ironclad5Epoch>())
        {
            unlockedCards.RemoveAll(card => Ironclad5Epoch.Cards.Any(epochCard => epochCard.Id == card.Id));
        }

        if (!unlockState.IsEpochRevealed<Ironclad7Epoch>())
        {
            unlockedCards.RemoveAll(card => Ironclad7Epoch.Cards.Any(epochCard => epochCard.Id == card.Id));
        }

        return unlockedCards;
    }
}
