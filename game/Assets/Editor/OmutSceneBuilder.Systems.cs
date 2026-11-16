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

        static partial void BuildUi(GameObject ui)
        {
            EnsureComponent<Hud>(ui);
            EnsureComponent<ShopUI>(ui);
            EnsureComponent<JournalUI>(ui);
            EnsureComponent<InventoryUI>(ui);
        }
    }
}
