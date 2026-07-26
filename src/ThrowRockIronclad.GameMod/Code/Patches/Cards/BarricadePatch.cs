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
using ThrowRockIronclad.ThrowRockIroncladCode.Powers;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Patches.Cards;

public static class BarricadePatch
{
    [HarmonyPatch(typeof(CardModel), "get_CanonicalVars")]
    private static class Vars
    {
        [HarmonyPostfix]
        private static void Replace(CardModel __instance, ref IEnumerable<DynamicVar> __result)
        {
            if (__instance is Barricade)
            {
                __result = [new BlockVar(RockRules.RockadeBlock, ValueProp.Unpowered)];
            }
        }
    }

    [HarmonyPatch(typeof(Barricade), "get_ExtraHoverTips")]
    private static class HoverTips
    {
        [HarmonyPrefix]
        private static bool Replace(ref IEnumerable<IHoverTip> __result)
        {
            __result = [HoverTipFactory.Static(StaticHoverTip.Block)];
            return false;
        }
    }

    [HarmonyPatch(typeof(Barricade), "OnPlay")]
    private static class Play
    {
        [HarmonyPrefix]
        private static bool Replace(
            Barricade __instance,
            PlayerChoiceContext choiceContext,
            CardPlay cardPlay,
            ref Task __result)
        {
            __result = PlayReplacement(__instance, choiceContext);
            return false;
        }

        private static async Task PlayReplacement(Barricade card, PlayerChoiceContext choiceContext)
        {
            await CreatureCmd.TriggerAnim(card.Owner.Creature, "PowerUp", card.Owner.Character.PowerUpAnimDelay);
            await PowerCmd.Apply<RockadePower>(
                choiceContext,
                card.Owner.Creature,
                card.DynamicVars["Block"].BaseValue,
                card.Owner.Creature,
                card);
        }
    }

    [HarmonyPatch(typeof(Barricade), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Replace(Barricade __instance)
        {
            __instance.DynamicVars["Block"].UpgradeValueBy(RockRules.RockadeBlockUpgrade);
            return false;
        }
    }
}
