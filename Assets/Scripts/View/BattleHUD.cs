using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace HeroesBattle
{
    public class BattleHUD : MonoBehaviour
    {
        public BattleState State;
        public Action OnWait;
        public Action OnDefend;
        public Action OnSkip;
        public Action<int> OnSpell;
        public Action OnRestart;
        public Action OnMenu;

        Canvas canvas;
        RectTransform root;

        Text roundText;
        Text playerHeroName;
        Text playerHeroInfo;
        Text enemyHeroName;
        Text enemyHeroInfo;
        Text hintText;
        Text logText;
        Text cardName;
        Text cardSub;
        Text cardStats;
        Text cardHpText;
        Text soundLabel;

        Image playerManaFill;
        Image enemyManaFill;
        Image portraitBg;
        Image portraitIcon;
        Image cardHpFill;

        RectTransform queueRoot;
        readonly List<GameObject> queueChips = new List<GameObject>();

        RectTransform playerRosterRoot;
        RectTransform enemyRosterRoot;
        Text playerHeroSide;
        Text enemyHeroSide;
        Text playerManaSide;
        Text enemyManaSide;
        readonly List<GameObject> rosterRows = new List<GameObject>();

        Button waitButton;
        Button defendButton;
        Button skipButton;
        readonly Button[] spellButtons = new Button[3];
        readonly Text[] spellLabels = new Text[3];

        RectTransform tooltipGroup;
        Text tooltipText;

        GameObject resultOverlay;
        Text resultTitle;
        Text resultInfo;

        SpellId? pendingSpell;
        UnitStack lastTooltipStack;

        public void Build(BattleState state)
        {
            State = state;

            canvas = UiKit.CreateCanvas("BattleCanvas", 10, transform);
            root = (RectTransform)canvas.transform;

            BuildTopBar();
            BuildBottomBar();
            BuildSidePanels();
            BuildTooltip();
            BuildResultOverlay();

            Refresh();
        }

        // ------------------------------------------------------------------ build

        void BuildTopBar()
        {
            var bar = UiKit.Panel(root, "TopBar", new Color(0.085f, 0.075f, 0.060f, 0.96f));
            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -80f);
            rt.offsetMax = Vector2.zero;

            playerHeroName = UiKit.Label(bar.transform, "PlayerHeroName", "Герой", 20, UiKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(playerHeroName.rectTransform, 18f, 8f, 340f, 26f);

            playerHeroInfo = UiKit.Label(bar.transform, "PlayerHeroInfo", "Мана 0/0", 15, new Color(0.68f, 0.80f, 0.95f));
            UiKit.Place(playerHeroInfo.rectTransform, 18f, 36f, 340f, 22f);

            var manaBg = UiKit.Panel(bar.transform, "PlayerManaBg", new Color(0.05f, 0.05f, 0.05f, 0.9f));
            UiKit.Place(manaBg.rectTransform, 18f, 62f, 220f, 9f);

            playerManaFill = UiKit.Img(manaBg.transform, "Fill", ProceduralArt.RoundedRect, new Color(0.35f, 0.60f, 0.95f));
            playerManaFill.type = Image.Type.Filled;
            playerManaFill.fillMethod = Image.FillMethod.Horizontal;
            UiKit.Stretch(playerManaFill.rectTransform, 1f, 1f, 1f, 1f);

            enemyHeroName = UiKit.Label(bar.transform, "EnemyHeroName", "Враг", 20, new Color(0.95f, 0.60f, 0.52f), TextAnchor.MiddleRight, FontStyle.Bold);
            UiKit.PlaceRight(enemyHeroName.rectTransform, 18f, 8f, 340f, 26f);

            enemyHeroInfo = UiKit.Label(bar.transform, "EnemyHeroInfo", "Мана 0/0", 15, new Color(0.95f, 0.72f, 0.62f), TextAnchor.MiddleRight);
            UiKit.PlaceRight(enemyHeroInfo.rectTransform, 18f, 36f, 340f, 22f);

            var enemyManaBg = UiKit.Panel(bar.transform, "EnemyManaBg", new Color(0.05f, 0.05f, 0.05f, 0.9f));
            UiKit.PlaceRight(enemyManaBg.rectTransform, 18f, 62f, 220f, 9f);

            enemyManaFill = UiKit.Img(enemyManaBg.transform, "Fill", ProceduralArt.RoundedRect, new Color(0.95f, 0.45f, 0.40f));
            enemyManaFill.type = Image.Type.Filled;
            enemyManaFill.fillMethod = Image.FillMethod.Horizontal;
            UiKit.Stretch(enemyManaFill.rectTransform, 1f, 1f, 1f, 1f);

            roundText = UiKit.Label(bar.transform, "RoundText", "Раунд 1", 18, UiKit.TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(roundText.rectTransform, 660f, 6f, 280f, 24f);

            var queueGroup = UiKit.Group(bar.transform, "Queue");
            UiKit.Place(queueGroup, 470f, 32f, 660f, 44f);
            queueRoot = queueGroup;

            var sound = UiKit.TextButton(bar.transform, "SoundButton", Sfx.Label, 10,
                new Color(0.17f, 0.15f, 0.12f), UiKit.TextDim, ToggleSound);
            UiKit.Place(sound.image.rectTransform, 1140f, 44f, 96f, 26f);
            soundLabel = sound.GetComponentInChildren<Text>();
            RefreshSoundLabel();
        }

        void ToggleSound()
        {
            Sfx.ToggleMute();
            RefreshSoundLabel();
        }

        void RefreshSoundLabel()
        {
            if (soundLabel != null) soundLabel.text = Sfx.Label;
        }

        void OnEnable()
        {
            Sfx.MuteChanged += RefreshSoundLabel;
        }

        void OnDisable()
        {
            Sfx.MuteChanged -= RefreshSoundLabel;
        }

        void BuildBottomBar()
        {
            var bar = UiKit.Panel(root, "BottomBar", new Color(0.085f, 0.075f, 0.060f, 0.97f));
            var rt = bar.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(0f, 0f);
            rt.offsetMax = new Vector2(0f, 172f);

            var card = UiKit.Panel(bar.transform, "Card", new Color(0.13f, 0.115f, 0.09f, 0.95f));
            UiKit.PlaceBottom(card.rectTransform, 12f, 8f, 460f, 156f);

            var portrait = UiKit.Group(card.transform, "Portrait");
            UiKit.Place(portrait, 16f, 18f, 74f, 74f);

            portraitBg = UiKit.Img(portrait, "Bg", ProceduralArt.Circle, new Color(0.3f, 0.3f, 0.3f));
            UiKit.Fill(portraitBg.rectTransform);

            portraitIcon = UiKit.Img(portrait, "Icon", ProceduralArt.SwordIcon, new Color(1f, 1f, 1f, 0.92f));
            UiKit.Stretch(portraitIcon.rectTransform, 16f, 16f, 16f, 16f);

            cardName = UiKit.Label(card.transform, "Name", "Отряд", 20, UiKit.TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(cardName.rectTransform, 102f, 12f, 340f, 26f);

            cardSub = UiKit.Label(card.transform, "Sub", "", 14, UiKit.TextDim);
            UiKit.Place(cardSub.rectTransform, 102f, 38f, 340f, 20f);

            cardHpText = UiKit.Label(card.transform, "HpText", "", 13, new Color(0.75f, 0.85f, 0.70f));
            UiKit.Place(cardHpText.rectTransform, 102f, 62f, 340f, 18f);

            var hpBg = UiKit.Panel(card.transform, "HpBg", new Color(0.05f, 0.05f, 0.05f, 0.9f));
            UiKit.Place(hpBg.rectTransform, 102f, 82f, 340f, 12f);

            cardHpFill = UiKit.Img(hpBg.transform, "Fill", ProceduralArt.RoundedRect, new Color(0.35f, 0.78f, 0.35f));
            cardHpFill.type = Image.Type.Filled;
            cardHpFill.fillMethod = Image.FillMethod.Horizontal;
            UiKit.Stretch(cardHpFill.rectTransform, 1f, 1f, 1f, 1f);

            cardStats = UiKit.Label(card.transform, "Stats", "", 14, UiKit.TextMain);
            UiKit.Place(cardStats.rectTransform, 16f, 104f, 428f, 46f);

            waitButton = UiKit.TextButton(bar.transform, "WaitButton", "Ждать (Space)", 15,
                new Color(0.22f, 0.26f, 0.20f), UiKit.TextMain, () => { if (OnWait != null) OnWait(); });
            UiKit.PlaceBottom(waitButton.image.rectTransform, 486f, 100f, 156f, 44f);

            defendButton = UiKit.TextButton(bar.transform, "DefendButton", "Защита (D)", 15,
                new Color(0.20f, 0.24f, 0.30f), UiKit.TextMain, () => { if (OnDefend != null) OnDefend(); });
            UiKit.PlaceBottom(defendButton.image.rectTransform, 652f, 100f, 156f, 44f);

            skipButton = UiKit.TextButton(bar.transform, "SkipButton", "Пропуск (P)", 15,
                new Color(0.26f, 0.20f, 0.20f), UiKit.TextMain, () => { if (OnSkip != null) OnSkip(); });
            UiKit.PlaceBottom(skipButton.image.rectTransform, 818f, 100f, 156f, 44f);

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var spell = Spellbook.All[i];
                var button = UiKit.TextButton(bar.transform, "SpellButton" + i, LabelFor(spell, i), 14,
                    new Color(0.16f, 0.17f, 0.23f), new Color(0.82f, 0.88f, 0.98f),
                    () => { if (OnSpell != null) OnSpell(index); });
                UiKit.PlaceBottom(button.image.rectTransform, 486f + i * 166f, 48f, 156f, 44f);
                spellButtons[i] = button;
                spellLabels[i] = button.GetComponentInChildren<Text>();
            }

            hintText = UiKit.Label(bar.transform, "Hint", "", 14, UiKit.TextDim);
            UiKit.PlaceBottom(hintText.rectTransform, 486f, 12f, 592f, 28f);

            var logPanel = UiKit.Panel(bar.transform, "LogPanel", new Color(0.11f, 0.10f, 0.082f, 0.95f));
            UiKit.PlaceBottom(logPanel.rectTransform, 1094f, 8f, 494f, 156f);

            var logTitle = UiKit.Label(logPanel.transform, "LogTitle", "Журнал боя", 13, UiKit.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(logTitle.rectTransform, 12f, 6f, 300f, 20f);

            logText = UiKit.Label(logPanel.transform, "LogText", "", 12, new Color(0.80f, 0.78f, 0.72f), TextAnchor.UpperLeft);
            UiKit.Place(logText.rectTransform, 12f, 28f, 470f, 120f);
        }

        static string LabelFor(SpellDef spell, int index)
        {
            return (index + 1) + " · " + spell.ShortName + " (" + spell.Cost + ")";
        }

        void BuildSidePanels()
        {
            BuildRosterPanel("PlayerRoster", 8f, true, out playerRosterRoot, out playerHeroSide, out playerManaSide);
            BuildRosterPanel("EnemyRoster", 1384f, false, out enemyRosterRoot, out enemyHeroSide, out enemyManaSide);
        }

        void BuildRosterPanel(string name, float x, bool player, out RectTransform listRoot, out Text heroLabel, out Text manaLabel)
        {
            var panel = UiKit.Panel(root, name, new Color(0.085f, 0.075f, 0.06f, 0.92f));
            UiKit.Place(panel.rectTransform, x, 130f, 208f, 372f);

            var title = UiKit.Label(panel.transform, "Title", player ? "Замок" : "Орда", 15,
                player ? new Color(0.56f, 0.76f, 0.96f) : new Color(0.96f, 0.56f, 0.48f),
                TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(title.rectTransform, 0f, 8f, 208f, 22f);

            heroLabel = UiKit.Label(panel.transform, "Hero", "", 13, UiKit.Gold, TextAnchor.MiddleCenter);
            UiKit.Place(heroLabel.rectTransform, 8f, 32f, 192f, 18f);

            manaLabel = UiKit.Label(panel.transform, "Mana", "", 11, UiKit.TextDim, TextAnchor.MiddleCenter);
            UiKit.Place(manaLabel.rectTransform, 8f, 50f, 192f, 16f);

            var divider = UiKit.Img(panel.transform, "Divider", ProceduralArt.WhitePixel, new Color(0.95f, 0.78f, 0.42f, 0.25f), false);
            UiKit.Place(divider.rectTransform, 16f, 70f, 176f, 1f);

            var list = UiKit.Group(panel.transform, "List");
            UiKit.Place(list, 0f, 76f, 208f, 290f);

            var spellHint = UiKit.Label(panel.transform, "SpellHint", "Магия героя: 1 · 2 · 3", 10, UiKit.TextDim, TextAnchor.MiddleCenter);
            UiKit.Place(spellHint.rectTransform, 0f, 342f, 208f, 16f);

            listRoot = list;
        }

        void RefreshRoster(Side side, RectTransform container)
        {
            if (container == null) return;

            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }

            float y = 0f;
            for (int i = 0; i < State.Stacks.Count; i++)
            {
                var stack = State.Stacks[i];
                if (stack.Side != side) continue;

                bool isCurrent = State.Current == stack;
                var row = UiKit.Panel(container, "Row" + i,
                    isCurrent ? new Color(0.32f, 0.27f, 0.13f, 0.95f) : new Color(0.13f, 0.12f, 0.095f, 0.9f));
                UiKit.Place(row.rectTransform, 0f, y, 208f, 58f);

                var badge = UiKit.UnitBadge(row.transform, "Badge", stack.Def, 34f);
                UiKit.PlaceBottom((RectTransform)badge.transform, 8f, 12f, 34f, 34f);

                var name = UiKit.Label(row.transform, "Name", stack.Def.Name, 12,
                    stack.Alive ? UiKit.TextMain : new Color(0.45f, 0.43f, 0.40f), TextAnchor.MiddleLeft);
                UiKit.Place(name.rectTransform, 50f, 4f, 150f, 18f);

                string countText = stack.Alive ? stack.Count.ToString() : "погиб";
                Color countColor = stack.Alive
                    ? (side == Side.Player ? new Color(0.62f, 0.80f, 0.98f) : new Color(0.98f, 0.66f, 0.58f))
                    : new Color(0.5f, 0.45f, 0.42f);
                var count = UiKit.Label(row.transform, "Count", countText, 12, countColor, TextAnchor.MiddleLeft, FontStyle.Bold);
                UiKit.Place(count.rectTransform, 50f, 22f, 80f, 16f);

                var bar = UiKit.Panel(row.transform, "HpBg", new Color(0.05f, 0.05f, 0.05f, 0.9f));
                UiKit.Place(bar.rectTransform, 50f, 44f, 146f, 6f);

                var fill = UiKit.Img(bar.transform, "Fill", ProceduralArt.RoundedRect,
                    stack.HpRatio > 0.5f ? new Color(0.35f, 0.78f, 0.35f) : new Color(0.85f, 0.45f, 0.25f));
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                UiKit.Stretch(fill.rectTransform, 1f, 1f, 1f, 1f);
                fill.fillAmount = stack.HpRatio;

                y += 62f;
            }
        }

        void BuildTooltip()
        {
            var group = UiKit.Group(root, "Tooltip");
            tooltipGroup = group;
            group.anchorMin = new Vector2(0.5f, 0.5f);
            group.anchorMax = new Vector2(0.5f, 0.5f);
            group.pivot = new Vector2(0f, 1f);
            group.sizeDelta = new Vector2(330f, 206f);

            var bg = UiKit.Panel(group, "Bg", new Color(0.08f, 0.07f, 0.055f, 0.97f), false);
            UiKit.Fill(bg.rectTransform);

            tooltipText = UiKit.Label(group, "Text", "", 13, UiKit.TextMain, TextAnchor.UpperLeft);
            UiKit.Stretch(tooltipText.rectTransform, 14f, 14f, 12f, 12f);

            group.gameObject.SetActive(false);
        }

        void BuildResultOverlay()
        {
            var overlay = UiKit.Group(root, "ResultOverlay");
            UiKit.Fill(overlay);
            resultOverlay = overlay.gameObject;

            var dim = UiKit.Img(overlay, "Dim", ProceduralArt.WhitePixel, new Color(0f, 0f, 0f, 0.78f), true);
            UiKit.Fill(dim.rectTransform);

            var panel = UiKit.Panel(overlay, "Panel", new Color(0.10f, 0.09f, 0.07f, 0.99f));
            UiKit.PlaceCenter(panel.rectTransform, 0f, 0f, 760f, 440f);

            resultTitle = UiKit.Label(panel.transform, "Title", "ПОБЕДА!", 44, UiKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(resultTitle.rectTransform, 0f, 26f, 760f, 56f);

            resultInfo = UiKit.Label(panel.transform, "Info", "", 15, UiKit.TextMain, TextAnchor.UpperCenter);
            UiKit.Place(resultInfo.rectTransform, 50f, 96f, 660f, 240f);

            var restart = UiKit.TextButton(panel.transform, "RestartButton", "Новый бой", 18,
                new Color(0.25f, 0.34f, 0.24f), UiKit.TextMain, () => { if (OnRestart != null) OnRestart(); });
            UiKit.PlaceBottom(restart.image.rectTransform, 120f, 30f, 240f, 54f);

            var menu = UiKit.TextButton(panel.transform, "MenuButton", "В меню", 18,
                new Color(0.26f, 0.23f, 0.18f), UiKit.TextMain, () => { if (OnMenu != null) OnMenu(); });
            UiKit.PlaceBottom(menu.image.rectTransform, 400f, 30f, 240f, 54f);

            resultOverlay.SetActive(false);
        }

        // ------------------------------------------------------------------ refresh

        public void Refresh()
        {
            if (State == null) return;

            lastTooltipStack = null;

            var current = State.Current;
            bool playerTurn = current != null && current.Side == Side.Player && !State.GameOver;

            UiKit.SetText(roundText, "Раунд " + State.Round);

            if (State.PlayerHero != null && State.PlayerHero.Def != null)
            {
                UiKit.SetText(playerHeroName, State.PlayerHero.Def.Name);
                UiKit.SetText(playerHeroInfo, "Мана " + State.PlayerHero.Mana + "/" + State.PlayerHero.ManaMax +
                                             "  ·  атака +" + State.PlayerHero.AttackBonus +
                                             (State.PlayerHero.CastThisRound ? "  ·  заклинание использовано" : ""));
                UiKit.SetBar(playerManaFill, State.PlayerHero.ManaMax > 0 ? (float)State.PlayerHero.Mana / State.PlayerHero.ManaMax : 0f);
            }

            if (State.EnemyHero != null && State.EnemyHero.Def != null)
            {
                UiKit.SetText(enemyHeroName, State.EnemyHero.Def.Name);
                UiKit.SetText(enemyHeroInfo, "Мана " + State.EnemyHero.Mana + "/" + State.EnemyHero.ManaMax +
                                              "  ·  атака +" + State.EnemyHero.AttackBonus);
                UiKit.SetBar(enemyManaFill, State.EnemyHero.ManaMax > 0 ? (float)State.EnemyHero.Mana / State.EnemyHero.ManaMax : 0f);
            }

            RefreshQueue();
            RefreshCard(current);

            if (playerHeroSide != null && State.PlayerHero != null && State.PlayerHero.Def != null)
            {
                UiKit.SetText(playerHeroSide, State.PlayerHero.Def.Name);
                UiKit.SetText(playerManaSide, "Мана " + State.PlayerHero.Mana + "/" + State.PlayerHero.ManaMax +
                                              (State.PlayerHero.CastThisRound ? " · ход магии сделан" : ""));
            }

            if (enemyHeroSide != null && State.EnemyHero != null && State.EnemyHero.Def != null)
            {
                UiKit.SetText(enemyHeroSide, State.EnemyHero.Def.Name);
                UiKit.SetText(enemyManaSide, "Мана " + State.EnemyHero.Mana + "/" + State.EnemyHero.ManaMax);
            }

            RefreshRoster(Side.Player, playerRosterRoot);
            RefreshRoster(Side.Enemy, enemyRosterRoot);

            if (waitButton != null) waitButton.interactable = playerTurn && current != null && !current.Waited;
            if (defendButton != null) defendButton.interactable = playerTurn;
            if (skipButton != null) skipButton.interactable = playerTurn;

            RefreshSpells(playerTurn);

            var sb = new StringBuilder();
            int start = Mathf.Max(0, State.Log.Count - 8);
            for (int i = start; i < State.Log.Count; i++) sb.AppendLine(State.Log[i]);
            UiKit.SetText(logText, sb.ToString());
        }

        void RefreshQueue()
        {
            for (int i = 0; i < queueChips.Count; i++)
            {
                if (queueChips[i] != null) Destroy(queueChips[i]);
            }

            queueChips.Clear();

            int shown = 0;
            for (int i = 0; i < State.Queue.Count; i++)
            {
                var stack = State.Queue[i];
                if (stack == null || !stack.Alive) continue;

                bool isCurrent = i == State.QueueIndex;
                bool acted = i < State.QueueIndex;

                var chip = UiKit.Panel(queueRoot, "Chip" + i, new Color(0.1f, 0.1f, 0.1f, 0.9f));
                UiKit.PlaceBottom(chip.rectTransform, shown * 54f, 0f, 48f, 44f);

                var background = stack.Side == Side.Player
                    ? new Color(0.22f, 0.40f, 0.62f, acted ? 0.35f : 0.95f)
                    : new Color(0.60f, 0.24f, 0.20f, acted ? 0.35f : 0.95f);

                if (isCurrent) background = new Color(0.85f, 0.70f, 0.34f, 1f);
                chip.color = background;

                var label = UiKit.Label(chip.transform, "Count", stack.Count.ToString(), 15,
                    isCurrent ? new Color(0.12f, 0.10f, 0.07f) : Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                UiKit.Fill(label.rectTransform);

                var abbr = UiKit.Label(chip.transform, "Abbr", stack.Def.Abbr, 10,
                    isCurrent ? new Color(0.12f, 0.10f, 0.07f) : new Color(1f, 1f, 1f, 0.85f), TextAnchor.LowerCenter);
                UiKit.Stretch(abbr.rectTransform, 0f, 0f, 0f, 2f);

                queueChips.Add(chip.gameObject);
                shown++;
            }
        }

        void RefreshCard(UnitStack current)
        {
            if (current == null)
            {
                UiKit.SetText(cardName, "Нет активного отряда");
                UiKit.SetText(cardSub, "");
                UiKit.SetText(cardHpText, "");
                UiKit.SetText(cardStats, "");
                UiKit.SetBar(cardHpFill, 0f);
                return;
            }

            var def = current.Def;
            UiKit.SetText(cardName, def.Name + " · " + current.Count);
            UiKit.SetText(cardSub, def.ClassName + " · " + def.Faction + " · " + (current.Side == Side.Player ? "ваш отряд" : "противник"));
            UiKit.SetText(cardHpText, "Здоровье отряда: " + current.TotalHp + " / " + current.MaxTotalHp);
            UiKit.SetBar(cardHpFill, current.HpRatio);

            string stats = "Атака " + current.EffectiveAttack + "   Защита " + current.EffectiveDefense +
                           "   Урон " + def.MinDamage + "–" + def.MaxDamage + "   Скорость " + current.EffectiveSpeed;
            if (def.Ranged) stats += "\nПатроны: " + current.ShotsLeft;
            if (def.Flying) stats += "   ·   летающий";
            if (current.Blessed) stats += "   ·   благословлён";
            if (current.Defended) stats += "   ·   в обороне";
            UiKit.SetText(cardStats, stats);

            portraitBg.color = UiKit.Darken(def.Color, 0.62f);
            portraitIcon.sprite = ProceduralArt.IconFor(def);
        }

        void RefreshSpells(bool playerTurn)
        {
            var hero = State.PlayerHero;
            for (int i = 0; i < 3; i++)
            {
                var spell = Spellbook.All[i];
                bool selected = pendingSpell.HasValue && pendingSpell.Value == spell.Id;
                bool canCast = playerTurn && hero != null && !hero.CastThisRound && hero.Mana >= spell.Cost;
                if (spellButtons[i] != null)
                {
                    spellButtons[i].interactable = canCast;
                    var image = spellButtons[i].image;
                    image.color = selected
                        ? new Color(0.62f, 0.50f, 0.22f)
                        : canCast
                            ? new Color(0.18f, 0.22f, 0.30f)
                            : new Color(0.14f, 0.14f, 0.16f);
                }

                if (spellLabels[i] != null)
                {
                    spellLabels[i].text = LabelFor(spell, i);
                }
            }
        }

        public void SetHint(string text)
        {
            UiKit.SetText(hintText, text);
        }

        public void SetSpellMode(SpellId? spell)
        {
            pendingSpell = spell;
            RefreshSpells(State != null && State.Current != null && State.Current.Side == Side.Player && !State.GameOver);
        }

        public void ShowTooltip(UnitStack stack, Vector2 screenPosition)
        {
            if (stack == null || tooltipGroup == null) return;

            if (lastTooltipStack != stack || !tooltipGroup.gameObject.activeSelf)
            {
                lastTooltipStack = stack;

                var def = stack.Def;
                var sb = new StringBuilder();
                sb.AppendLine(def.Name + " (" + stack.Count + ")");
                sb.AppendLine(def.ClassName + " · " + def.Faction);
                sb.AppendLine("Здоровье: " + stack.TotalHp + " / " + stack.MaxTotalHp);
                sb.AppendLine("Атака " + stack.EffectiveAttack + "   Защита " + stack.EffectiveDefense);
                sb.AppendLine("Урон " + def.MinDamage + "–" + def.MaxDamage + "   Скорость " + stack.EffectiveSpeed);
                if (def.Ranged) sb.AppendLine("Патроны: " + stack.ShotsLeft);
                if (def.Flying) sb.AppendLine("Летающий отряд");
                if (stack.Blessed) sb.AppendLine("Благословение: максимальный урон");
                sb.AppendLine();
                sb.Append(def.Description);

                UiKit.SetText(tooltipText, sb.ToString());
            }

            tooltipGroup.gameObject.SetActive(true);

            var canvasRect = (RectTransform)canvas.transform;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out local);
            float halfW = canvasRect.rect.width * 0.5f;
            float halfH = canvasRect.rect.height * 0.5f;
            float width = tooltipGroup.sizeDelta.x;
            float height = tooltipGroup.sizeDelta.y;

            float x = Mathf.Clamp(local.x + 18f, -halfW + 8f, halfW - width - 8f);
            float y = Mathf.Clamp(local.y - 18f, -halfH + height + 8f, halfH - 8f);
            tooltipGroup.anchoredPosition = new Vector2(x, y);
        }

        public void HideTooltip()
        {
            lastTooltipStack = null;
            if (tooltipGroup != null) tooltipGroup.gameObject.SetActive(false);
        }

        public void ShowResult(bool playerWon)
        {
            if (resultOverlay == null) return;

            HideTooltip();
            Sfx.Play(playerWon ? SfxId.Victory : SfxId.Defeat, 0.9f);

            resultOverlay.SetActive(true);
            resultTitle.text = playerWon ? "ПОБЕДА!" : "ПОРАЖЕНИЕ";
            resultTitle.color = playerWon ? UiKit.Gold : new Color(0.90f, 0.42f, 0.36f);

            var sb = new StringBuilder();
            sb.AppendLine("Раундов: " + State.Round);
            sb.AppendLine();
            sb.AppendLine("Ваши отряды:");
            for (int i = 0; i < State.Stacks.Count; i++)
            {
                var stack = State.Stacks[i];
                if (stack.Side != Side.Player) continue;
                sb.AppendLine("• " + stack.Def.Name + ": " + stack.InitialCount + " → " + stack.Count +
                              "  (погибло " + (stack.InitialCount - stack.Count) + ")");
            }

            sb.AppendLine();
            sb.AppendLine("Противник:");
            for (int i = 0; i < State.Stacks.Count; i++)
            {
                var stack = State.Stacks[i];
                if (stack.Side != Side.Enemy) continue;
                sb.AppendLine("• " + stack.Def.Name + ": " + stack.InitialCount + " → " + stack.Count);
            }

            resultInfo.text = sb.ToString();
        }

        public void HideResult()
        {
            if (resultOverlay != null) resultOverlay.SetActive(false);
        }
    }
}
