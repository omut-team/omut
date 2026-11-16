using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Omut.Network
{
    // Данные для обмена с сервером (имена полей как в JSON сервера).
    [Serializable] public class AuthRequestDto { public string name; public string password; }
    [Serializable] public class AuthResponseDto { public string token; public string name; }
    [Serializable] public class ErrorDto { public string error; }

    [Serializable]
    public class CatchDto
    {
        public string fishId;
        public string fishName;
        public double weightKg;
        public bool night;
        public string zone;
        public int gameDay;
        public int price;
    }

    [Serializable]
    public class CatchRecordDto
    {
        public string player;
        public string fishId;
        public string fishName;
        public double weightKg;
        public bool night;
    }

    // JsonUtility не умеет читать массив верхнего уровня, поэтому оборачиваем.
    [Serializable] public class CatchRecordList { public CatchRecordDto[] items; }

    // Ответ сервера: удалось ли, код, текст и ошибка для игрока.
    public struct ApiResult
    {
        public bool ok;
        public long status;
        public string body;
        public string error;
    }

    // Запросы к серверу через UnityWebRequest. Все методы корутины: StartCoroutine(api.Login(...)).
    public class ApiClient
    {
        public const string DefaultBaseUrl = "http://localhost:5000";

        public string BaseUrl { get; set; }

        public ApiClient(string baseUrl)
        {
            BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.TrimEnd('/');
        }

        public IEnumerator Health(Action<ApiResult> done) => Send("GET", "/api/health", null, null, done);

        public IEnumerator Register(string name, string password, Action<ApiResult> done) =>
            Send("POST", "/api/auth/register", JsonUtility.ToJson(new AuthRequestDto { name = name, password = password }), null, done);

        public IEnumerator Login(string name, string password, Action<ApiResult> done) =>
            Send("POST", "/api/auth/login", JsonUtility.ToJson(new AuthRequestDto { name = name, password = password }), null, done);

        public IEnumerator PostCatch(CatchDto dto, string token, Action<ApiResult> done) =>
            Send("POST", "/api/catches", JsonUtility.ToJson(dto), token, done);

        public IEnumerator Leaderboard(int top, Action<ApiResult> done) => Send("GET", "/api/leaderboard?top=" + top, null, null, done);

        private IEnumerator Send(string method, string path, string json, string token, Action<ApiResult> done)
        {
            using var request = new UnityWebRequest(BaseUrl + path, method);
            if (json != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.SetRequestHeader("Content-Type", "application/json");
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 8;
            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", "Bearer " + token);

            yield return request.SendWebRequest();

            var result = new ApiResult
            {
                status = request.responseCode,
                body = request.downloadHandler != null ? request.downloadHandler.text : "",
                ok = request.result == UnityWebRequest.Result.Success
            };

            if (!result.ok)
                result.error = ErrorText(request, result.body);

            done?.Invoke(result);
        }

        // Понятный текст ошибки: из ответа сервера или "сервер не отвечает".
        public static string ErrorText(UnityWebRequest request, string body)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError)
                return "Сервер не отвечает. Запущен ли docker compose?";

            if (!string.IsNullOrEmpty(body) && body.Contains("\"error\""))
            {
                try
                {
                    return JsonUtility.FromJson<ErrorDto>(body).error;
                }
                catch (ArgumentException)
                {
                }
            }

            return request.responseCode == 401 ? "Нужно войти заново" : $"Ошибка сервера ({request.responseCode})";
        }

        // Массив из ответа сервера: [{...},{...}] -> CatchRecordDto[].
        public static CatchRecordDto[] ParseRecords(string body)
        {
            if (string.IsNullOrEmpty(body))
                return Array.Empty<CatchRecordDto>();
            CatchRecordList list = JsonUtility.FromJson<CatchRecordList>("{\"items\":" + body + "}");
            return list?.items ?? Array.Empty<CatchRecordDto>();
        }
    }
}
