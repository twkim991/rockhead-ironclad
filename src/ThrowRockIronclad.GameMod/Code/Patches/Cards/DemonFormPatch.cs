using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Powers;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Patches.Cards;

public static class DemonFormPatch
{
    [HarmonyPatch(typeof(DemonForm), "get_CanonicalVars")]
    private static class Vars
    {
        [HarmonyPrefix]
        private static bool Replace(ref IEnumerable<DynamicVar> __result)
        {
            __result = [];
            return false;
        }
    }

    [HarmonyPatch(typeof(DemonForm), "get_ExtraHoverTips")]
    private static class HoverTips
    {
        [HarmonyPrefix]
        private static bool Replace(DemonForm __instance, ref IEnumerable<IHoverTip> __result)
        {
            __result = [HoverTipFactory.FromCard<GiantRock>(__instance.IsUpgraded)];
            return false;
        }
    }

    [HarmonyPatch(typeof(DemonForm), "OnPlay")]
    private static class Play
    {
        [HarmonyPrefix]
        private static bool Replace(
            DemonForm __instance,
            PlayerChoiceContext choiceContext,
            CardPlay cardPlay,
            ref Task __result)
        {
            __result = PlayReplacement(__instance, choiceContext);
            return false;
        }

        private static async Task PlayReplacement(DemonForm card, PlayerChoiceContext choiceContext)
        {
            await CreatureCmd.TriggerAnim(card.Owner.Creature, "PowerUp", card.Owner.Character.PowerUpAnimDelay);
            await PowerCmd.Apply<RockFormPower>(
                choiceContext,
                card.Owner.Creature,
                RockFormPower.ApplicationAmount(card.IsUpgraded),
                card.Owner.Creature,
                card);
        }
    }

    [HarmonyPatch(typeof(DemonForm), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Replace() => false;
    }
}
