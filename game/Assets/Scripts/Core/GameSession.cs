using Omut.Fishing;
using UnityEngine;

namespace Omut.Core
{
    // Прогресс текущей игры (паттерн Singleton): журнал, трюм, деньги, наживки, квесты...
    // Сами расчёты лежат в обычных C#-классах (Journal, InventoryGrid, Wallet...), а этот объект
    // их хранит, связывает с событиями GameEvents и отдаёт остальным скриптам.
    // Лежит в сцене на объекте Systems (ставит сборщик сцены).
    public partial class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [SerializeField] private GameDatabase database;

        public GameDatabase Database => database;
        public Journal Journal { get; } = new Journal();

        private void Awake()
        {
            Instance = this;
            InitSystems();
            ResetProgress();
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
            SubscribeMore(true);
        }

        private void OnDisable()
        {
            GameEvents.NewGameStarted -= ResetProgress;
            GameEvents.FishCaught -= OnFishCaught;
            SubscribeMore(false);
        }

        // Новая игра: всё с нуля.
        public void ResetProgress()
        {
            Journal.Clear();
            ResetMore();
        }

        private void OnFishCaught(CatchInfo info)
        {
            info.isNewRecord = Journal.Register(info);
            if (info.isNewRecord)
                UI.Notifications.Show($"Новая запись в журнале: {info.Title} ({Journal.CaughtCount} из {Journal.TotalEntries})", UI.UiFactory.Theme.Gold);
        }

        // Следующие этапы добавляют сюда свои системы (partial-методы в других файлах GameSession.*.cs).
        partial void InitSystems();
        partial void ResetMore();
        partial void SubscribeMore(bool subscribe);
    }
}
