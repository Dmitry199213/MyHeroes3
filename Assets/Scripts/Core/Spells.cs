using UnityEngine;

namespace HeroesBattle
{
    public enum SpellId
    {
        MagicArrow,
        Bless,
        Haste
    }

    public enum SpellTarget
    {
        Enemy,
        Friendly
    }

    public class SpellDef
    {
        public SpellId Id;
        public string Name;
        public string ShortName;
        public int Cost;
        public SpellTarget Target;
        public Color Color;
        public string Description;
    }

    public static class Spellbook
    {
        public static readonly SpellDef MagicArrow = new SpellDef
        {
            Id = SpellId.MagicArrow,
            Name = "Стрела магии",
            ShortName = "Стрела",
            Cost = 5,
            Target = SpellTarget.Enemy,
            Color = new Color(0.55f, 0.78f, 1f),
            Description = "Наносит 10 + 10×сила магии урона выбранному вражескому отряду, игнорируя защиту."
        };

        public static readonly SpellDef Bless = new SpellDef
        {
            Id = SpellId.Bless,
            Name = "Благословение",
            ShortName = "Благосл.",
            Cost = 3,
            Target = SpellTarget.Friendly,
            Color = new Color(1f, 0.88f, 0.50f),
            Description = "Отряд до конца боя наносит максимальный урон (без случайного разброса)."
        };

        public static readonly SpellDef Haste = new SpellDef
        {
            Id = SpellId.Haste,
            Name = "Ускорение",
            ShortName = "Ускор.",
            Cost = 3,
            Target = SpellTarget.Friendly,
            Color = new Color(0.60f, 1f, 0.62f),
            Description = "Даёт отряду +3 к скорости до конца боя. Влияет и на порядок хода."
        };

        public static readonly SpellDef[] All = { MagicArrow, Bless, Haste };

        public static SpellDef Get(SpellId id)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id) return All[i];
            }

            return MagicArrow;
        }

        public static int IndexOf(SpellId id)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id) return i;
            }

            return 0;
        }
    }

    public class HeroDef
    {
        public string Name;
        public string Title;
        public string BonusText;
        public int AttackBonus;
        public int MagicPower;
        public int MaxMana;
        public Color Color;
    }

    public static class HeroDb
    {
        public static readonly HeroDef[] PlayerHeroes =
        {
            new HeroDef
            {
                Name = "Сэр Кристиан",
                Title = "Рыцарь Замка",
                BonusText = "Атака войск +2 · Сила магии 1 · Мана 9",
                AttackBonus = 2,
                MagicPower = 1,
                MaxMana = 9,
                Color = new Color(0.35f, 0.60f, 0.90f)
            },
            new HeroDef
            {
                Name = "Аделаида",
                Title = "Маг Замка",
                BonusText = "Атака войск +0 · Сила магии 3 · Мана 15",
                AttackBonus = 0,
                MagicPower = 3,
                MaxMana = 15,
                Color = new Color(0.70f, 0.50f, 0.90f)
            }
        };

        public static readonly HeroDef[] EnemyHeroes =
        {
            new HeroDef
            {
                Name = "Гортус",
                Title = "Вождь Орды",
                BonusText = "Атака войск +1 · Сила магии 2 · Мана 12",
                AttackBonus = 1,
                MagicPower = 2,
                MaxMana = 12,
                Color = new Color(0.85f, 0.35f, 0.30f)
            },
            new HeroDef
            {
                Name = "Мортис",
                Title = "Тёмный жрец",
                BonusText = "Атака войск +0 · Сила магии 3 · Мана 10",
                AttackBonus = 0,
                MagicPower = 3,
                MaxMana = 10,
                Color = new Color(0.60f, 0.35f, 0.75f)
            }
        };

        public static HeroDef RandomEnemy(System.Random rng)
        {
            return EnemyHeroes[rng.Next(EnemyHeroes.Length)];
        }
    }
}
