namespace ThrowRockIronclad.ThrowRockIroncladCode.Core;

public static class ModInfo
{
    public const string Id = "ThrowRockIronclad";
#if THROW_ROCK_GAME_0_109
    public const string SupportedGameVersion = "0.109.x";

    public static bool SupportsGameVersion(string version)
        => Version.TryParse(version.TrimStart('v', 'V'), out Version? parsed)
            && parsed.Major == 0
            && parsed.Minor == 109;
#else
    public const string SupportedGameVersion = "0.107.1";

    public static bool SupportsGameVersion(string version)
        => string.Equals(
            version.TrimStart('v', 'V'),
            SupportedGameVersion,
            StringComparison.Ordinal);
#endif
}
