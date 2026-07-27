using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Compatibility;

public static class PatchCompatibilityDiagnostics
{
    public static void ValidateTargetsBeforePatching()
    {
        Require(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.Tags)), "CardModel.Tags");
        Require(AccessTools.PropertyGetter(typeof(CardModel), nameof(CardModel.PortraitPath)), "CardModel.PortraitPath");
        Require(AccessTools.PropertyGetter(typeof(CardModel), "PortraitPngPath"), "CardModel.PortraitPngPath");
        Require(AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.AllCharacters)), "ModelDb.AllCharacters");
        Require(AccessTools.PropertyGetter(typeof(CharacterModel), "VisualsPath"), "CharacterModel.VisualsPath");
        Require(AccessTools.PropertyGetter(typeof(CharacterModel), nameof(CharacterModel.RunWonAchievement)), "CharacterModel.RunWonAchievement");
        Require(AccessTools.DeclaredMethod(typeof(ProgressSaveManager), "ObtainCharUnlockEpoch"), "ProgressSaveManager.ObtainCharUnlockEpoch");
        Require(
            AccessTools.DeclaredMethod(typeof(ProgressSaveManager), "CheckFifteenElitesDefeatedEpoch"),
            "ProgressSaveManager.CheckFifteenElitesDefeatedEpoch");
        Require(
            AccessTools.DeclaredMethod(typeof(ProgressSaveManager), "CheckFifteenBossesDefeatedEpoch"),
            "ProgressSaveManager.CheckFifteenBossesDefeatedEpoch");
    }

    public static void LogPatchOwners()
    {
        var method = AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.AllCharacters));
        HarmonyLib.Patches? patchInfo = Harmony.GetPatchInfo(method);
        string owners = patchInfo is null ? "none" : string.Join(", ", patchInfo.Owners);
        MainFile.Logger.Info($"Patched ModelDb.AllCharacters; Harmony owners: {owners}");
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
