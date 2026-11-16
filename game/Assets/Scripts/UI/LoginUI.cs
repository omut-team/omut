using System.Text;
using Omut.Network;
using UnityEngine;
using UnityEngine.UI;

namespace Omut.UI
{
    // Окно "Онлайн-рекорды" из главного меню: адрес сервера, вход и регистрация, топ трофеев.
    // Без входа играть можно, тогда трофеи не попадают на сайт.
    public class LoginUI : MonoBehaviour
    {
        public static LoginUI Instance { get; private set; }

        private Canvas canvas;
        private InputField serverField;
        private InputField nameField;
        private InputField passwordField;
        private Text status;
        private Text records;
        private RectTransform loginBlock;
        private Button logoutButton;
        private System.Action onClose;

        private void Awake()
        {
            Instance = this;
            canvas = UiFactory.CreateCanvas("LoginCanvas", 110, transform);
            Build();
            canvas.gameObject.SetActive(false);
        }

        private void OnEnable() => AuthService.Changed += Refresh;

        private void OnDisable() => AuthService.Changed -= Refresh;

        private void Build()
        {
            RectTransform paper = UiFactory.Window(canvas.transform, "Login", new Vector2(1500f, 820f), "Онлайн-рекорды", out _);

            RectTransform left = UiFactory.Rect(paper, "Left");
            UiFactory.Place(left, new Vector2(0f, 1f), new Vector2(640f, 620f), new Vector2(50f, -100f));
            UiFactory.Column(left, 14f, TextAnchor.UpperLeft);

            UiFactory.Label(left, "Адрес сервера", 26, TextAnchor.MiddleLeft, UiFactory.Theme.InkLight).rectTransform.sizeDelta = new Vector2(640f, 34f);
            serverField = UiFactory.Input(left, ApiClient.DefaultBaseUrl, false, new Vector2(640f, 56f));

            loginBlock = UiFactory.Rect(left, "LoginBlock");
            loginBlock.sizeDelta = new Vector2(640f, 280f);
            UiFactory.Column(loginBlock, 14f, TextAnchor.UpperLeft);
            UiFactory.Label(loginBlock, "Имя и пароль", 26, TextAnchor.MiddleLeft, UiFactory.Theme.InkLight).rectTransform.sizeDelta = new Vector2(640f, 34f);
            nameField = UiFactory.Input(loginBlock, "имя (3-20 букв)", false, new Vector2(640f, 56f));
            passwordField = UiFactory.Input(loginBlock, "пароль (от 6 символов)", true, new Vector2(640f, 56f));

            RectTransform row = UiFactory.Rect(loginBlock, "Buttons");
            row.sizeDelta = new Vector2(640f, 64f);
            UiFactory.Row(row, 20f, TextAnchor.MiddleLeft);
            UiFactory.Button(row, "Войти", () => Auth(false), new Vector2(300f, 60f));
            UiFactory.Button(row, "Регистрация", () => Auth(true), new Vector2(300f, 60f));

            logoutButton = UiFactory.Button(left, "Выйти из аккаунта", Logout, new Vector2(400f, 60f));

            status = UiFactory.Label(left, "", 26, TextAnchor.UpperLeft, UiFactory.Theme.Accent);
            status.rectTransform.sizeDelta = new Vector2(640f, 90f);

            records = UiFactory.Label(paper, "", 26, TextAnchor.UpperLeft, UiFactory.Theme.Ink);
            records.supportRichText = true;
            UiFactory.Place(records.rectTransform, new Vector2(1f, 1f), new Vector2(680f, 600f), new Vector2(-50f, -100f));

            Button close = UiFactory.Button(paper, "Закрыть", Close, new Vector2(300f, 64f));
            UiFactory.Place((RectTransform)close.transform, new Vector2(1f, 0f), new Vector2(300f, 64f), new Vector2(-50f, 30f));
        }

        public bool IsOpen => canvas.gameObject.activeSelf;

        public void Show(System.Action closed)
        {
            onClose = closed;
            canvas.gameObject.SetActive(true);
            serverField.text = AuthService.Instance != null ? AuthService.Instance.Api.BaseUrl : ApiClient.DefaultBaseUrl;
            status.text = "";
            Refresh();
            LoadRecords();
        }

        public void Close()
        {
            canvas.gameObject.SetActive(false);
            onClose?.Invoke();
        }

        private void Refresh()
        {
            AuthService auth = AuthService.Instance;
            bool logged = auth != null && auth.IsLoggedIn;
            loginBlock.gameObject.SetActive(!logged);
            logoutButton.gameObject.SetActive(logged);
            if (logged)
                status.text = $"Ты вошёл как <b>{auth.PlayerName}</b>. Пойманные трофеи уходят в рекорды.";
            status.supportRichText = true;
        }

        private void Auth(bool register)
        {
            AuthService auth = AuthService.Instance;
            if (auth == null)
                return;

            auth.SetServer(serverField.text);
            status.text = "Связываюсь с сервером...";
            System.Action<string> done = error =>
            {
                status.text = error ?? "";
                if (error == null)
                {
                    passwordField.text = "";
                    LoadRecords();
                }

                Refresh();
            };

            if (register) auth.Register(nameField.text, passwordField.text, done);
            else auth.Login(nameField.text, passwordField.text, done);
        }

        private void Logout()
        {
            AuthService.Instance?.Logout();
            status.text = "Вышел из аккаунта. Играть можно и так.";
        }

        private void LoadRecords()
        {
            AuthService auth = AuthService.Instance;
            if (auth == null)
                return;

            auth.SetServer(serverField.text);
            records.text = "<b>Самые тяжёлые трофеи</b>\nзагружаю...";
            StartCoroutine(auth.Api.Leaderboard(10, result =>
            {
                if (!result.ok)
                {
                    records.text = "<b>Самые тяжёлые трофеи</b>\n" + result.error +
                                   "\n\nЗапусти сервер: в папке проекта команда\n<b>docker compose up --build</b>";
                    return;
                }

                var sb = new StringBuilder("<b>Самые тяжёлые трофеи</b>\n");
                CatchRecordDto[] list = ApiClient.ParseRecords(result.body);
                if (list.Length == 0)
                    sb.Append("Пока пусто. Будь первым!");
                for (int i = 0; i < list.Length; i++)
                    sb.Append($"{i + 1}. {list[i].fishName}, {list[i].weightKg:0.00} кг, {list[i].player}{(list[i].night ? " (ночь)" : "")}\n");
                sb.Append("\nВсе рекорды на сайте: http://localhost:8080");
                records.text = sb.ToString();
            }));
        }
    }
}
