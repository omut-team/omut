using Omut.Core;
using Omut.Inventory;
using Omut.UI;
using UnityEngine;

namespace Omut.EditorTools
{
    // Системные объекты и окна интерфейса, которые добавляются в сцену по этапам.
    public static partial class OmutSceneBuilder
    {
        static partial void BuildSystems()
        {
            GameObject systems = EnsureRoot("Systems");
            SetRef(EnsureComponent<GameSession>(systems), "database", Database);

            // Свет суток всегда из кода (DayNightCycle.CreateDefault...), чтобы правки цветов попадали в сцену.
            var cycle = Object.FindAnyObjectByType<Omut.World.DayNightCycle>();
            if (cycle != null)
            {
                var so = new UnityEditor.SerializedObject(cycle);
                so.FindProperty("lightColor").gradientValue = Omut.World.DayNightCycle.CreateDefaultColors();
                so.FindProperty("lightIntensity").animationCurveValue = Omut.World.DayNightCycle.CreateDefaultIntensity();
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static partial void BuildMoreDockPeople(Transform dock)
        {
            GameObject mikhalych = Npc(dock, "Mikhalych", "npc_mikhalych", MikhalychPosition);
            AddMikhalych(mikhalych).Setup(Omut.NPC.NpcId.Mikhalych, "Михалыч", OmutDataBuilder.FindSprite("npc_mikhalych_portrait"),
                Database.FindDialogue("mikhalych_first"),
                new System.Collections.Generic.List<string> { "Ну что, лодка цела?", "Железо привёз?", "Мотор слышу издалека. Заходи." });
        }

        private static Omut.NPC.Npc AddMikhalych(GameObject go) => go.AddComponent<Omut.NPC.Npc>();

        static partial void BuildUi(GameObject ui)
        {
            EnsureComponent<Hud>(ui);
            EnsureComponent<ShopUI>(ui);
            EnsureComponent<JournalUI>(ui);
            EnsureComponent<QuestLogUI>(ui);
            SetRef(EnsureComponent<DialogueUI>(ui), "heroPortrait", OmutDataBuilder.FindSprite("hero_symon_portrait"));
            EnsureComponent<InventoryUI>(ui);
        }
    }
}
