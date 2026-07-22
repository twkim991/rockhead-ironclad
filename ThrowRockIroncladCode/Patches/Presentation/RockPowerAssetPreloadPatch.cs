using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using ThrowRockIronclad.ThrowRockIroncladCode.Core;
using ThrowRockIronclad.ThrowRockIroncladCode.Powers;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Patches.Presentation;

/// <summary>
/// BaseLib redirects custom power icon paths, but those large icons are not automatically included in the
/// game's run asset set. Attach every Rock power texture to the replaced cards so normal run preloading
/// fills <see cref="MegaCrit.Sts2.Core.Assets.PreloadManager.Cache"/> before a power tooltip requests them.
/// </summary>
[HarmonyPatch(typeof(CardModel), "get_RunAssetPaths")]
public static class RockPowerAssetPreloadPatch
{
    [HarmonyPostfix]
    private static void AddRockPowerIcons(CardModel __instance, ref IEnumerable<string> __result)
    {
        if (!RockCardRegistry.ShouldHaveRockTag(__instance))
        {
            return;
        }

        __result = __result
            .Concat(ThrowRockIroncladPower.GetAllIconPaths())
            .Distinct()
            .ToArray();
    }
}
