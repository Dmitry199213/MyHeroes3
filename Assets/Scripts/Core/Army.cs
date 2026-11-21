using System.Collections.Generic;
using UnityEngine;

namespace HeroesBattle
{
    public class ArmyStackSpec
    {
        public UnitId Unit;
        public int Count;

        public int TotalCost
        {
            get
            {
                var def = UnitDb.Get(Unit);
                return def != null ? def.Cost * Count : 0;
            }
        }
    }

    public class ArmySetup
    {
        public List<ArmyStackSpec> PlayerArmy = new List<ArmyStackSpec>();
        public List<ArmyStackSpec> EnemyArmy = new List<ArmyStackSpec>();
        public HeroDef PlayerHero;
        public HeroDef EnemyHero;
        public Difficulty Difficulty = Difficulty.Normal;

        public int PlayerCost
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < PlayerArmy.Count; i++) sum += PlayerArmy[i].TotalCost;
                return sum;
            }
        }

        public int EnemyCost
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < EnemyArmy.Count; i++) sum += EnemyArmy[i].TotalCost;
                return sum;
            }
        }
    }

    public static class ArmyBuilder
    {
        public const int Budget = 6000;
        public const int MaxStacks = 4;

        public static int Multiplier(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return 100;
                case Difficulty.Hard: return 130;
                default: return 110;
            }
        }

        public static List<ArmyStackSpec> RandomPlayerArmy(System.Random rng)
        {
            int stacks = rng.Next(3, MaxStacks + 1);
            return BuyArmy(UnitDb.PlayerPool, Budget, stacks, rng);
        }

        public static ArmySetup CreateSetup(List<ArmyStackSpec> playerArmy, HeroDef playerHero, Difficulty difficulty, System.Random rng)
        {
            var setup = new ArmySetup
            {
                PlayerArmy = new List<ArmyStackSpec>(playerArmy),
                PlayerHero = playerHero,
                Difficulty = difficulty
            };

            int playerCost = setup.PlayerCost;
            if (playerCost <= 0) playerCost = Budget;
            int enemyBudget = Mathf.RoundToInt(playerCost * Multiplier(difficulty) / 100f);
            int stacks = difficulty == Difficulty.Easy ? 3 : 4;
            setup.EnemyArmy = BuyArmy(UnitDb.EnemyPool, enemyBudget, stacks, rng);
            setup.EnemyHero = HeroDb.RandomEnemy(rng);
            return setup;
        }

        public static List<ArmyStackSpec> BuyArmy(List<UnitDef> pool, int budget, int desiredStacks, System.Random rng)
        {
            var result = new List<ArmyStackSpec>();
            var types = new List<UnitDef>(pool);
            Shuffle(types, rng);

            int stacks = Mathf.Clamp(desiredStacks, 1, MaxStacks);
            var chosen = new List<UnitDef>();
            for (int i = 0; i < types.Count && chosen.Count < stacks; i++)
            {
                if (types[i].Cost <= budget) chosen.Add(types[i]);
            }

            if (chosen.Count == 0) return result;

            int remaining = budget;
            for (int i = 0; i < chosen.Count; i++)
            {
                var def = chosen[i];
                int slotsLeft = chosen.Count - i;
                int share = remaining / slotsLeft;
                int count = share / def.Cost;
                if (count < 1)
                {
                    count = 1;
                }

                int reserve = 0;
                for (int j = i + 1; j < chosen.Count; j++) reserve += chosen[j].Cost;
                while (count > 1 && count * def.Cost + reserve > remaining) count--;
                if (count * def.Cost > remaining) continue;

                remaining -= count * def.Cost;
                result.Add(new ArmyStackSpec { Unit = def.Id, Count = count });
            }

            if (result.Count > 0)
            {
                int cheapestIndex = 0;
                int cheapestCost = int.MaxValue;
                for (int i = 0; i < result.Count; i++)
                {
                    int cost = UnitDb.Get(result[i].Unit).Cost;
                    if (cost < cheapestCost)
                    {
                        cheapestCost = cost;
                        cheapestIndex = i;
                    }
                }

                while (remaining >= cheapestCost && result[cheapestIndex].Count < 250)
                {
                    result[cheapestIndex].Count++;
                    remaining -= cheapestCost;
                }
            }

            return result;
        }

        public static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
