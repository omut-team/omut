using Omut.Core;
using Omut.Fishing;
using Omut.UI;
using Omut.World;
using UnityEngine;

namespace Omut.Player
{
    // Место у конца мостков, где лодка причаливает. Подплыл и нажал E: Сымон сходит на берег.
    // Подошёл к концу мостков пешком и нажал E: садится в лодку.
    public class DockingZone : MonoBehaviour
    {
        public static DockingZone Instance { get; private set; }

        [SerializeField] private BoatController boat;
        [SerializeField] private PlayerWalker walker;
        [SerializeField] private CameraFollow cameraFollow;

        [Tooltip("Где на мостках стоит Сымон, когда сходит с лодки")]
        [SerializeField] private Transform walkPoint;

        [Tooltip("Где Сымон появляется в начале новой игры (у хаты)")]
        [SerializeField] private Transform startPoint;

        [Tooltip("С какого расстояния лодка может причалить, клеток")]
        [SerializeField] private float dockRadius = 3.5f;

        [Tooltip("С какого расстояния от лодки Сымон может в неё сесть (стоит на мостках у кармана)")]
        [SerializeField] private float boardRadius = 3.3f;

        public BoatController Boat => boat;
        public PlayerWalker Walker => walker;

        // Лодка у причала (рядом с мостками).
        public bool BoatNearDock => boat != null && Vector2.Distance(boat.transform.position, transform.position) <= dockRadius;

        // Сымон пешком стоит у лодки: на нижних мостках у точки посадки или где угодно на мостках рядом с лодкой.
        // Раньше садились только с одной точки (1,3 клетки от неё), и к лодке подходили, а сесть не могли.
        public bool CanBoard(Vector2 walkerPosition) =>
            BoatNearDock && (Vector2.Distance(walkerPosition, walkPoint.position) <= 1.8f
                             || Vector2.Distance(walkerPosition, boat.transform.position) <= boardRadius);

        private void Awake()
        {
            Instance = this;
            FishingController.CanFishHere = () => !PlayerWalker.OnFoot;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable() => GameEvents.NewGameStarted += StartOnFoot;

        private void OnDisable() => GameEvents.NewGameStarted -= StartOnFoot;

        private void Start()
        {
            // До нажатия "Новая игра" Сымон сидит в лодке (за меню видно озеро).
            if (!PlayerWalker.OnFoot)
                walker.Hide();
        }

        private void Update()
        {
            GameManager game = GameManager.Instance;
            if (game != null && game.CurrentState != GameState.Sailing)
                return;

            if (!PlayerWalker.OnFoot)
            {
                if (BoatNearDock)
                {
                    Hud.SetPrompt("E: причалить и сойти на берег", 5);
                    if (Controls.Pressed(Controls.Interact))
                        Disembark();
                }
            }
            else if (CanBoard(walker.transform.position))
            {
                Hud.SetPrompt("E: сесть в лодку", 20);
                if (Controls.Pressed(Controls.Interact))
                    Board();
            }
        }

        // Сойти на берег.
        public void Disembark()
        {
            Rigidbody2D body = boat.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            boat.transform.position = transform.position;

            PlayerWalker.OnFoot = true;
            walker.Show(walkPoint.position);
            cameraFollow.Target = walker.transform;
            GameEvents.RaiseDockChanged(true);
        }

        // Сесть в лодку.
        public void Board()
        {
            PlayerWalker.OnFoot = false;
            walker.Hide();
            boat.transform.position = transform.position;
            cameraFollow.Target = boat.transform;
            GameEvents.RaiseDockChanged(false);
        }

        // Новая игра: Сымон у хаты деда, лодка у мостков.
        private void StartOnFoot()
        {
            boat.transform.position = transform.position;
            boat.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            PlayerWalker.OnFoot = true;
            walker.Show(startPoint.position);
            cameraFollow.Target = walker.transform;
            cameraFollow.SnapToTarget();
        }

        // Загрузка сохранения: где лодка и где Сымон.
        public Vector2 MooringPosition => transform.position;
        public Vector2 StartPosition => startPoint != null ? (Vector2)startPoint.position : (Vector2)walkPoint.position;

        public void Restore(bool onFoot, Vector2 boatPosition, Vector2 walkerPosition)
        {
            Rigidbody2D body = boat.GetComponent<Rigidbody2D>();
            body.linearVelocity = Vector2.zero;
            boat.transform.position = boatPosition == Vector2.zero ? (Vector2)transform.position : boatPosition;

            PlayerWalker.OnFoot = onFoot;
            if (onFoot)
            {
                walker.Show(PlayerWalker.CanStand(walkerPosition) ? walkerPosition : (Vector2)walkPoint.position);
                cameraFollow.Target = walker.transform;
            }
            else
            {
                walker.Hide();
                cameraFollow.Target = boat.transform;
            }

            cameraFollow.SnapToTarget();
        }

        // Обморок или потопление: проснулись на причале.
        public void WakeUpAtDock()
        {
            boat.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            boat.transform.position = transform.position;
            PlayerWalker.OnFoot = true;
            walker.Show(walkPoint.position);
            cameraFollow.Target = walker.transform;
            cameraFollow.SnapToTarget();
            GameEvents.RaiseDockChanged(true);
        }
    }
}
