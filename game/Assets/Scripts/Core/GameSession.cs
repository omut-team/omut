using Omut.Fishing;
using Omut.Inventory;
using Omut.Player;
using Omut.UI;
using UnityEngine;

namespace Omut.Core
{
    // Прогресс текущей игры (паттерн Singleton): журнал, трюм, деньги, наживки, квесты...
    // Сами расчёты лежат в обычных C#-классах (Journal, InventoryGrid, Wallet...), а этот объект
    // их хранит, связывает с событиями GameEvents и отдаёт остальным скриптам.
    // Лежит в сцене на объекте Systems (ставит сборщик сцены).
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [SerializeField] private GameDatabase database;

        public GameDatabase Database => database;
        public Journal Journal { get; } = new Journal();
        public InventoryGrid Hold { get; private set; }

        // Лодка, на которой плаваем сейчас (меняется на верфи, этап 14).
        public BoatData CurrentBoat { get; private set; }

        private void Awake()
        {
            Instance = this;
            ResetProgress();
            CatchCard.TryStore = StoreCatch;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            GameEvents.NewGameStarted += ResetProgress;
            GameEvents.FishCaught += OnFishCaught;
        }

        private void OnDisable()
        {
            GameEvents.NewGameStarted -= ResetProgress;
            GameEvents.FishCaught -= OnFishCaught;
        }

        // Новая игра: всё с нуля.
        public void ResetProgress()
        {
            Journal.Clear();
            CurrentBoat = database != null && database.boats.Count > 0 ? database.boats[0] : null;
            Hold = new InventoryGrid(CurrentBoat != null ? CurrentBoat.holdWidth : 4, CurrentBoat != null ? CurrentBoat.holdHeight : 5);
        }

        private void OnFishCaught(CatchInfo info)
        {
            info.isNewRecord = Journal.Register(info);
            if (info.isNewRecord)
                Notifications.Show($"Новая запись в журнале: {info.Title} ({Journal.CaughtCount} из {Journal.TotalEntries})", UiFactory.Theme.Gold);
        }

        // Положить улов в трюм. null = положили, иначе текст для игрока.
        private string StoreCatch(CatchInfo info)
        {
            if (Hold.TryAdd(info) != null)
            {
                Notifications.Show($"{info.Title} в трюме. Улов стоит {Hold.TotalPrice} мон.");
                return null;
            }

            return "Трюм полон! Отпусти рыбу или выброси что-нибудь из трюма (Tab).";
        }
    }
}
