using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Compatibility;

public static class PatchCompatibilityDiagnostics
{
    private static readonly Type[] PatchedCardTypes =
    [
        typeof(Barricade),
        typeof(DemonForm),
        typeof(StoneArmor),
        typeof(Juggernaut),
        typeof(BodySlam),
    ];

    public static void ValidateTargetsBeforePatching()
    {
        foreach (Type cardType in PatchedCardTypes)
        {
            Require(AccessTools.DeclaredMethod(cardType, "OnPlay"), $"{cardType.Name}.OnPlay");
            Require(AccessTools.DeclaredMethod(cardType, "OnUpgrade"), $"{cardType.Name}.OnUpgrade");
        }

        Require(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.Tags)), "CardModel.Tags");
        Require(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.TitleLocString)), "CardModel.TitleLocString");
        Require(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.Description)), "CardModel.Description");
        Require(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.PortraitPath)), "CardModel.PortraitPath");
        Require(AccessTools.PropertyGetter(typeof(CardModel), "PortraitPngPath"), "CardModel.PortraitPngPath");
    }

    public static void LogPatchOwners()
    {
        foreach (Type cardType in PatchedCardTypes)
        {
            var method = AccessTools.DeclaredMethod(cardType, "OnPlay");
            HarmonyLib.Patches? patchInfo = Harmony.GetPatchInfo(method);
            string owners = patchInfo is null ? "none" : string.Join(", ", patchInfo.Owners);
            MainFile.Logger.Info($"Patched {cardType.Name}.OnPlay; Harmony owners: {owners}");
        }
    }

    private static void Require(System.Reflection.MethodInfo? method, string displayName)
    {
        if (method is null)
        {
            throw new MissingMethodException(
                $"{displayName} was not found. ThrowRockIronclad supports game version {Core.ModInfo.SupportedGameVersion}.");
        }
    }
}
