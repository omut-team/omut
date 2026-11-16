using System;
using System.Collections.Generic;
using Omut.Core;
using Omut.Fishing;
using Omut.UI;

namespace Omut.NPC
{
    // Дед Язэп: появляется на мостках после того, как Хозяин Омута отпущен. Дарит свою удочку.
    public class Yazep : Npc
    {
        public override List<(string label, Action action)> ExtraOptions()
        {
            var list = new List<(string, Action)>();
            GameSession session = GameSession.Instance;
            if (session != null && !session.OwnsRod("mystic"))
                list.Add(("Взять удочку Хозяина", () =>
                {
                    RodData rod = session.Database.FindRod("mystic");
                    session.GiveRod(rod);
                    Notifications.Show("Дед подарил «Удочку Хозяина»!", UiFactory.Theme.Gold);
                    GameEvents.RaisePlaySound("quest");
                }));
            return list;
        }
    }
}
