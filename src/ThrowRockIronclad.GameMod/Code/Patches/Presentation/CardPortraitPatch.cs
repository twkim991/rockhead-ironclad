using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Extensions;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Patches.Presentation;

/// <summary>
/// Replaces the five reworked vanilla cards' portraits while preserving their original model IDs.
/// </summary>
public static class CardPortraitPatch
{
    public static string? GetPortraitFileName(CardModel card) => card switch
    {
        Rockade => "rockade.png",
        RockForm => "rock_form.png",
        RockArmor => "rock_armor.png",
        AbsoluteRock => "absolute_rock.png",
        RockSlam => "rock_slam.png",
        HiddenRock => "hidden_rock.png",
        InevitableRock => "inevitable_rock.png",
        RockFive => "rock_five.png",
        RockCharge => "rock_charge.png",
        RockTrap => "hidden_rock.png",
        AllForRock => "rock_five.png",
        CreativeArtificialRock => "rock_form.png",
        RockCastle => "rockade.png",
        _ => null,
    };

    public static string? GetPortraitPath(CardModel card)
        => GetPortraitFileName(card)?.CardImagePath();

    [HarmonyPatch(typeof(CardModel), "get_PortraitPath")]
    private static class PortraitPath
    {
        [HarmonyPostfix]
        private static void Replace(CardModel __instance, ref string __result)
        {
            string? customPath = GetPortraitPath(__instance);
            if (customPath is not null)
            {
                __result = customPath;
            }
        }
    }

    // HasPortrait reads this private path rather than PortraitPath. Keep art diagnostics consistent with the UI.
    [HarmonyPatch(typeof(CardModel), "get_PortraitPngPath")]
    private static class PortraitPngPath
    {
        [HarmonyPostfix]
        private static void Replace(CardModel __instance, ref string __result)
        {
            string? customPath = GetPortraitPath(__instance);
            if (customPath is not null)
            {
                __result = customPath;
            }
        }
    }
}
