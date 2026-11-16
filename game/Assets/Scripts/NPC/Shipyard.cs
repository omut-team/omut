using System;
using System.Collections.Generic;
using Omut.UI;

namespace Omut.NPC
{
    // Михалыч, механик: чинит лодку за монеты и металлолом, продаёт катера.
    public class Shipyard : Npc
    {
        public override List<(string label, Action action)> ExtraOptions()
        {
            return new List<(string, Action)> { ("Ремонт и лодки", () => ShipyardUI.Instance?.Open()) };
        }
    }
}
