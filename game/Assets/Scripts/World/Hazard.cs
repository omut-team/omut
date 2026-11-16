using UnityEngine;

namespace Omut.World
{
    // Опасный объект: коряга или скала. Удар лодки о него снимает прочность (этап 14, BoatDamage).
    public class Hazard : MonoBehaviour
    {
        [Tooltip("Сколько прочности снимает удар")]
        [Min(1)] [SerializeField] private int damage = 1;

        [Tooltip("С какой скорости удар считается (клеток в секунду). Медленно подплыть можно")]
        [Min(0f)] [SerializeField] private float minImpactSpeed = 1.5f;

        public int Damage => damage;
        public float MinImpactSpeed => minImpactSpeed;
    }
}
