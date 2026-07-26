using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using ThrowRockIronclad.ThrowRockIroncladCode.Core;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Patches;

[HarmonyPatch(typeof(CardModel), "get_Tags")]
public static class RockTagPatch
{
    [HarmonyPostfix]
    private static void AddRockTag(CardModel __instance, ref IEnumerable<CardTag> __result)
    {
        if (!RockCardRegistry.ShouldHaveRockTag(__instance))
        {
            return;
        }

        if (__result is HashSet<CardTag> tags)
        {
            tags.Add(RockTags.Rock);
            return;
        }

        __result = __result.Append(RockTags.Rock).Distinct().ToArray();
    }
}
