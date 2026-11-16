using System;
using System.Collections.Generic;
using Omut.Core;
using Omut.Fishing;
using UnityEngine;

namespace Omut.Network
{
    // Отправляет пойманные трофеи на сервер (рекорды на сайте).
    // Если сервер недоступен, трофеи копятся в очереди (PlayerPrefs) и уходят позже.
    public class CatchUploader : MonoBehaviour
    {
        private const string QueueKey = "omut.upload_queue";
        private const float RetrySeconds = 20f;

        [Serializable]
        private class Queue
        {
            public List<CatchDto> items = new List<CatchDto>();
        }

        private Queue queue = new Queue();
        private bool sending;
        private float retryTimer;

        public int Pending => queue.items.Count;

        private void Awake()
        {
            string json = PlayerPrefs.GetString(QueueKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    queue = JsonUtility.FromJson<Queue>(json) ?? new Queue();
                }
                catch (ArgumentException)
                {
                    queue = new Queue();
                }
            }
        }

        private void OnEnable() => GameEvents.FishCaught += OnFishCaught;

        private void OnDisable() => GameEvents.FishCaught -= OnFishCaught;

        // Какие трофеи отправлять: рыбы из журнала (без сундучков и металлолома).
        public static bool ShouldUpload(CatchInfo info) => info?.fish != null && info.fish.InJournal && !info.fish.isChest;

        public static CatchDto ToDto(CatchInfo info) => new CatchDto
        {
            fishId = info.fishId,
            fishName = info.Title,
            weightKg = Math.Round(info.weight, 2),
            night = info.caughtAtNight,
            zone = info.zone.ToString(),
            gameDay = info.day,
            price = info.price
        };

        private void OnFishCaught(CatchInfo info)
        {
            AuthService auth = AuthService.Instance;
            if (auth == null || !auth.IsLoggedIn || !ShouldUpload(info))
                return;

            queue.items.Add(ToDto(info));
            Save();
            TrySend();
        }

        private void Update()
        {
            if (queue.items.Count == 0 || sending)
                return;

            retryTimer += Time.unscaledDeltaTime;
            if (retryTimer >= RetrySeconds)
                TrySend();
        }

        private void TrySend()
        {
            AuthService auth = AuthService.Instance;
            retryTimer = 0f;
            if (sending || queue.items.Count == 0 || auth == null || !auth.IsLoggedIn)
                return;

            sending = true;
            CatchDto next = queue.items[0];
            StartCoroutine(auth.Api.PostCatch(next, auth.Token, result =>
            {
                sending = false;
                if (result.ok)
                {
                    queue.items.Remove(next);
                    Save();
                    if (queue.items.Count > 0)
                        TrySend();
                }
                else if (result.status == 401)
                {
                    auth.TokenRejected();
                }
                else if (result.status == 400)
                {
                    queue.items.Remove(next); // сервер не принял (испорченные данные), не повторяем
                    Save();
                }
            }));
        }

        private void Save()
        {
            PlayerPrefs.SetString(QueueKey, JsonUtility.ToJson(queue));
            PlayerPrefs.Save();
        }
    }
}
