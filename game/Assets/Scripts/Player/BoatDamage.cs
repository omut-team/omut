using Omut.Core;
using Omut.UI;
using Omut.World;
using UnityEngine;

namespace Omut.Player
{
    // Удары лодки о коряги и скалы (объекты с компонентом Hazard).
    // Медленно подплыть можно, удар считается только на скорости.
    // Прочность кончилась: лодка пробита, улов тонет, Михалыч вытаскивает Сымона к причалу.
    [RequireComponent(typeof(Rigidbody2D))]
    public class BoatDamage : MonoBehaviour
    {
        [Tooltip("После удара столько секунд лодка неуязвима (чтобы одна коряга не била много раз)")]
        [SerializeField] private float invulnerableSeconds = 1.2f;

        [SerializeField] private SpriteRenderer sprite;

        private float safeUntil;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            Hazard hazard = collision.collider.GetComponent<Hazard>();
            GameSession session = GameSession.Instance;
            if (hazard == null || session == null || Time.time < safeUntil)
                return;

            if (collision.relativeVelocity.magnitude < hazard.MinImpactSpeed)
                return;

            safeUntil = Time.time + invulnerableSeconds;
            GameEvents.RaisePlaySound("hit");
            bool broken = session.Boat.Hit(hazard.Damage);

            if (broken)
            {
                Sink(session);
                return;
            }

            Notifications.Show($"Удар! Прочность лодки {session.Boat.Durability}/{session.Boat.MaxDurability}. Михалыч починит.", UiFactory.Theme.Bad);
        }

        private void Update()
        {
            if (sprite == null)
                return;

            // Мигаем после удара.
            bool blinking = Time.time < safeUntil;
            sprite.color = blinking && Mathf.Repeat(Time.time * 10f, 1f) > 0.5f ? new Color(1f, 0.6f, 0.6f) : Color.white;
        }

        private static void Sink(GameSession session)
        {
            int lost = session.Hold.Count;
            session.Hold.Clear();
            session.Boat.Set(1);
            DockingZone.Instance?.WakeUpAtDock();
            GameEvents.RaiseBoatSunk();
            Notifications.Show(lost > 0
                ? $"Лодка пробита! Улов ({lost} шт.) утонул. Михалыч дотащил тебя до причала."
                : "Лодка пробита! Михалыч дотащил тебя до причала.", UiFactory.Theme.Bad);
        }
    }
}
