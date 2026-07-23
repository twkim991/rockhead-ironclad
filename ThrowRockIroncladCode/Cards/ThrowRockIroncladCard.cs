using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Cards;

/// <summary>
/// Base type used to give this mod's original cards stable namespaced model IDs.
/// </summary>
public abstract class ThrowRockIroncladCard(
    int canonicalEnergyCost,
    CardType type,
    CardRarity rarity,
    TargetType targetType)
    : CardModel(canonicalEnergyCost, type, rarity, targetType);
