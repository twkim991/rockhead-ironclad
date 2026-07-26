using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ThrowRockIronclad.ThrowRockIroncladCode.Cards;
using ThrowRockIronclad.ThrowRockIroncladCode.Compatibility;
using ThrowRockIronclad.ThrowRockIroncladCode.Core;
using ThrowRockIronclad.ThrowRockIroncladCode.Relics;

namespace ThrowRockIronclad.ThrowRockIroncladCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "ThrowRockIronclad"; //Used for resource filepath
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        //Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
        
        string runningVersion = ReleaseInfoManager.Instance.SemVer?.ToString() ?? "unknown";
        Logger.Info($"Initializing {ModId}; supported game version={ModInfo.SupportedGameVersion}, running={runningVersion}");
        if (runningVersion != "unknown" && !ModInfo.SupportsGameVersion(runningVersion))
        {
            Logger.Warn($"Game version {runningVersion} has not been validated. Expected {ModInfo.SupportedGameVersion}.");
        }

        ModHelper.AddModelToPool<IroncladCardPool, HiddenRock>();
        ModHelper.AddModelToPool<IroncladCardPool, InevitableRock>();
        ModHelper.AddModelToPool<IroncladCardPool, RockFive>();
        ModHelper.AddModelToPool<IroncladCardPool, RockCharge>();
        ModHelper.AddModelToPool<IroncladRelicPool, Rock>();

        PatchCompatibilityDiagnostics.ValidateTargetsBeforePatching();
        Harmony harmony = new(ModId);

        harmony.PatchAll();
        PatchCompatibilityDiagnostics.LogPatchOwners();
    }
}
