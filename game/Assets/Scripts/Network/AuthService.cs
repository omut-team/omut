using System;
using UnityEngine;

namespace Omut.Network
{
    // Вход на сервер (паттерн Singleton). Создаётся сам при запуске игры.
    // Токен и имя хранятся в PlayerPrefs, поэтому входить каждый раз не надо.
    // Играть можно и без входа: тогда трофеи просто не попадают в рекорды на сайте.
    public class AuthService : MonoBehaviour
    {
        private const string TokenKey = "omut.token";
        private const string NameKey = "omut.player";
        private const string UrlKey = "omut.server";

        public static AuthService Instance { get; private set; }

        public ApiClient Api { get; private set; }
        public string Token { get; private set; }
        public string PlayerName { get; private set; }
        public bool IsLoggedIn => !string.IsNullOrEmpty(Token);

        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateOnStart()
        {
            Instance = null;
            Changed = null;
            var go = new GameObject("AuthService");
            go.AddComponent<AuthService>();
            go.AddComponent<CatchUploader>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Api = new ApiClient(PlayerPrefs.GetString(UrlKey, ApiClient.DefaultBaseUrl));
            Token = PlayerPrefs.GetString(TokenKey, "");
            PlayerName = PlayerPrefs.GetString(NameKey, "");
        }

        public void SetServer(string url)
        {
            Api.BaseUrl = string.IsNullOrWhiteSpace(url) ? ApiClient.DefaultBaseUrl : url.Trim().TrimEnd('/');
            PlayerPrefs.SetString(UrlKey, Api.BaseUrl);
            PlayerPrefs.Save();
        }

        // done(null) = получилось, иначе текст ошибки.
        public void Login(string name, string password, Action<string> done) =>
            StartCoroutine(Api.Login(name, password, r => Finish(r, done)));

        public void Register(string name, string password, Action<string> done) =>
            StartCoroutine(Api.Register(name, password, r => Finish(r, done)));

        public void CheckServer(Action<string> done) =>
            StartCoroutine(Api.Health(r => done(r.ok ? null : r.error)));

        public void Logout()
        {
            Token = "";
            PlayerName = "";
            PlayerPrefs.DeleteKey(TokenKey);
            PlayerPrefs.DeleteKey(NameKey);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        private void Finish(ApiResult result, Action<string> done)
        {
            if (!result.ok)
            {
                done(result.error);
                return;
            }

            AuthResponseDto auth = JsonUtility.FromJson<AuthResponseDto>(result.body);
            Token = auth.token;
            PlayerName = auth.name;
            PlayerPrefs.SetString(TokenKey, Token);
            PlayerPrefs.SetString(NameKey, PlayerName);
            PlayerPrefs.Save();
            Changed?.Invoke();
            done(null);
        }

        // Сервер сказал, что токен больше не годится.
        public void TokenRejected()
        {
            Logout();
            UI.Notifications.Show("Вход на сервер устарел. Войди заново в главном меню.");
        }
    }
}
