using System.Collections.Generic;
using UnityEngine;

namespace Omut.NPC
{
    // То, с чем можно взаимодействовать на причале по E: Тарас, Михалыч, сундук деда.
    // Герой (PlayerWalker) сам ищет ближайший такой объект и показывает подсказку.
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        [Tooltip("С какого расстояния можно нажать E, клеток")]
        [SerializeField] private float radius = 1.6f;

        public float Radius => radius;

        // Текст подсказки: "Поговорить с Тарасом".
        public abstract string Prompt { get; }

        public virtual bool CanInteract => true;

        public abstract void Interact();

        protected virtual void OnEnable() => All.Add(this);

        protected virtual void OnDisable() => All.Remove(this);

        public static Interactable FindNearest(Vector2 position)
        {
            Interactable best = null;
            float bestDistance = float.MaxValue;
            foreach (Interactable i in All)
            {
                float d = Vector2.Distance(position, i.transform.position);
                if (d <= i.Radius && d < bestDistance && i.CanInteract)
                {
                    best = i;
                    bestDistance = d;
                }
            }

            return best;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
