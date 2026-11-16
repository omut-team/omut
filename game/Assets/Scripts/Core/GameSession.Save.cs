using System;
using System.Collections.Generic;
using Omut.Fishing;
using Omut.Inventory;
using Omut.Player;
using Omut.UI;
using Omut.World;
using UnityEngine;

namespace Omut.Core
{
    // Сохранение и загрузка прогресса: собрать всё в SaveData и разложить обратно.
    public partial class GameSession
    {
        // Другие системы (квесты, лодки, паника, дневник) дописывают сюда своё.
        public static event Action<SaveData> Capturing;
        public static event Action<SaveData> Applying;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSaveEvents()
        {
            Capturing = null;
            Applying = null;
            BoatChanged = null;
        }

        private void HookSaving()
        {
            MainMenu.HasSave = SaveSystem.Exists;
            PauseMenu.SaveGame = () => SaveGame() ? "Игра сохранена." : "Не получилось сохранить.";
        }

        public bool SaveGame()
        {
            bool ok = SaveSystem.Save(Capture());
            if (ok)
                Debug.Log("[GameSession] Сохранено: " + SaveSystem.FilePath);
            return ok;
        }

        private void AutoSave(string reason)
        {
            GameManager game = GameManager.Instance;
            if (game == null || game.CurrentState == GameState.Menu)
                return;

            if (SaveGame())
                Notifications.Show("Автосохранение (" + reason + ")", new Color(0.7f, 0.8f, 0.7f));
        }

        private void OnNewDayAutoSave(int day) => AutoSave("утро");

        private void OnDockAutoSave(bool onShore)
        {
            if (onShore)
                AutoSave("причал");
        }

        private void OnContinue()
        {
            SaveData data = SaveSystem.Load();
            if (data == null)
            {
                Notifications.Show("Сохранение не найдено или испорчено. Начинаем новую игру.");
                GameEvents.RaiseNewGameStarted();
                return;
            }

            Apply(data);
            Notifications.Show($"Загружено: день {data.day}, {data.savedAt}");
        }

        public SaveData Capture()
        {
            var data = new SaveData();

            GameClock clock = DayNightCycle.Instance != null ? DayNightCycle.Instance.Clock : null;
            if (clock != null)
            {
                data.day = clock.Day;
                data.minutes = clock.Minutes;
            }

            data.money = Wallet.Money;
            data.totalEarned = Wallet.TotalEarned;
            data.kerosene = Kerosene;
            data.thermos = Thermos;
            data.spoons = Tackle.Count(BaitType.Spoon);
            data.phosphor = Tackle.Count(BaitType.Phosphor);
            data.currentBait = Tackle.Current.ToString();
            data.boatId = CurrentBoat != null ? CurrentBoat.name : "Barkas";
            data.journal = Journal.ToList();
            data.totalCaught = TotalCaught;

            foreach (GridItem item in Hold.Items)
            {
                CatchInfo c = item.catchInfo;
                data.hold.Add(new SavedItem
                {
                    fishId = c.fishId, weight = c.weight, night = c.caughtAtNight, price = c.price, zone = c.zone, day = c.day,
                    x = item.position.x, y = item.position.y, rotated = item.rotated
                });
            }

            data.onFoot = PlayerWalker.OnFoot;
            DockingZone dock = DockingZone.Instance;
            if (dock != null)
            {
                Vector2 boat = dock.Boat.transform.position;
                Vector2 walker = dock.Walker.transform.position;
                data.boatX = boat.x;
                data.boatY = boat.y;
                data.walkerX = walker.x;
                data.walkerY = walker.y;
            }

            CaptureQuests(data);
            Capturing?.Invoke(data);
            return data;
        }

        public void Apply(SaveData data)
        {
            ResetProgress();

            GameClock clock = DayNightCycle.Instance != null ? DayNightCycle.Instance.Clock : null;
            if (clock != null)
            {
                clock.SetDay(data.day);
                int total = Mathf.RoundToInt(data.minutes);
                clock.SetTime(total / 60, total % 60);
            }

            BoatData boat = database.FindBoat(data.boatId);
            if (boat != null)
                SetBoat(boat);

            Wallet.Set(data.money, data.totalEarned);
            Kerosene = Mathf.Clamp(data.kerosene, 0f, 100f);
            Thermos = Mathf.Clamp(data.thermos, 0, MaxThermos);
            Tackle.Set(BaitType.Spoon, data.spoons);
            Tackle.Set(BaitType.Phosphor, data.phosphor);
            if (Enum.TryParse(data.currentBait, out BaitType bait))
                Tackle.Select(bait);

            Journal.Load(data.journal);
            TotalCaught = data.totalCaught;

            foreach (SavedItem saved in data.hold ?? new List<SavedItem>())
            {
                FishData fish = database.FindFish(saved.fishId);
                if (fish == null)
                    continue;

                var info = new CatchInfo(fish, saved.weight, saved.night, saved.zone, saved.day) { price = saved.price };
                var item = new GridItem { catchInfo = info, baseSize = fish.holdSize, position = new Vector2Int(saved.x, saved.y), rotated = saved.rotated };
                if (!Hold.Restore(item))
                    Hold.TryAdd(info);
            }

            DockingZone.Instance?.Restore(data.onFoot, new Vector2(data.boatX, data.boatY), new Vector2(data.walkerX, data.walkerY));

            ApplyQuests(data);
            Applying?.Invoke(data);
        }
    }
}
