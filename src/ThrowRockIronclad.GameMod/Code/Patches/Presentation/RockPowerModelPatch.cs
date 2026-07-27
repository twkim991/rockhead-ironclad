using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using ThrowRockIronclad.ThrowRockIroncladCode.CardPools;
using ThrowRockIronclad.ThrowRockIroncladCode.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Characters;
using ThrowRockIronclad.ThrowRockIroncladCode.Powers;
using ThrowRockIronclad.ThrowRockIroncladCode.RelicPools;
using ThrowRockIronclad.ThrowRockIroncladCode.Relics;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Patches.Presentation;

/// <summary>
/// Supplies stable namespaced IDs for this mod's original models and custom icon paths for its powers.
/// </summary>
public static class RockPowerModelPatch
{
    public const string IdPrefix = "THROWROCKIRONCLAD-";

    public static string GetExpectedEntry(Type modelType)
        => IdPrefix + StringHelper.Slugify(modelType.Name);

    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.GetEntry))]
    private static class PrefixPowerIdPatch
    {
        [HarmonyPostfix]
        private static void PrefixPowerId(Type type, ref string __result)
        {
            if ((typeof(ThrowRockIroncladPower).IsAssignableFrom(type)
                    || typeof(ThrowRockIroncladCard).IsAssignableFrom(type)
                    || typeof(ThrowRockIroncladRelic).IsAssignableFrom(type)
                    || type == typeof(Rockclad)
                    || type == typeof(RockcladCardPool)
                    || type == typeof(RockcladRelicPool))
                && !__result.StartsWith(IdPrefix, StringComparison.Ordinal))
            {
                __result = IdPrefix + __result;
            }
        }
    }

    [HarmonyPatch(typeof(PowerModel), "get_PackedIconPath")]
    private static class PackedIconPathPatch
    {
        [HarmonyPrefix]
        private static bool UseCustomPackedIcon(PowerModel __instance, ref string __result)
        {
            if (__instance is not ThrowRockIroncladPower rockPower)
            {
                return true;
            }

            __result = rockPower.CustomPackedIconPath;
            return false;
        }
    }

    [HarmonyPatch(typeof(PowerModel), "get_BigIconPath")]
    private static class BigIconPathPatch
    {
        [HarmonyPrefix]
        private static bool UseCustomBigIcon(PowerModel __instance, ref string __result)
        {
            if (__instance is not ThrowRockIroncladPower rockPower)
            {
                return true;
            }

            __result = rockPower.CustomBigIconPath;
            return false;
        }
    }
}
