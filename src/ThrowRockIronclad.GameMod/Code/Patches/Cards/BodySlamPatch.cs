using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using ThrowRockIronclad.ThrowRockIroncladCode.Core;
using ThrowRockIronclad.ThrowRockIroncladCode.Utilities;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Patches.Cards;

public static class BodySlamPatch
{
    [HarmonyPatch(typeof(BodySlam), "get_CanonicalVars")]
    private static class Vars
    {
        [HarmonyPrefix]
        private static bool Replace(ref IEnumerable<DynamicVar> __result)
        {
            __result = [new DamageVar(RockRules.RockSlamDamage, ValueProp.Move)];
            return false;
        }
    }

    [HarmonyPatch(typeof(CardModel), "get_CanonicalKeywords")]
    private static class Keywords
    {
        [HarmonyPostfix]
        private static void AddExhaust(CardModel __instance, ref IEnumerable<CardKeyword> __result)
        {
            if (__instance is BodySlam)
            {
                __result = __result.Append(CardKeyword.Exhaust).Distinct().ToArray();
            }
        }
    }

    [HarmonyPatch(typeof(BodySlam), "get_ExtraHoverTips")]
    private static class HoverTips
    {
        [HarmonyPrefix]
        private static bool Replace(ref IEnumerable<IHoverTip> __result)
        {
            __result = [HoverTipFactory.FromCard<GiantRock>(false)];
            return false;
        }
    }

    [HarmonyPatch(typeof(BodySlam), "OnPlay")]
    private static class Play
    {
        [HarmonyPrefix]
        private static bool Replace(
            BodySlam __instance,
            PlayerChoiceContext choiceContext,
            CardPlay cardPlay,
            ref Task __result)
        {
            __result = PlayReplacement(__instance, choiceContext, cardPlay);
            return false;
        }

        private static async Task PlayReplacement(
            BodySlam card,
            PlayerChoiceContext choiceContext,
            CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue)
                .FromCard(card
#if THROW_ROCK_GAME_0_109
                    , cardPlay
#endif
                )
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_blunt", null, "blunt_attack.mp3")
                .Execute(choiceContext);
            await GiantRockCreation.AddToCombat(card.CombatState!, card.Owner, PileType.Discard, upgraded: false);
        }
    }

    [HarmonyPatch(typeof(BodySlam), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Replace(BodySlam __instance)
        {
            __instance.EnergyCost.UpgradeBy(-1);
            return false;
        }
    }
}
