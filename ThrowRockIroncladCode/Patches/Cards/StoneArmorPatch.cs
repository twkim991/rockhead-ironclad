using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using ThrowRockIronclad.ThrowRockIroncladCode.Core;
using ThrowRockIronclad.ThrowRockIroncladCode.Powers;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Patches.Cards;

public static class StoneArmorPatch
{
    [HarmonyPatch(typeof(StoneArmor), "get_CanonicalVars")]
    private static class Vars
    {
        [HarmonyPrefix]
        private static bool Replace(ref IEnumerable<DynamicVar> __result)
        {
            __result = [new BlockVar(RockRules.RockArmorBlock, ValueProp.Unpowered)];
            return false;
        }
    }

    [HarmonyPatch(typeof(StoneArmor), "get_ExtraHoverTips")]
    private static class HoverTips
    {
        [HarmonyPrefix]
        private static bool Replace(ref IEnumerable<IHoverTip> __result)
        {
            __result =
            [
                HoverTipFactory.Static(StaticHoverTip.Block),
                HoverTipFactory.FromCard<GiantRock>(false),
            ];
            return false;
        }
    }

    [HarmonyPatch(typeof(StoneArmor), "OnPlay")]
    private static class Play
    {
        [HarmonyPrefix]
        private static bool Replace(
            StoneArmor __instance,
            PlayerChoiceContext choiceContext,
            CardPlay cardPlay,
            ref Task __result)
        {
            __result = PowerCmd.Apply<RockArmorPower>(
                choiceContext,
                __instance.Owner.Creature,
                __instance.DynamicVars["Block"].BaseValue,
                __instance.Owner.Creature,
                __instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(StoneArmor), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Replace(StoneArmor __instance)
        {
            __instance.DynamicVars["Block"].UpgradeValueBy(RockRules.RockArmorBlockUpgrade);
            return false;
        }
    }
}
