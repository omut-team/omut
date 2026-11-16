using System.Collections.Generic;
using Omut.Player;

namespace Omut.Core
{
    // Лодки игрока: какие куплены и прочность текущей.
    public partial class GameSession
    {
        private readonly HashSet<string> ownedBoats = new HashSet<string>();

        public BoatStats Boat { get; private set; } = new BoatStats(2);

        public bool OwnsBoat(BoatData boat) => boat != null && ownedBoats.Contains(boat.name);

        public void AddOwnedBoat(BoatData boat)
        {
            if (boat != null)
                ownedBoats.Add(boat.name);
        }

        private void ResetBoats()
        {
            ownedBoats.Clear();
            if (CurrentBoat != null)
                ownedBoats.Add(CurrentBoat.name);
            Boat = new BoatStats(CurrentBoat != null ? CurrentBoat.durability : 2);
        }

        private string BoatHudLine()
        {
            string hearts = new string('■', Boat.Durability) + new string('□', Boat.Missing);
            return $"Лодка: {hearts}";
        }

        private void CaptureBoats(SaveData data)
        {
            data.ownedBoats = new List<string>(ownedBoats);
            data.durability = Boat.Durability;
        }

        private void ApplyBoats(SaveData data)
        {
            if (data.ownedBoats != null)
                ownedBoats.UnionWith(data.ownedBoats);
            Boat.Set(data.durability);
        }
    }
}
