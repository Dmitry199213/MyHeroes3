using System.Collections.Generic;
using UnityEngine;

namespace HeroesBattle
{
    public enum UnitId
    {
        Pikeman,
        Archer,
        Griffin,
        Swordsman,
        Cavalier,
        Angel,
        Goblin,
        Orc,
        Wolf,
        Ogre,
        Skeleton,
        Lich
    }

    public enum Side
    {
        Player,
        Enemy
    }

    public enum UnitClass
    {
        Melee,
        Ranged,
        Flyer
    }

    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    public class UnitDef
    {
        public UnitId Id;
        public string Name;
        public string Singular;
        public string Abbr;
        public string Faction;
        public string Description;
        public int Cost;
        public int Hp;
        public int MinDamage;
        public int MaxDamage;
        public int Attack;
        public int Defense;
        public int Speed;
        public bool Ranged;
        public int Shots;
        public bool Flying;
        public Color Color;

        public UnitClass Class
        {
            get
            {
                if (Flying) return UnitClass.Flyer;
                return Ranged ? UnitClass.Ranged : UnitClass.Melee;
            }
        }

        public float AverageDamage
        {
            get { return (MinDamage + MaxDamage) * 0.5f; }
        }

        public string ClassName
        {
            get
            {
                if (Flying) return "Летающий";
                return Ranged ? "Стрелок" : "Ближний бой";
            }
        }
    }

    public static class UnitDb
    {
        public static readonly Dictionary<UnitId, UnitDef> Map = new Dictionary<UnitId, UnitDef>();
        public static readonly List<UnitDef> PlayerPool = new List<UnitDef>();
        public static readonly List<UnitDef> EnemyPool = new List<UnitDef>();

        static UnitDb()
        {
            Add(new UnitDef
            {
                Id = UnitId.Pikeman, Name = "Копейщики", Singular = "Копейщик", Abbr = "Коп",
                Faction = "Замок", Cost = 60, Hp = 10, MinDamage = 1, MaxDamage = 3,
                Attack = 4, Defense = 5, Speed = 4,
                Color = new Color(0.44f, 0.55f, 0.72f),
                Description = "Дешёвая и многочисленная пехота. Держит строй и прикрывает стрелков."
            }, true, false);

            Add(new UnitDef
            {
                Id = UnitId.Archer, Name = "Лучники", Singular = "Лучник", Abbr = "Лук",
                Faction = "Замок", Cost = 120, Hp = 10, MinDamage = 2, MaxDamage = 3,
                Attack = 6, Defense = 3, Speed = 6, Ranged = true, Shots = 24,
                Color = new Color(0.49f, 0.66f, 0.36f),
                Description = "Стрелки. Бьют на любую дистанцию, но беззащитны в ближнем бою."
            }, true, false);

            Add(new UnitDef
            {
                Id = UnitId.Griffin, Name = "Грифоны", Singular = "Грифон", Abbr = "Грф",
                Faction = "Замок", Cost = 220, Hp = 25, MinDamage = 3, MaxDamage = 6,
                Attack = 8, Defense = 8, Speed = 7, Flying = true,
                Color = new Color(0.62f, 0.76f, 0.88f),
                Description = "Летающий отряд: игнорирует препятствия и быстро сближается с врагом."
            }, true, false);

            Add(new UnitDef
            {
                Id = UnitId.Swordsman, Name = "Мечники", Singular = "Мечник", Abbr = "Меч",
                Faction = "Замок", Cost = 320, Hp = 35, MinDamage = 6, MaxDamage = 9,
                Attack = 10, Defense = 12, Speed = 5,
                Color = new Color(0.72f, 0.41f, 0.32f),
                Description = "Тяжёлая пехота с крепкой бронёй. Отлично держит удар."
            }, true, false);

            Add(new UnitDef
            {
                Id = UnitId.Cavalier, Name = "Рыцари", Singular = "Рыцарь", Abbr = "Рыц",
                Faction = "Замок", Cost = 1000, Hp = 100, MinDamage = 15, MaxDamage = 25,
                Attack = 12, Defense = 12, Speed = 9,
                Color = new Color(0.83f, 0.68f, 0.34f),
                Description = "Элитная кавалерия. Высокая скорость и сокрушительный удар."
            }, true, false);

            Add(new UnitDef
            {
                Id = UnitId.Angel, Name = "Ангелы", Singular = "Ангел", Abbr = "Анг",
                Faction = "Замок", Cost = 3000, Hp = 200, MinDamage = 50, MaxDamage = 50,
                Attack = 20, Defense = 20, Speed = 12, Flying = true,
                Color = new Color(0.95f, 0.92f, 0.76f),
                Description = "Легендарный отряд. Огромный запас здоровья и урон, не зависящий от удачи."
            }, true, false);

            Add(new UnitDef
            {
                Id = UnitId.Goblin, Name = "Гоблины", Singular = "Гоблин", Abbr = "Гоб",
                Faction = "Орда", Cost = 40, Hp = 5, MinDamage = 1, MaxDamage = 2,
                Attack = 4, Defense = 2, Speed = 5,
                Color = new Color(0.50f, 0.62f, 0.29f),
                Description = "Слабая, но крайне многочисленная пехота орды."
            }, false, true);

            Add(new UnitDef
            {
                Id = UnitId.Orc, Name = "Орки-лучники", Singular = "Орк-лучник", Abbr = "Орк",
                Faction = "Орда", Cost = 150, Hp = 15, MinDamage = 2, MaxDamage = 4,
                Attack = 8, Defense = 4, Speed = 4, Ranged = true, Shots = 12,
                Color = new Color(0.63f, 0.49f, 0.30f),
                Description = "Меткие стрелки орды. Опасны на дистанции, слабы вблизи."
            }, false, true);

            Add(new UnitDef
            {
                Id = UnitId.Wolf, Name = "Волки", Singular = "Волк", Abbr = "Волк",
                Faction = "Орда", Cost = 220, Hp = 20, MinDamage = 3, MaxDamage = 5,
                Attack = 9, Defense = 4, Speed = 8,
                Color = new Color(0.55f, 0.56f, 0.60f),
                Description = "Быстрые хищники: первыми сближаются и рвут слабые отряды."
            }, false, true);

            Add(new UnitDef
            {
                Id = UnitId.Ogre, Name = "Огры", Singular = "Огр", Abbr = "Огр",
                Faction = "Орда", Cost = 520, Hp = 60, MinDamage = 10, MaxDamage = 15,
                Attack = 13, Defense = 11, Speed = 5,
                Color = new Color(0.61f, 0.42f, 0.35f),
                Description = "Живая гора мышц. Медлительные, но очень живучие."
            }, false, true);

            Add(new UnitDef
            {
                Id = UnitId.Skeleton, Name = "Скелеты", Singular = "Скелет", Abbr = "Скл",
                Faction = "Орда", Cost = 55, Hp = 6, MinDamage = 1, MaxDamage = 3,
                Attack = 5, Defense = 4, Speed = 4,
                Color = new Color(0.76f, 0.74f, 0.64f),
                Description = "Восставшие костяки. Медленны, но упорно идут вперёд."
            }, false, true);

            Add(new UnitDef
            {
                Id = UnitId.Lich, Name = "Личи", Singular = "Лич", Abbr = "Лич",
                Faction = "Орда", Cost = 1600, Hp = 90, MinDamage = 12, MaxDamage = 18,
                Attack = 14, Defense = 10, Speed = 6, Ranged = true, Shots = 16,
                Color = new Color(0.56f, 0.43f, 0.71f),
                Description = "Тёмные маги орды. Опасные стрелки, но боятся ближнего боя."
            }, false, true);
        }

        static void Add(UnitDef def, bool player, bool enemy)
        {
            Map[def.Id] = def;
            if (player) PlayerPool.Add(def);
            if (enemy) EnemyPool.Add(def);
        }

        public static UnitDef Get(UnitId id)
        {
            UnitDef def;
            return Map.TryGetValue(id, out def) ? def : null;
        }
    }
}
