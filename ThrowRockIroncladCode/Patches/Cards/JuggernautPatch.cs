using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Core;
using ThrowRockIronclad.ThrowRockIroncladCode.Powers;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Patches.Cards;

public static class JuggernautPatch
{
    [HarmonyPatch(typeof(Juggernaut), "get_CanonicalVars")]
    private static class Vars
    {
        [HarmonyPrefix]
        private static bool Replace(ref IEnumerable<DynamicVar> __result)
        {
            __result = [new DynamicVar("ExtraDamage", RockRules.AbsoluteRockDamage)];
            return false;
        }
    }

    [HarmonyPatch(typeof(Juggernaut), "get_ExtraHoverTips")]
    private static class HoverTips
    {
        [HarmonyPrefix]
        private static bool Replace(ref IEnumerable<IHoverTip> __result)
        {
            __result = [HoverTipFactory.FromCard<GiantRock>(false)];
            return false;
        }
    }

    [HarmonyPatch(typeof(Juggernaut), "OnPlay")]
    private static class Play
    {
        [HarmonyPrefix]
        private static bool Replace(
            Juggernaut __instance,
            PlayerChoiceContext choiceContext,
            CardPlay cardPlay,
            ref Task __result)
        {
            __result = PlayReplacement(__instance, choiceContext);
            return false;
        }

        private static async Task PlayReplacement(Juggernaut card, PlayerChoiceContext choiceContext)
        {
            await CreatureCmd.TriggerAnim(card.Owner.Creature, "PowerUp", card.Owner.Character.PowerUpAnimDelay);
            await PowerCmd.Apply<AbsoluteRockPower>(
                choiceContext,
                card.Owner.Creature,
                card.DynamicVars["ExtraDamage"].BaseValue,
                card.Owner.Creature,
                card);
        }
    }

    [HarmonyPatch(typeof(Juggernaut), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Replace(Juggernaut __instance)
        {
            __instance.EnergyCost.UpgradeBy(-1);
            return false;
        }
    }
}
