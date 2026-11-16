using System.Collections.Generic;
using Omut.Core;
using Omut.Fishing;
using Omut.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Omut.Inventory
{
    // Окно трюма (Tab или I): сетка клеток, рыбы-прямоугольники.
    // Мышью перетаскивать, R во время перетаскивания: повернуть, бросить мимо сетки: выбросить за борт.
    public class InventoryUI : GameWindow
    {
        private const float Cell = 100f;

        private RectTransform grid;
        private RectTransform cellsLayer;
        private RectTransform itemsLayer;
        private Text info;
        private Text summary;
        private Button throwButton;
        private RectTransform confirm;
        private Text confirmText;

        private readonly List<DraggableItem> views = new List<DraggableItem>();
        private GridItem selected;
        private GridItem pendingThrow;
        private DraggableItem dragging;
        private int builtWidth = -1;
        private int builtHeight = -1;

        private InventoryGrid Hold => GameSession.Instance != null ? GameSession.Instance.Hold : null;

        protected override Key ToggleKey => Controls.Hold;
        protected override Key ToggleKeyAlt => Controls.HoldAlt;

        protected override RectTransform Build(Transform canvas)
        {
            RectTransform paper = UiFactory.Window(canvas, "Hold", new Vector2(1400f, 860f), "Трюм", out _);

            grid = UiFactory.Rect(paper, "Grid");
            grid.anchorMin = grid.anchorMax = new Vector2(0f, 1f);
            grid.pivot = new Vector2(0f, 1f);
            grid.anchoredPosition = new Vector2(70f, -110f);
            cellsLayer = UiFactory.Stretch(UiFactory.Rect(grid, "Cells"));
            itemsLayer = UiFactory.Stretch(UiFactory.Rect(grid, "Items"));

            RectTransform side = UiFactory.Rect(paper, "Side");
            UiFactory.Place(side, new Vector2(1f, 1f), new Vector2(620f, 700f), new Vector2(-50f, -110f));

            info = UiFactory.Label(side, "", 30, TextAnchor.UpperLeft, UiFactory.Theme.Ink);
            UiFactory.Place(info.rectTransform, new Vector2(0f, 1f), new Vector2(620f, 260f), Vector2.zero);
            info.supportRichText = true;

            throwButton = UiFactory.Button(side, "Выбросить за борт", AskThrowSelected, new Vector2(380f, 60f), 26);
            UiFactory.Place((RectTransform)throwButton.transform, new Vector2(0f, 1f), new Vector2(380f, 60f), new Vector2(0f, -270f));

            summary = UiFactory.Label(side, "", 28, TextAnchor.UpperLeft, UiFactory.Theme.Ink);
            UiFactory.Place(summary.rectTransform, new Vector2(0f, 1f), new Vector2(620f, 160f), new Vector2(0f, -360f));

            Text help = UiFactory.Label(side, "Тащи рыбу мышью. R во время перетаскивания: повернуть.\nБрось мимо сетки: выбросить за борт.\nПродать улов можно Тарасу на причале.",
                24, TextAnchor.UpperLeft, UiFactory.Theme.InkLight);
            UiFactory.Place(help.rectTransform, new Vector2(0f, 0f), new Vector2(620f, 130f), Vector2.zero);

            BuildConfirm(paper);
            return (RectTransform)paper.parent;
        }

        private void BuildConfirm(RectTransform paper)
        {
            Image back = UiFactory.Panel(paper, "Confirm", new Color(0.2f, 0.15f, 0.1f, 0.95f));
            confirm = back.rectTransform;
            UiFactory.Place(confirm, new Vector2(0.5f, 0.5f), new Vector2(700f, 240f), Vector2.zero);
            confirmText = UiFactory.HudLabel(confirm, "", 30, TextAnchor.MiddleCenter);
            UiFactory.Place(confirmText.rectTransform, new Vector2(0.5f, 1f), new Vector2(660f, 120f), new Vector2(0f, -10f));

            RectTransform row = UiFactory.Rect(confirm, "Buttons");
            UiFactory.Place(row, new Vector2(0.5f, 0f), new Vector2(660f, 70f), new Vector2(0f, 20f));
            UiFactory.Row(row, 30f);
            UiFactory.Button(row, "Выбросить", ConfirmThrow, new Vector2(260f, 60f));
            UiFactory.Button(row, "Оставить", () => { pendingThrow = null; confirm.gameObject.SetActive(false); }, new Vector2(260f, 60f));
            confirm.gameObject.SetActive(false);
        }

        protected override void Refresh()
        {
            InventoryGrid hold = Hold;
            if (hold == null)
                return;

            if (hold.Width != builtWidth || hold.Height != builtHeight)
                BuildCells(hold.Width, hold.Height);

            foreach (DraggableItem view in views)
                Destroy(view.gameObject);
            views.Clear();

            foreach (GridItem item in hold.Items)
                views.Add(CreateView(item));

            if (selected != null && !Contains(hold, selected))
                selected = null;

            UpdateInfo();
        }

        private static bool Contains(InventoryGrid hold, GridItem item)
        {
            foreach (GridItem i in hold.Items)
                if (i == item) return true;
            return false;
        }

        private void BuildCells(int width, int height)
        {
            foreach (Transform child in cellsLayer)
                Destroy(child.gameObject);

            grid.sizeDelta = new Vector2(width * Cell, height * Cell);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Image cell = UiFactory.Panel(cellsLayer, $"Cell {x},{y}", new Color(0.55f, 0.47f, 0.36f, 1f));
                    RectTransform r = cell.rectTransform;
                    r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
                    r.pivot = new Vector2(0f, 1f);
                    r.sizeDelta = new Vector2(Cell - 4f, Cell - 4f);
                    r.anchoredPosition = new Vector2(x * Cell + 2f, -y * Cell - 2f);
                    cell.raycastTarget = false;
                }
            }

            builtWidth = width;
            builtHeight = height;
        }

        private DraggableItem CreateView(GridItem item)
        {
            Image back = UiFactory.Panel(itemsLayer, item.catchInfo.Title, ItemColor(item, false));
            RectTransform r = back.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            back.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.5f);

            Image picture = UiFactory.Icon(back.transform, item.catchInfo.fish.sprite, Vector2.zero);
            picture.preserveAspect = false;

            var view = back.gameObject.AddComponent<DraggableItem>();
            view.Setup(this, item, back);
            Layout(view, item.position, item.rotated);
            return view;
        }

        // Поставить картинку предмета в клетку position (с поворотом или без).
        private void Layout(DraggableItem view, Vector2Int position, bool rotated)
        {
            GridItem item = view.Item;
            Vector2Int size = rotated ? new Vector2Int(item.baseSize.y, item.baseSize.x) : item.baseSize;
            var r = (RectTransform)view.transform;
            r.sizeDelta = new Vector2(size.x * Cell, size.y * Cell);
            r.anchoredPosition = new Vector2(position.x * Cell, -position.y * Cell);

            RectTransform picture = (RectTransform)r.Find("Icon");
            picture.anchorMin = picture.anchorMax = new Vector2(0.5f, 0.5f);
            picture.sizeDelta = new Vector2(item.baseSize.x * Cell - 8f, item.baseSize.y * Cell - 8f);
            picture.anchoredPosition = Vector2.zero;
            picture.localRotation = Quaternion.Euler(0f, 0f, rotated ? 90f : 0f);
        }

        private Color ItemColor(GridItem item, bool isSelected)
        {
            Color c = item.catchInfo.caughtAtNight ? new Color(0.3f, 0.35f, 0.6f, 0.9f) : new Color(0.85f, 0.8f, 0.65f, 0.9f);
            return isSelected ? Color.Lerp(c, UiFactory.Theme.Gold, 0.5f) : c;
        }

        // ---------- выбор и описание ----------

        public void Select(GridItem item)
        {
            selected = item;
            foreach (DraggableItem view in views)
                view.Tint(ItemColor(view.Item, view.Item == selected));
            UpdateInfo();
        }

        private void UpdateInfo()
        {
            InventoryGrid hold = Hold;
            throwButton.interactable = selected != null;

            if (selected != null)
            {
                CatchInfo c = selected.catchInfo;
                info.text = $"<b>{c.Title}</b>\nВес: {c.weight:0.00} кг\nРедкость: {FishData.RarityName(c.fish.rarity)}\n" +
                            $"Цена: {c.price} мон.{(c.caughtAtNight ? " (ночной x3)" : "")}\nПоймана в день {c.day}";
            }
            else
            {
                info.text = "Нажми на рыбу, чтобы посмотреть.";
            }

            if (hold != null)
                summary.text = $"Трюм {hold.Width}x{hold.Height}, свободно клеток: {hold.FreeCells}\n" +
                               $"Рыб: {hold.Count}\nВесь улов стоит: {hold.TotalPrice} мон.";
        }

        // ---------- перетаскивание ----------

        public bool ScreenToGrid(Vector2 screen, out Vector2 local)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(grid, screen, null, out local);
            return local.x >= 0f && local.y <= 0f && local.x <= grid.sizeDelta.x && -local.y <= grid.sizeDelta.y;
        }

        public void BeginDrag(DraggableItem view) => dragging = view;

        protected override void Update()
        {
            base.Update();

            if (dragging != null && Controls.Pressed(Controls.Rotate))
            {
                dragging.PreviewRotated = !dragging.PreviewRotated;
                Vector2Int cell = dragging.HoverCell(Cell);
                Layout(dragging, cell, dragging.PreviewRotated);
                UpdateDragPreview(dragging);
            }
        }

        public void UpdateDragPreview(DraggableItem view)
        {
            GridItem item = view.Item;
            Vector2Int size = view.PreviewRotated ? new Vector2Int(item.baseSize.y, item.baseSize.x) : item.baseSize;
            bool ok = Hold.CanPlace(view.HoverCell(Cell), size, item);
            view.Tint(ok ? new Color(0.5f, 0.85f, 0.5f, 0.9f) : new Color(0.9f, 0.4f, 0.35f, 0.9f));
        }

        public void EndDrag(DraggableItem view, bool droppedInsideGrid)
        {
            dragging = null;
            GridItem item = view.Item;

            if (!droppedInsideGrid)
            {
                Layout(view, item.position, item.rotated);
                AskThrow(item);
                Select(item);
                return;
            }

            if (!Hold.TryMove(item, view.HoverCell(Cell), view.PreviewRotated))
                Notifications.Show("Сюда не помещается.");

            Refresh();
            Select(item);
        }

        // ---------- выброс за борт ----------

        private void AskThrowSelected()
        {
            if (selected != null)
                AskThrow(selected);
        }

        private void AskThrow(GridItem item)
        {
            pendingThrow = item;
            confirmText.text = $"Выбросить «{item.catchInfo.Title}» ({item.catchInfo.weight:0.0} кг) за борт?";
            confirm.gameObject.SetActive(true);
            confirm.SetAsLastSibling();
        }

        private void ConfirmThrow()
        {
            confirm.gameObject.SetActive(false);
            if (pendingThrow == null)
                return;

            Hold.Remove(pendingThrow);
            Notifications.Show($"{pendingThrow.catchInfo.Title} за бортом.");
            GameEvents.RaisePlaySound("splash");
            pendingThrow = null;
            selected = null;
            Refresh();
        }
    }
}
