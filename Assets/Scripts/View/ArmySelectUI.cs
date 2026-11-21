using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HeroesBattle
{
    public class ArmySelectUI : MonoBehaviour
    {
        public Action<ArmySetup> OnStart;
        public Action OnBack;

        class RowRefs
        {
            public UnitDef Def;
            public Text Count;
            public Button Plus;
            public Button Minus;
            public Image Background;
        }

        Canvas canvas;
        readonly Dictionary<UnitId, int> counts = new Dictionary<UnitId, int>();
        readonly List<RowRefs> rows = new List<RowRefs>();
        readonly List<Button> difficultyButtons = new List<Button>();
        readonly List<Button> heroButtons = new List<Button>();

        Text budgetText;
        Text summaryText;
        Text hintText;
        Button startButton;
        HeroDef hero;
        Difficulty difficulty = Difficulty.Normal;

        public void Build()
        {
            hero = HeroDb.PlayerHeroes[0];

            canvas = UiKit.CreateCanvas("ArmyCanvas", 6, transform);
            var root = (RectTransform)canvas.transform;

            var bg = UiKit.Img(root, "Background", ProceduralArt.Background, Color.white, false);
            UiKit.Fill(bg.rectTransform);
            var dim = UiKit.Img(root, "Dim", ProceduralArt.WhitePixel, new Color(0.02f, 0.02f, 0.02f, 0.55f), false);
            UiKit.Fill(dim.rectTransform);

            var title = UiKit.Label(root, "Title", "СБОР АРМИИ", 42, UiKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(title.rectTransform, 40f, 22f, 500f, 50f);

            var subtitle = UiKit.Label(root, "Subtitle",
                "До 4 отрядов · бюджет " + ArmyBuilder.Budget + " золота · противник получит армию по силе вашей",
                16, UiKit.TextDim, TextAnchor.MiddleLeft);
            UiKit.Place(subtitle.rectTransform, 42f, 70f, 880f, 24f);

            var random = UiKit.TextButton(root, "Random", "Случайно", 14,
                new Color(0.20f, 0.23f, 0.30f), UiKit.TextMain, Randomize);
            UiKit.Place(random.image.rectTransform, 646f, 42f, 130f, 38f);

            var clear = UiKit.TextButton(root, "Clear", "Очистить", 14,
                new Color(0.24f, 0.20f, 0.18f), UiKit.TextMain, ClearAll);
            UiKit.Place(clear.image.rectTransform, 786f, 42f, 130f, 38f);

            BuildRows(root);
            BuildRightPanel(root);

            Refresh();
        }

        void BuildRows(RectTransform root)
        {
            float top = 108f;
            float rowHeight = 86f;
            float step = 92f;

            for (int i = 0; i < UnitDb.PlayerPool.Count; i++)
            {
                var def = UnitDb.PlayerPool[i];
                var rowPanel = UiKit.Panel(root, "Row_" + def.Id, new Color(0.095f, 0.085f, 0.068f, 0.95f));
                UiKit.Place(rowPanel.rectTransform, 40f, top + i * step, 880f, rowHeight);

                var badge = UiKit.UnitBadge(rowPanel.transform, "Badge", def, 62f);
                UiKit.PlaceBottom((RectTransform)badge.transform, 10f, 12f, 62f, 62f);

                var name = UiKit.Label(rowPanel.transform, "Name", def.Name, 19, UiKit.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
                UiKit.Place(name.rectTransform, 86f, 8f, 420f, 26f);

                string stats = "HP " + def.Hp + "   Урон " + def.MinDamage + "–" + def.MaxDamage +
                               "   Атака " + def.Attack + "   Защита " + def.Defense +
                               "   Скорость " + def.Speed;
                if (def.Ranged) stats += "   Патроны " + def.Shots;
                if (def.Flying) stats += "   Летающий";
                stats += "   Цена " + def.Cost;

                var statsLabel = UiKit.Label(rowPanel.transform, "Stats", stats, 13, UiKit.TextDim);
                UiKit.Place(statsLabel.rectTransform, 86f, 36f, 600f, 20f);

                var desc = UiKit.Label(rowPanel.transform, "Desc", def.Description, 12, new Color(0.62f, 0.60f, 0.55f));
                UiKit.Place(desc.rectTransform, 86f, 56f, 590f, 24f);

                var minus = UiKit.TextButton(rowPanel.transform, "Minus", "−", 24,
                    new Color(0.22f, 0.20f, 0.17f), UiKit.TextMain, () => AddUnit(def.Id, -1));
                UiKit.PlaceBottom(minus.image.rectTransform, 700f, 22f, 42f, 42f);

                var count = UiKit.Label(rowPanel.transform, "Count", "0", 20, UiKit.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
                UiKit.PlaceBottom(count.rectTransform, 748f, 22f, 58f, 42f);

                var plus = UiKit.TextButton(rowPanel.transform, "Plus", "+", 24,
                    new Color(0.24f, 0.30f, 0.20f), UiKit.TextMain, () => AddUnit(def.Id, 1));
                UiKit.PlaceBottom(plus.image.rectTransform, 812f, 22f, 42f, 42f);

                rows.Add(new RowRefs { Def = def, Count = count, Plus = plus, Minus = minus, Background = rowPanel });
            }
        }

        void BuildRightPanel(RectTransform root)
        {
            var panel = UiKit.Panel(root, "SummaryPanel", new Color(0.095f, 0.085f, 0.068f, 0.97f));
            UiKit.Place(panel.rectTransform, 948f, 104f, 612f, 728f);

            var title = UiKit.Label(panel.transform, "Title", "Ваша армия", 22, UiKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(title.rectTransform, 18f, 14f, 300f, 30f);

            budgetText = UiKit.Label(panel.transform, "Budget", "", 18, UiKit.TextMain, TextAnchor.MiddleRight, FontStyle.Bold);
            UiKit.PlaceRight(budgetText.rectTransform, 18f, 16f, 280f, 26f);

            summaryText = UiKit.Label(panel.transform, "Summary", "", 14, UiKit.TextMain, TextAnchor.UpperLeft);
            UiKit.Place(summaryText.rectTransform, 18f, 52f, 576f, 200f);

            var diffLabel = UiKit.Label(panel.transform, "DiffLabel", "Сложность", 15, UiKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(diffLabel.rectTransform, 18f, 262f, 300f, 22f);

            string[] diffNames = { "Легко", "Норма", "Сложно" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var button = UiKit.TextButton(panel.transform, "Diff" + i, diffNames[i], 15,
                    new Color(0.16f, 0.17f, 0.20f), UiKit.TextMain, () =>
                    {
                        difficulty = (Difficulty)index;
                        Refresh();
                    });
                UiKit.Place(button.image.rectTransform, 18f + i * 190f, 288f, 180f, 42f);
                difficultyButtons.Add(button);
            }

            var heroLabel = UiKit.Label(panel.transform, "HeroLabel", "Герой", 15, UiKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(heroLabel.rectTransform, 18f, 344f, 300f, 22f);

            for (int i = 0; i < HeroDb.PlayerHeroes.Length; i++)
            {
                var heroDef = HeroDb.PlayerHeroes[i];
                string caption = heroDef.Name + "\n" + heroDef.BonusText;
                var button = UiKit.TextButton(panel.transform, "Hero" + i, caption, 13,
                    new Color(0.16f, 0.17f, 0.20f), UiKit.TextMain, () =>
                    {
                        hero = heroDef;
                        Refresh();
                    });
                UiKit.Place(button.image.rectTransform, 18f + i * 292f, 370f, 280f, 80f);
                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.alignment = TextAnchor.MiddleCenter;
                    label.fontSize = 13;
                    label.lineSpacing = 1.1f;
                }

                heroButtons.Add(button);
            }

            startButton = UiKit.TextButton(panel.transform, "Start", "В БОЙ", 24,
                new Color(0.30f, 0.40f, 0.24f), UiKit.TextMain, StartBattle);
            UiKit.Place(startButton.image.rectTransform, 18f, 486f, 576f, 62f);

            var back = UiKit.TextButton(panel.transform, "Back", "Назад", 17,
                new Color(0.22f, 0.19f, 0.17f), UiKit.TextDim, () => { if (OnBack != null) OnBack(); });
            UiKit.Place(back.image.rectTransform, 18f, 560f, 576f, 46f);

            hintText = UiKit.Label(panel.transform, "Hint", "", 12, UiKit.TextDim, TextAnchor.UpperLeft);
            UiKit.Place(hintText.rectTransform, 18f, 622f, 576f, 90f);
        }

        public void AddUnit(UnitId id, int delta)
        {
            int current;
            counts.TryGetValue(id, out current);
            int next = Mathf.Max(0, current + delta);
            var def = UnitDb.Get(id);

            if (next > current)
            {
                if (current == 0 && SelectedStacks() >= ArmyBuilder.MaxStacks)
                {
                    Sfx.Play(SfxId.Error, 0.6f);
                    return;
                }

                if (TotalCost() + (next - current) * def.Cost > ArmyBuilder.Budget)
                {
                    Sfx.Play(SfxId.Error, 0.6f);
                    return;
                }
            }

            bool changed = next != current;
            if (next == 0) counts.Remove(id);
            else counts[id] = next;

            if (changed) Sfx.Play(SfxId.Turn, 0.4f);
            Refresh();
        }

        void Randomize()
        {
            counts.Clear();
            var specs = ArmyBuilder.RandomPlayerArmy(new System.Random());
            for (int i = 0; i < specs.Count; i++)
            {
                counts[specs[i].Unit] = specs[i].Count;
            }

            Refresh();
        }

        void ClearAll()
        {
            counts.Clear();
            Refresh();
        }

        int TotalCost()
        {
            int sum = 0;
            foreach (var pair in counts) sum += UnitDb.Get(pair.Key).Cost * pair.Value;
            return sum;
        }

        int SelectedStacks()
        {
            int n = 0;
            foreach (var pair in counts) if (pair.Value > 0) n++;
            return n;
        }

        void Refresh()
        {
            int total = TotalCost();
            UiKit.SetText(budgetText, "Золото: " + total + " / " + ArmyBuilder.Budget);
            budgetText.color = total > ArmyBuilder.Budget ? new Color(0.95f, 0.40f, 0.35f) : UiKit.Gold;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Отрядов: " + SelectedStacks() + " / " + ArmyBuilder.MaxStacks);
            sb.AppendLine();

            if (counts.Count == 0)
            {
                sb.AppendLine("Добавьте отряды кнопками «+».");
                sb.AppendLine("Стрелки бьют издалека, летающие");
                sb.AppendLine("игнорируют препятствия.");
                sb.AppendLine();
                sb.AppendLine("Совет: 2–3 вида войск обычно");
                sb.AppendLine("сильнее одного дорогого отряда.");
            }
            else
            {
                foreach (var def in UnitDb.PlayerPool)
                {
                    int count;
                    if (!counts.TryGetValue(def.Id, out count) || count <= 0) continue;
                    sb.AppendLine(count + " × " + def.Name + " — " + (def.Cost * count) + " зол.");
                }

                sb.AppendLine();
                sb.AppendLine("Итого: " + total + " зол.");
            }

            UiKit.SetText(summaryText, sb.ToString());

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                int count;
                counts.TryGetValue(row.Def.Id, out count);
                UiKit.SetText(row.Count, count.ToString());
                row.Background.color = count > 0
                    ? new Color(0.16f, 0.17f, 0.11f, 0.98f)
                    : new Color(0.095f, 0.085f, 0.068f, 0.95f);

                bool canAdd = count > 0
                    ? total + row.Def.Cost <= ArmyBuilder.Budget
                    : SelectedStacks() < ArmyBuilder.MaxStacks && total + row.Def.Cost <= ArmyBuilder.Budget;
                row.Plus.interactable = canAdd;
                row.Minus.interactable = count > 0;
            }

            for (int i = 0; i < difficultyButtons.Count; i++)
            {
                difficultyButtons[i].image.color = (int)difficulty == i
                    ? new Color(0.55f, 0.45f, 0.20f)
                    : new Color(0.16f, 0.17f, 0.20f);
            }

            for (int i = 0; i < heroButtons.Count; i++)
            {
                heroButtons[i].image.color = hero == HeroDb.PlayerHeroes[i]
                    ? new Color(0.30f, 0.42f, 0.58f)
                    : new Color(0.16f, 0.17f, 0.20f);
            }

            if (startButton != null) startButton.interactable = SelectedStacks() > 0;

            string hint = "Противник: ";
            switch (difficulty)
            {
                case Difficulty.Easy:
                    hint += "равный по стоимости гарнизон (100%).";
                    break;
                case Difficulty.Hard:
                    hint += "усиленная орда (130% вашей силы).";
                    break;
                default:
                    hint += "орда немного сильнее (110%).";
                    break;
            }

            hint += "\n\nУправление в бою: ЛКМ — выбор/движение/атака, ПКМ — отмена, 1–3 — заклинания.";
            UiKit.SetText(hintText, hint);
        }

        void StartBattle()
        {
            var setup = new ArmySetup
            {
                PlayerHero = hero,
                Difficulty = difficulty
            };

            foreach (var def in UnitDb.PlayerPool)
            {
                int count;
                if (counts.TryGetValue(def.Id, out count) && count > 0)
                {
                    setup.PlayerArmy.Add(new ArmyStackSpec { Unit = def.Id, Count = count });
                }
            }

            if (setup.PlayerArmy.Count == 0) return;
            if (OnStart != null) OnStart(setup);
        }

        public void Dispose()
        {
            if (canvas != null) Destroy(canvas.gameObject);
        }
    }
}
