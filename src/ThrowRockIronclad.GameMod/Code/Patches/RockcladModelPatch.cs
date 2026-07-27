using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Achievements;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Saves.Managers;
using ThrowRockIronclad.ThrowRockIroncladCode.Characters;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Patches;

public static class RockcladModelPatch
{
    [HarmonyPatch(typeof(ModelDb), "get_AllCharacters")]
    private static class RegisterCharacter
    {
        [HarmonyPostfix]
        private static void AddRockclad(ref IEnumerable<CharacterModel> __result)
        {
            CharacterModel rockclad = ModelDb.Character<Rockclad>();
            if (__result.All(character => character.Id != rockclad.Id))
            {
                __result = __result.Append(rockclad).ToArray();
            }
        }
    }

    [HarmonyPatch]
    private static class ReuseIroncladStringResources
    {
        private static readonly IReadOnlyDictionary<string, string> Values =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["TrailPath"] = SceneHelper.GetScenePath("vfx/card_trail_ironclad"),
                ["VisualsPath"] = SceneHelper.GetScenePath("creature_visuals/ironclad"),
                ["IconTexturePath"] = ImageHelper.GetImagePath("ui/top_panel/character_icon_ironclad.png"),
                ["IconOutlineTexturePath"] = ImageHelper.GetImagePath("ui/top_panel/character_icon_ironclad_outline.png"),
                ["EnergyCounterPath"] = SceneHelper.GetScenePath("combat/energy_counters/ironclad_energy_counter"),
                ["MerchantAnimPath"] = SceneHelper.GetScenePath("merchant/characters/ironclad_merchant"),
                ["RestSiteAnimPath"] = SceneHelper.GetScenePath("rest_site/characters/ironclad_rest_site"),
                ["ArmPointingTexturePath"] = ImageHelper.GetImagePath("ui/hands/multiplayer_hand_ironclad_point.png"),
                ["ArmRockTexturePath"] = ImageHelper.GetImagePath("ui/hands/multiplayer_hand_ironclad_rock.png"),
                ["ArmPaperTexturePath"] = ImageHelper.GetImagePath("ui/hands/multiplayer_hand_ironclad_paper.png"),
                ["ArmScissorsTexturePath"] = ImageHelper.GetImagePath("ui/hands/multiplayer_hand_ironclad_scissors.png"),
                ["CharacterSelectBg"] = SceneHelper.GetScenePath("screens/char_select/char_select_bg_ironclad"),
                ["CharacterSelectTransitionPath"] = "res://materials/transitions/ironclad_transition_mat.tres",
                ["AttackSfx"] = "event:/sfx/characters/ironclad/ironclad_attack",
                ["CastSfx"] = "event:/sfx/characters/ironclad/ironclad_cast",
                ["DeathSfx"] = "event:/sfx/characters/ironclad/ironclad_die",
            };

        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (string propertyName in Values.Keys)
            {
                MethodInfo? getter = AccessTools.PropertyGetter(typeof(CharacterModel), propertyName);
                if (getter is not null)
                {
                    yield return getter;
                }
            }
        }

        [HarmonyPostfix]
        private static void UseIroncladResource(
            CharacterModel __instance,
            MethodBase __originalMethod,
            ref string __result)
        {
            if (__instance is not Rockclad)
            {
                return;
            }

            string propertyName = __originalMethod.Name["get_".Length..];
            __result = Values[propertyName];
        }
    }

    [HarmonyPatch(typeof(CharacterModel), "get_RunWonAchievement")]
    private static class ReuseIroncladWinAchievement
    {
        [HarmonyPrefix]
        private static bool GetIroncladAchievement(CharacterModel __instance, ref Achievement __result)
        {
            if (__instance is not Rockclad)
            {
                return true;
            }

            __result = Achievement.IroncladWin;
            return false;
        }
    }

    [HarmonyPatch(typeof(Ironclad), nameof(Ironclad.GetHeavyAnimIfApplicable))]
    private static class ReuseHeavyAttackAnimation
    {
        [HarmonyPrefix]
        private static bool GetHeavyAnimation(CharacterModel character, ref string __result)
        {
            if (character is not Rockclad)
            {
                return true;
            }

            __result = Ironclad.heavyAttackTrigger;
            return false;
        }
    }

    [HarmonyPatch(typeof(Ironclad), nameof(Ironclad.GetHeavyAttackDelayIfApplicable))]
    private static class ReuseHeavyAttackDelay
    {
        [HarmonyPrefix]
        private static bool GetHeavyDelay(CharacterModel character, ref float __result)
        {
            if (character is not Rockclad)
            {
                return true;
            }

            __result = 0.2f;
            return false;
        }
    }

    /// <summary>
    /// Vanilla character epoch helpers only recognize the five built-in character types and
    /// either look up a non-existent ROCKCLAD epoch or throw for unknown characters.
    /// Rockclad progression is intentionally disabled for this minimal prototype.
    /// </summary>
    [HarmonyPatch]
    private static class SkipVanillaCharacterEpochProgression
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(ProgressSaveManager), "ObtainCharUnlockEpoch")!;
            yield return AccessTools.DeclaredMethod(typeof(ProgressSaveManager), "CheckFifteenElitesDefeatedEpoch")!;
            yield return AccessTools.DeclaredMethod(typeof(ProgressSaveManager), "CheckFifteenBossesDefeatedEpoch")!;
        }

        [HarmonyPrefix]
        private static bool SkipForRockclad(Player localPlayer)
            => localPlayer.Character is not Rockclad;
    }
}
