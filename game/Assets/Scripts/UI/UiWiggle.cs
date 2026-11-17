using UnityEngine;

namespace Omut.UI
{
    // Живая картинка улова в интерфейсе: при появлении "выпрыгивает", потом слегка покачивается.
    // Вешается на картинку рыбы или предмета (карточка трофея, трюм).
    public class UiWiggle : MonoBehaviour
    {
        [Tooltip("Качаться всё время (false: только прыжок при появлении)")]
        public bool sway = true;

        [Tooltip("Насколько сильно качается, градусы")]
        public float angle = 4f;

        private const float PopTime = 0.35f;
        private float age;
        private float phase;
        private Quaternion baseRotation;
        private bool started;

        // Начать заново (новая рыба на той же картинке).
        public void Replay()
        {
            age = 0f;
            phase = Random.value * 10f;
        }

        private void OnEnable()
        {
            if (!started)
            {
                baseRotation = transform.localRotation;
                started = true;
            }
            Replay();
        }

        // Без прыжка (вещь уже видели, окно просто перерисовалось).
        public void SkipPop() => age = PopTime;

        // Поворот картинки в трюме (R) меняет базовый угол.
        public void SetBaseRotation(Quaternion rotation)
        {
            baseRotation = rotation;
            started = true;
        }

        private void Update()
        {
            age += Time.unscaledDeltaTime;

            // Прыжок: из 60% в 110% и обратно в 100%.
            float t = Mathf.Clamp01(age / PopTime);
            float scale = t < 1f ? Mathf.LerpUnclamped(0.6f, 1f, Back(t)) : 1f;
            transform.localScale = Vector3.one * scale;

            float tilt = sway ? Mathf.Sin((age + phase) * 2.6f) * angle : 0f;
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, tilt);
        }

        // Плавно с небольшим перелётом (как пружинка).
        private static float Back(float t)
        {
            const float s = 1.7f;
            t -= 1f;
            return t * t * ((s + 1f) * t + s) + 1f;
        }
    }
}
