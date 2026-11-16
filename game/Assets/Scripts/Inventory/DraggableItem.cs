using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Omut.Inventory
{
    // Рыба в окне трюма, которую можно тащить мышью. Сама ничего не решает:
    // сообщает окну трюма (InventoryUI), куда её бросили.
    public class DraggableItem : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public GridItem Item { get; private set; }
        public bool PreviewRotated { get; set; }

        private InventoryUI owner;
        private RectTransform rect;
        private Image background;
        private Vector2 grabOffset;

        public void Setup(InventoryUI inventory, GridItem item, Image back)
        {
            owner = inventory;
            Item = item;
            rect = (RectTransform)transform;
            background = back;
        }

        public void OnPointerClick(PointerEventData eventData) => owner.Select(Item);

        public void OnBeginDrag(PointerEventData eventData)
        {
            owner.Select(Item);
            owner.BeginDrag(this);
            PreviewRotated = Item.rotated;
            background.raycastTarget = false;
            rect.SetAsLastSibling();
            owner.ScreenToGrid(eventData.position, out Vector2 local);
            grabOffset = rect.anchoredPosition - local;
        }

        public void OnDrag(PointerEventData eventData)
        {
            owner.ScreenToGrid(eventData.position, out Vector2 local);
            rect.anchoredPosition = local + grabOffset;
            owner.UpdateDragPreview(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            background.raycastTarget = true;
            bool inside = owner.ScreenToGrid(eventData.position, out _);
            owner.EndDrag(this, inside);
        }

        // Левая верхняя клетка, над которой сейчас предмет.
        public Vector2Int HoverCell(float cellSize)
        {
            Vector2 p = rect.anchoredPosition;
            return new Vector2Int(Mathf.RoundToInt(p.x / cellSize), Mathf.RoundToInt(-p.y / cellSize));
        }

        public void Tint(Color color) => background.color = color;
    }
}
