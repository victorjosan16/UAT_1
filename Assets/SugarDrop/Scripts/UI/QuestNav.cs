using System;
using System.Collections.Generic;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Bara de jos în stilul Drop Quest (pe meniu și pe hartă): o scândură de lemn pe toată lățimea, cu butoane
    /// rotunde colorate, numele sub fiecare și un punct roșu când e ceva nou. Butonul ecranului deschis e mai mare
    /// și are un inel auriu. Doar funcții care există în joc (fără magazin).
    /// </summary>
    public sealed class QuestNav
    {
        public const float Height = 196f;
        private const float ButtonSize = 112f;

        private sealed class Item
        {
            public RectTransform Button;
            public UiLabel Caption;
            public GameObject Badge;
            public string Key;
        }

        private readonly List<Item> _items = new();

        public RectTransform Root { get; }

        /// <param name="selected">Indexul ecranului deschis (−1 = niciunul).</param>
        public QuestNav(Transform parent, float bottom, int selected, params (string key, Sprite icon, Color color, Action onClick)[] items)
        {
            Root = UiFactory.CreateRect("BottomNav", parent);
            Root.anchorMin = new Vector2(0f, 0f);
            Root.anchorMax = new Vector2(1f, 0f);
            Root.pivot = new Vector2(0.5f, 0f);
            Root.anchoredPosition = new Vector2(0f, bottom);
            Root.sizeDelta = new Vector2(0f, Height);

            // Scândura: muchia închisă sus, lemnul dedesubt (se întinde și sub marginea ecranului).
            Image plank = UiFactory.CreatePanel("Plank", Root, QuestStyle.Wood);
            UiFactory.Stretch((RectTransform)plank.transform);
            ((RectTransform)plank.transform).offsetMin = new Vector2(0f, -bottom - 40f);
            ((RectTransform)plank.transform).offsetMax = new Vector2(0f, -20f);
            plank.raycastTarget = true;
            Image edge = UiFactory.CreatePanel("Edge", Root, QuestStyle.WoodDeep);
            UiFactory.Place((RectTransform)edge.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(3000f, 12f));

            float spacing = 1080f / items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                bool current = i == selected;
                Action onClick = items[i].onClick;
                Button button = SugarUi.IconButton(items[i].key, Root, items[i].icon, items[i].color, () =>
                {
                    SugarUi.Click();
                    onClick?.Invoke();
                }, 0.56f);
                float size = current ? ButtonSize + 20f : ButtonSize;
                var rect = UiFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2((i - (items.Length - 1) * 0.5f) * spacing, current ? -58f : -70f), new Vector2(size, size));
                if (current)
                {
                    Image ring = UiFactory.CreateImage("Ring", rect, SugarSprites.Ring, QuestStyle.Gold);
                    UiFactory.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size + 22f, size + 22f));
                    ring.raycastTarget = false;
                    ring.transform.SetAsFirstSibling();
                }
                UiLabel caption = SugarUi.Outlined("Caption", Root, "", 28, Color.white, 2.5f);
                UiFactory.Place(caption.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f),
                    new Vector2((i - (items.Length - 1) * 0.5f) * spacing, -152f), new Vector2(spacing, 40f));
                Image badge = UiFactory.CreateImage("Badge", rect, SugarSprites.Circle, QuestStyle.Red);
                UiFactory.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-12f, -12f), new Vector2(36f, 36f));
                badge.gameObject.SetActive(false);
                _items.Add(new Item { Button = rect, Caption = caption, Badge = badge.gameObject, Key = items[i].key });
            }
            Translate();
        }

        public void SetBadge(int index, bool on) => _items[index].Badge.SetActive(on);

        public RectTransform ButtonAt(int index) => _items[index].Button;

        public void Translate()
        {
            foreach (Item item in _items) item.Caption.Text = SugarLoc.T(item.Key);
        }
    }
}
