using System;
using Omut.World;

namespace Omut.Fishing
{
    // Один пойманный трофей: что, сколько весит, когда пойман, сколько стоит.
    [Serializable]
    public class CatchInfo
    {
        public string fishId;
        public float weight;
        public bool caughtAtNight;
        public int price;
        public ZoneId zone;
        public int day;

        [NonSerialized] public FishData fish;

        // Первый раз поймали этот вид (ставит журнал).
        [NonSerialized] public bool isNewRecord;

        public CatchInfo(FishData fish, float weight, bool caughtAtNight, ZoneId zone, int day)
        {
            this.fish = fish;
            fishId = fish != null ? fish.id : "";
            this.weight = weight;
            this.caughtAtNight = caughtAtNight;
            this.zone = zone;
            this.day = day;
            price = fish != null ? FishSelector.Price(fish, weight, caughtAtNight) : 0;
        }

        public string Title => fish != null ? fish.displayName : fishId;
    }
}
