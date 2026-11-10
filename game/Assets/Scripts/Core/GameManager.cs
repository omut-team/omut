using UnityEngine;

namespace Omut.Core
{
    // Главный управляющий объект игры (паттерн Singleton: на всю игру он один,
    // и любой скрипт может обратиться к нему через GameManager.Instance).
    // Хранит автомат состояний, ставит игру на паузу и сообщает о смене состояний через GameEvents.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Tooltip("Писать смену состояний в консоль")]
        [SerializeField] private bool logStateChanges = true;

        private GameStateMachine stateMachine;

        public GameState CurrentState => stateMachine.Current;

        // Unity сама вызывает этот метод перед загрузкой первой сцены.
        // Так GameManager появляется в любой сцене, и его не надо добавлять руками.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateOnStart()
        {
            Instance = null; // сброс после прошлого запуска Play

            var managerObject = new GameObject("GameManager");
            managerObject.AddComponent<GameManager>();

            // Клавиши для проверки состояний нужны только в редакторе и тестовых сборках.
            if (Debug.isDebugBuild)
                managerObject.AddComponent<DebugStateKeys>();
        }

        private void Awake()
        {
            // Второй GameManager не нужен: если он уже есть, этот удаляем.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject); // не удалять при смене сцены

            stateMachine = new GameStateMachine(GameState.Menu);
            stateMachine.StateChanged += OnStateChanged;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            if (logStateChanges)
                Debug.Log($"[GameManager] Игра запущена, состояние: {CurrentState}");
        }

        // Перейти в другое состояние. Возвращает false, если такой переход запрещён.
        public bool ChangeState(GameState next)
        {
            bool changed = stateMachine.TryChangeTo(next);

            if (!changed && logStateChanges)
                Debug.LogWarning($"[GameManager] Нельзя перейти из {CurrentState} в {next}");

            return changed;
        }

        public void StartGame() => ChangeState(GameState.Sailing);

        public void BackToMenu() => ChangeState(GameState.Menu);

        public void TogglePause()
        {
            if (!stateMachine.TogglePause() && logStateChanges)
                Debug.LogWarning($"[GameManager] Сейчас ({CurrentState}) пауза недоступна");
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            // На паузе останавливаем время: всё, что зависит от Time.deltaTime, замирает.
            Time.timeScale = to == GameState.Paused ? 0f : 1f;

            if (logStateChanges)
                Debug.Log($"[GameManager] Состояние: {from} -> {to}");

            // Сообщаем всем подписчикам (интерфейс, звук, лодка и т.д.).
            GameEvents.RaiseGameStateChanged(from, to);
        }
    }
}
