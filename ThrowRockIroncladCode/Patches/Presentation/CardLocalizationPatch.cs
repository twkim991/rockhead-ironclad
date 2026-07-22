using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Patches.Presentation;

public static class CardLocalizationPatch
{
    private const string Rockade = "THROW_ROCK_IRONCLAD_CARD_ROCKADE";
    private const string RockForm = "THROW_ROCK_IRONCLAD_CARD_ROCK_FORM";
    private const string RockArmor = "THROW_ROCK_IRONCLAD_CARD_ROCK_ARMOR";
    private const string AbsoluteRock = "THROW_ROCK_IRONCLAD_CARD_ABSOLUTE_ROCK";
    private const string RockSlam = "THROW_ROCK_IRONCLAD_CARD_ROCK_SLAM";

    private static string? GetLocKey(CardModel card) => card switch
    {
        Barricade => Rockade,
        DemonForm => RockForm,
        StoneArmor => RockArmor,
        Juggernaut => AbsoluteRock,
        BodySlam => RockSlam,
        _ => null,
    };

    [HarmonyPatch(typeof(CardModel), "get_TitleLocString")]
    private static class Title
    {
        [HarmonyPostfix]
        private static void Replace(CardModel __instance, ref LocString __result)
        {
            string? key = GetLocKey(__instance);
            if (key is not null)
            {
                __result = new LocString("cards", $"{key}.title");
            }
        }
    }

    [HarmonyPatch(typeof(CardModel), "get_Description")]
    private static class Description
    {
        [HarmonyPostfix]
        private static void Replace(CardModel __instance, ref LocString __result)
        {
            string? key = GetLocKey(__instance);
            if (key is not null)
            {
                __result = new LocString("cards", $"{key}.description");
            }
        }
    }
}
