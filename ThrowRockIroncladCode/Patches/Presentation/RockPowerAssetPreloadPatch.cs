using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using ThrowRockIronclad.ThrowRockIroncladCode.Core;
using ThrowRockIronclad.ThrowRockIroncladCode.Powers;
using ThrowRockIronclad.ThrowRockIroncladCode.Relics;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Patches.Presentation;

/// <summary>
/// Custom power and relic icons are not automatically included in the game's run asset set. Attach them
/// to Rock cards so normal run preloading fills <see cref="MegaCrit.Sts2.Core.Assets.PreloadManager.Cache"/>
/// before a tooltip requests them.
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
            .Concat(GetRelicIconPaths())
            .Distinct()
            .ToArray();
    }

    private static IEnumerable<string> GetRelicIconPaths()
    {
        yield return $"{MainFile.ResPath}/images/relics/{Rock.IconFile}";
        yield return $"{MainFile.ResPath}/images/relics/rock_outline.png";
        yield return $"{MainFile.ResPath}/images/relics/big/{Rock.IconFile}";
    }
}
