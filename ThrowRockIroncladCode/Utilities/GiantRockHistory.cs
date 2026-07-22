using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ThrowRockIronclad.ThrowRockIroncladCode.Utilities;

public static class GiantRockHistory
{
    public static int CountFinishedPlaysThisCombat(Creature owner, ICombatState combatState)
    {
        if (owner.CombatState != combatState)
        {
            return 0;
        }

        return CombatManager.Instance.History.CardPlaysFinished.Count(entry =>
            entry.CardPlay.Card is GiantRock && entry.CardPlay.Card.Owner.Creature == owner);
    }
}
