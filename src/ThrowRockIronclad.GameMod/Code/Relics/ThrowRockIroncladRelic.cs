using MegaCrit.Sts2.Core.Models;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Relics;

/// <summary>
/// Base type used to give this mod's original relics stable namespaced IDs and custom icon paths.
/// </summary>
public abstract class ThrowRockIroncladRelic : RelicModel
{
    public abstract string IconFileName { get; }

    public string CustomPackedIconPath
        => $"{MainFile.ResPath}/images/relics/{IconFileName}";

    public string CustomPackedIconOutlinePath
        => $"{MainFile.ResPath}/images/relics/{Path.GetFileNameWithoutExtension(IconFileName)}_outline.png";

    public string CustomBigIconPath
        => $"{MainFile.ResPath}/images/relics/big/{IconFileName}";

    public override string PackedIconPath => CustomPackedIconPath;

    protected override string PackedIconOutlinePath => CustomPackedIconOutlinePath;

    protected override string BigIconPath => CustomBigIconPath;
}
