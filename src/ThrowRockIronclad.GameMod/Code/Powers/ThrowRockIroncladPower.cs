using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using ThrowRockIronclad.ThrowRockIroncladCode.Extensions;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Powers;

public abstract class ThrowRockIroncladPower : PowerModel
{
    private static readonly string[] IconFileNames =
    [
        "rockade_power.png",
        "rock_form_power.png",
        "rock_armor_power.png",
        "absolute_rock_power.png",
        "rock_charge_power.png",
    ];

    public abstract string IconFileName { get; }

    public string CustomPackedIconPath => IconFileName.PowerImagePath();
    public string CustomBigIconPath => IconFileName.BigPowerImagePath();

    public static IEnumerable<string> GetAllIconPaths()
        => IconFileNames.SelectMany(fileName => new[]
        {
            fileName.PowerImagePath(),
            fileName.BigPowerImagePath(),
        });

    public abstract override PowerType Type { get; }
    public abstract override PowerStackType StackType { get; }
}
