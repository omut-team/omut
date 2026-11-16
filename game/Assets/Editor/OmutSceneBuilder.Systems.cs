using Omut.Core;
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
        }

        static partial void BuildUi(GameObject ui)
        {
            EnsureComponent<JournalUI>(ui);
        }
    }
}
