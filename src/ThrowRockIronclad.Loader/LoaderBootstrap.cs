using System.Collections;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace ThrowRockIronclad.Loader;

[ModInitializer(nameof(Initialize))]
public static class LoaderBootstrap
{
    private const string ModId = "ThrowRockIronclad";
    private const string VariantAssemblyName = "ThrowRockIronclad";
    private const string CompatibilityMetadataKey = "ThrowRockCompatibilityTarget";
    private static readonly List<Assembly> VariantAssemblies = [];
    private static bool _reflectionBridgeInstalled;
    private static bool _associationCallbackInstalled;
    private static Assembly? _selectedVariantAssembly;

    public static void Initialize()
    {
        string loaderDirectory = Path.GetDirectoryName(typeof(LoaderBootstrap).Assembly.Location)
            ?? throw new InvalidOperationException("Could not resolve the ThrowRockIronclad loader directory.");
        Version hostVersion = ResolveHostVersion();
        VariantCandidate variant = PickVariant(loaderDirectory, hostVersion);

        Log.Info(
            $"[{ModId}.Loader] Host version={hostVersion}; selected compatibility target={variant.Version}.");

        AssemblyLoadContext context =
            AssemblyLoadContext.GetLoadContext(typeof(LoaderBootstrap).Assembly)
            ?? AssemblyLoadContext.Default;
        Assembly assembly = context.LoadFromAssemblyPath(variant.AssemblyPath);

        ValidateVariantAssembly(assembly, variant.Version);
        RegisterVariantAssembly(assembly);
        AssociateVariantAssembly(assembly);
        InvokeVariantInitializer(assembly);
    }

    private static Version ResolveHostVersion()
    {
        string? gameAssemblyDirectory = Path.GetDirectoryName(typeof(ModManager).Assembly.Location);
        string? gameDirectory = gameAssemblyDirectory is null
            ? null
            : Directory.GetParent(gameAssemblyDirectory)?.FullName;
        string? releaseInfoPath = gameDirectory is null
            ? null
            : Path.Combine(gameDirectory, "release_info.json");

        if (releaseInfoPath is null || !File.Exists(releaseInfoPath))
        {
            throw new FileNotFoundException(
                "Could not locate release_info.json beside the game data directory.",
                releaseInfoPath);
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(releaseInfoPath));
        string label = document.RootElement.GetProperty("version").GetString()
            ?? throw new InvalidDataException("release_info.json has no version string.");
        if (!TryParseVersion(label, out Version version))
        {
            throw new InvalidDataException($"Unsupported game version label '{label}'.");
        }

        return version;
    }

    private static VariantCandidate PickVariant(string loaderDirectory, Version hostVersion)
    {
        string libDirectory = Path.Combine(loaderDirectory, "lib");
        if (!Directory.Exists(libDirectory))
        {
            throw new DirectoryNotFoundException($"Compatibility library directory is missing: {libDirectory}");
        }

        List<VariantCandidate> candidates = [];
        foreach (string directory in Directory.EnumerateDirectories(libDirectory))
        {
            string target = Path.GetFileName(directory);
            string markerPath = Path.Combine(directory, "compat-target.txt");
            string assemblyPath = Path.Combine(directory, $"{VariantAssemblyName}.dll");
            if (!TryParseVersion(target, out Version version)
                || !File.Exists(markerPath)
                || !string.Equals(File.ReadAllText(markerPath).Trim(), target, StringComparison.Ordinal)
                || !File.Exists(assemblyPath))
            {
                continue;
            }

            candidates.Add(new VariantCandidate(version, Path.GetFullPath(assemblyPath)));
        }

        VariantCandidate? selected = candidates
            .Where(candidate =>
                candidate.Version.Major == hostVersion.Major
                && candidate.Version.Minor == hostVersion.Minor
                && candidate.Version <= hostVersion)
            .OrderBy(candidate => candidate.Version)
            .LastOrDefault();
        return selected
            ?? throw new NotSupportedException(
                $"No {ModId} compatibility variant supports game version {hostVersion}.");
    }

    private static void ValidateVariantAssembly(Assembly assembly, Version target)
    {
        if (!string.Equals(assembly.GetName().Name, VariantAssemblyName, StringComparison.Ordinal))
        {
            throw new BadImageFormatException(
                $"Compatibility assembly identity is '{assembly.GetName().Name}', expected '{VariantAssemblyName}'.");
        }

        string? metadataTarget = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute =>
                string.Equals(attribute.Key, CompatibilityMetadataKey, StringComparison.Ordinal))
            ?.Value;
        if (!string.Equals(metadataTarget, target.ToString(3), StringComparison.Ordinal))
        {
            throw new BadImageFormatException(
                $"Compatibility metadata is '{metadataTarget ?? "<missing>"}', expected '{target.ToString(3)}'.");
        }
    }

    private static void RegisterVariantAssembly(Assembly assembly)
    {
        VariantAssemblies.Add(assembly);
        if (_reflectionBridgeInstalled)
        {
            return;
        }

        MethodInfo getter = AccessTools.PropertyGetter(typeof(ReflectionHelper), nameof(ReflectionHelper.ModTypes))
            ?? throw new MissingMethodException(typeof(ReflectionHelper).FullName, "get_ModTypes");
        new Harmony($"{ModId}.Loader.ReflectionBridge").Patch(
            getter,
            postfix: new HarmonyMethod(
                typeof(LoaderBootstrap),
                nameof(AddVariantTypesToReflectionHelper)));
        _reflectionBridgeInstalled = true;
    }

    private static void AddVariantTypesToReflectionHelper(ref Type[] __result)
    {
        __result = __result
            .Concat(VariantAssemblies.SelectMany(GetLoadableTypes))
            .Distinct()
            .ToArray();
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            Log.Warn($"[{ModId}.Loader] Some compatibility types could not load: {exception.Message}");
            return exception.Types.OfType<Type>();
        }
    }

    private static void AssociateVariantAssembly(Assembly assembly)
    {
        _selectedVariantAssembly = assembly;
        MethodInfo? associateMethod = typeof(ModManager).GetMethod(
            "AssociateAssemblyWithMod",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            [typeof(string), typeof(Assembly)],
            modifiers: null);
        associateMethod?.Invoke(null, [ModId, assembly]);

        if (!_associationCallbackInstalled)
        {
            ModManager.OnModDetected += OnModDetected;
            _associationCallbackInstalled = true;
        }
    }

    private static void OnModDetected(Mod mod)
    {
        if (_selectedVariantAssembly is null
            || !string.Equals(mod.manifest?.id, ModId, StringComparison.Ordinal))
        {
            return;
        }

        FieldInfo? assembliesField = typeof(Mod).GetField(
            "assemblies",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (assembliesField?.GetValue(mod) is IList assemblies
            && !assemblies.Contains(_selectedVariantAssembly))
        {
            assemblies.Add(_selectedVariantAssembly);
        }

        FieldInfo? legacyAssemblyField = typeof(Mod).GetField(
            "assembly",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        legacyAssemblyField?.SetValue(mod, _selectedVariantAssembly);

        ModManager.OnModDetected -= OnModDetected;
        _associationCallbackInstalled = false;
    }

    private static void InvokeVariantInitializer(Assembly assembly)
    {
        Type initializerType = GetLoadableTypes(assembly)
            .Single(type => type.GetCustomAttribute<ModInitializerAttribute>() is not null);
        ModInitializerAttribute attribute =
            initializerType.GetCustomAttribute<ModInitializerAttribute>()!;
        MethodInfo initializer = initializerType.GetMethod(
            attribute.initializerMethod,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(initializerType.FullName, attribute.initializerMethod);
        initializer.Invoke(null, null);
    }

    private static bool TryParseVersion(string label, out Version version)
    {
        string normalized = label.Trim().TrimStart('v', 'V');
        int suffix = normalized.IndexOfAny(['-', '+']);
        if (suffix >= 0)
        {
            normalized = normalized[..suffix];
        }

        if (Version.TryParse(normalized, out Version? parsed))
        {
            version = parsed;
            return true;
        }

        version = new Version(0, 0);
        return false;
    }

    private sealed record VariantCandidate(Version Version, string AssemblyPath);
}
