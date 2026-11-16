using Omut.Core;
using Omut.Player;
using UnityEngine;

namespace Omut.World
{
    // Следит, в какой зоне лодка (или Сымон), и сообщает о въезде в новую зону (GameEvents.ZoneEntered).
    public class ZoneTracker : MonoBehaviour
    {
        public static Zone Current { get; private set; }

        private void Update()
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.CurrentState == GameState.Menu)
                return;

            Vector2 point = PlayerWalker.OnFoot && PlayerWalker.Instance != null
                ? (Vector2)PlayerWalker.Instance.transform.position
                : DockingZone.Instance != null ? (Vector2)DockingZone.Instance.Boat.transform.position : Vector2.zero;

            Zone zone = Zone.Find(point);
            if (zone == null || zone == Current)
                return;

            Current = zone;
            GameEvents.RaiseZoneEntered(zone);
        }

        private void OnDestroy() => Current = null;
    }
}
