using System;
using UnityEngine;
using UnityEngine.UI;

namespace HeroesBattle
{
    public class MainMenuUI : MonoBehaviour
    {
        public Action OnNewBattle;
        public Action OnRandomBattle;
        public Action OnQuit;

        Canvas canvas;

        public void Build()
        {
            canvas = UiKit.CreateCanvas("MenuCanvas", 5, transform);
            var root = (RectTransform)canvas.transform;

            var bg = UiKit.Img(root, "Background", ProceduralArt.Background, Color.white, false);
            UiKit.Fill(bg.rectTransform);

            var vignette = UiKit.Img(root, "Vignette", ProceduralArt.WhitePixel, new Color(0.02f, 0.018f, 0.014f, 0.35f), false);
            UiKit.Fill(vignette.rectTransform);

            var title = UiKit.Label(root, "Title", "БИТВА ГЕРОЕВ", 88, UiKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(title.rectTransform, 0f, 110f, 1600f, 110f);

            var subtitle = UiKit.Label(root, "Subtitle",
                "Пошаговые сражения армий в духе Heroes of Might and Magic III",
                24, UiKit.TextMain, TextAnchor.MiddleCenter);
            UiKit.Place(subtitle.rectTransform, 0f, 224f, 1600f, 34f);

            var line = UiKit.Img(root, "Divider", ProceduralArt.WhitePixel, new Color(0.95f, 0.78f, 0.42f, 0.5f), false);
            UiKit.Place(line.rectTransform, 610f, 270f, 380f, 2f);

            var newBattle = UiKit.TextButton(root, "NewBattle", "Новый бой", 24,
                new Color(0.28f, 0.38f, 0.24f), UiKit.TextMain, () => { if (OnNewBattle != null) OnNewBattle(); });
            UiKit.Place(newBattle.image.rectTransform, 610f, 320f, 380f, 62f);

            var randomBattle = UiKit.TextButton(root, "RandomBattle", "Случайная армия", 22,
                new Color(0.24f, 0.26f, 0.34f), UiKit.TextMain, () => { if (OnRandomBattle != null) OnRandomBattle(); });
            UiKit.Place(randomBattle.image.rectTransform, 610f, 394f, 380f, 58f);

            var quit = UiKit.TextButton(root, "Quit", "Выход", 20,
                new Color(0.24f, 0.20f, 0.18f), UiKit.TextDim, QuitGame);
            UiKit.Place(quit.image.rectTransform, 610f, 464f, 380f, 50f);

            var hint = UiKit.Label(root, "Hint",
                "Гексагональное поле · инициатива · ответные удары · стрелки · магия героя",
                16, UiKit.TextDim, TextAnchor.MiddleCenter);
            UiKit.Place(hint.rectTransform, 0f, 560f, 1600f, 26f);

            var badges = UiKit.Group(root, "Badges");
            UiKit.Place(badges, 0f, 606f, 1600f, 90f);
            var pool = UnitDb.PlayerPool;
            float badgeSize = 72f;
            float spacing = 96f;
            float startX = (1600f - (pool.Count - 1) * spacing - badgeSize) * 0.5f;
            for (int i = 0; i < pool.Count; i++)
            {
                var badge = UiKit.UnitBadge(badges, "Badge" + i, pool[i], badgeSize);
                UiKit.PlaceBottom((RectTransform)badge.transform, startX + i * spacing, 8f, badgeSize, badgeSize);
                var label = UiKit.Label(badge.transform, "Name", pool[i].Singular, 12, UiKit.TextDim, TextAnchor.UpperCenter);
                UiKit.Place(label.rectTransform, -20f, 76f, badgeSize + 40f, 20f);
            }

            var footer = UiKit.Label(root, "Footer", "Unity 6000.4.7f1 · вся графика и звук генерируются кодом", 13,
                new Color(0.55f, 0.53f, 0.48f), TextAnchor.MiddleRight);
            UiKit.Place(footer.rectTransform, 0f, 850f, 1570f, 22f);

            var sound = UiKit.TextButton(root, "Sound", Sfx.Label, 13,
                new Color(0.16f, 0.15f, 0.12f), UiKit.TextDim, ToggleSound);
            UiKit.Place(sound.image.rectTransform, 40f, 796f, 180f, 36f);
            soundLabel = sound.GetComponentInChildren<Text>();
            RefreshSoundLabel();
        }

        Text soundLabel;

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

        void QuitGame()
        {
            if (OnQuit != null) OnQuit();
        }

        public void Dispose()
        {
            if (canvas != null) Destroy(canvas.gameObject);
        }
    }
}
