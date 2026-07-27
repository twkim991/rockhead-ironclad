using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.PotionPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using ThrowRockIronclad.ThrowRockIroncladCode.CardPools;
using ThrowRockIronclad.ThrowRockIroncladCode.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.RelicPools;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Characters;

/// <summary>
/// Minimal standalone character prototype that reuses Ironclad presentation
/// while owning a separate nine-card pool.
/// </summary>
public sealed class Rockclad : CharacterModel
{
    public override CharacterGender Gender => CharacterGender.Masculine;

    protected override CharacterModel? UnlocksAfterRunAs => null;

    public override Color NameColor => StsColors.red;

    public override int StartingHp => 80;

    public override int StartingGold => 99;

    public override CardPoolModel CardPool => ModelDb.CardPool<RockcladCardPool>();

    public override PotionPoolModel PotionPool => ModelDb.PotionPool<IroncladPotionPool>();

    public override RelicPoolModel RelicPool => ModelDb.RelicPool<RockcladRelicPool>();

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<StrikeIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<DefendIronclad>(),
        ModelDb.Card<RockSlam>(),
    ];

    public override IReadOnlyList<RelicModel> StartingRelics
        => [ModelDb.Relic<BurningBlood>()];

    public override float AttackAnimDelay => 0.15f;

    public override float CastAnimDelay => 0.25f;

    public override Color EnergyLabelOutlineColor => new("801212FF");

    public override Color DialogueColor => new("590700");

    public override VfxColor SpeechBubbleColor => VfxColor.Red;

    public override Color MapDrawingColor => new("CB282B");

    public override Color RemoteTargetingLineColor => new("E15847FF");

    public override Color RemoteTargetingLineOutline => new("801212FF");

    protected override string IconPath => SceneHelper.GetScenePath("ui/character_icons/ironclad_icon");

    protected override string CharacterSelectIconPath
        => ImageHelper.GetImagePath("packed/character_select/char_select_ironclad.png");

    protected override string CharacterSelectLockedIconPath
        => ImageHelper.GetImagePath("packed/character_select/char_select_ironclad_locked.png");

    protected override string MapMarkerPath
        => ImageHelper.GetImagePath("packed/map/icons/map_marker_ironclad.png");

    public override string CharacterSelectSfx
        => "event:/sfx/characters/ironclad/ironclad_select";

    public override string CharacterTransitionSfx
        => "event:/sfx/ui/wipe_ironclad";

    public override List<string> GetArchitectAttackVfx()
        => ModelDb.Character<Ironclad>().GetArchitectAttackVfx();

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
        => ModelDb.Character<Ironclad>().GenerateAnimator(controller);
}
