using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroesBattle.EditorTools
{
    /// <summary>Автотест правил боя: прогоняет много битв ИИ против ИИ без графики.</summary>
    public static class BattleSelfTest
    {
        [UnityEditor.MenuItem("HeroesBattle/Test/Simulate 300 battles")]
        public static void SimulateMenu()
        {
            RunBatch();
        }

        public static void RunBatch()
        {
            const int battles = 300;
            int failures = 0;
            int playerWins = 0;
            int totalRounds = 0;
            int longestBattle = 0;
            var winsByDiff = new int[3];
            var battlesByDiff = new int[3];

            for (int i = 0; i < battles; i++)
            {
                try
                {
                    var rng = new System.Random(1000 + i * 7919);
                    var army = ArmyBuilder.RandomPlayerArmy(rng);
                    var hero = HeroDb.PlayerHeroes[i % HeroDb.PlayerHeroes.Length];
                    var difficulty = (Difficulty)(i % 3);
                    var setup = ArmyBuilder.CreateSetup(army, hero, difficulty, rng);

                    ValidateSetup(setup);

                    var state = BattleState.Create(setup, i * 131 + 7);
                    BattleSimulator.Run(state, 8000);

                    ValidateState(state);
                    totalRounds += state.Round;
                    longestBattle = Mathf.Max(longestBattle, state.Round);
                    battlesByDiff[(int)difficulty]++;
                    if (state.PlayerWon)
                    {
                        playerWins++;
                        winsByDiff[(int)difficulty]++;
                    }
                }
                catch (Exception e)
                {
                    failures++;
                    Debug.LogError("[SelfTest] Бой #" + i + " провален: " + e.Message + "\n" + e.StackTrace);
                }
            }

            string summary = "[SelfTest] Боёв: " + battles + ", провалов: " + failures +
                             ", побед игрока: " + playerWins + ", среднее раундов: " +
                             (battles > 0 ? (totalRounds / battles) : 0) + ", максимум раундов: " + longestBattle +
                             ". По сложности: " +
                             "легко " + winsByDiff[0] + "/" + battlesByDiff[0] +
                             ", норма " + winsByDiff[1] + "/" + battlesByDiff[1] +
                             ", сложно " + winsByDiff[2] + "/" + battlesByDiff[2];

            if (failures > 0)
            {
                throw new Exception(summary);
            }

            Debug.Log(summary);
        }

        static void ValidateSetup(ArmySetup setup)
        {
            if (setup.PlayerArmy.Count == 0) throw new Exception("Пустая армия игрока.");
            if (setup.PlayerArmy.Count > ArmyBuilder.MaxStacks) throw new Exception("Слишком много отрядов игрока.");
            if (setup.PlayerCost > ArmyBuilder.Budget) throw new Exception("Игрок превысил бюджет: " + setup.PlayerCost);
            if (setup.EnemyArmy.Count == 0) throw new Exception("Пустая армия противника.");

            for (int i = 0; i < setup.PlayerArmy.Count; i++)
            {
                if (setup.PlayerArmy[i].Count <= 0) throw new Exception("Нулевой отряд игрока.");
            }
        }

        static void ValidateState(BattleState state)
        {
            if (!state.GameOver) throw new Exception("Бой не завершён.");
            if (state.Round < 1) throw new Exception("Некорректный номер раунда.");
            if (state.Round > 80) throw new Exception("Слишком длинный бой: " + state.Round + " раундов.");
            if (state.ActionCount > 8000) throw new Exception("Слишком много действий.");

            if (state.PlayerHero != null && state.PlayerHero.Mana < 0) throw new Exception("Отрицательная мана игрока.");
            if (state.EnemyHero != null && state.EnemyHero.Mana < 0) throw new Exception("Отрицательная мана противника.");

            if (state.PlayerHero != null && state.PlayerHero.Mana > state.PlayerHero.ManaMax)
            {
                throw new Exception("Мана игрока больше максимума.");
            }

            if (state.Obstacles.Count < 3 || state.Obstacles.Count > 10)
            {
                throw new Exception("Странное количество препятствий: " + state.Obstacles.Count);
            }

            var occupied = new HashSet<Hex>();
            for (int i = 0; i < state.Stacks.Count; i++)
            {
                var stack = state.Stacks[i];
                if (stack.TotalHp < 0) throw new Exception("Отрицательное HP у " + stack.Def.Name);
                if (stack.Count < 0) throw new Exception("Отрицательное количество у " + stack.Def.Name);
                if (stack.Count > 0 && stack.TotalHp <= 0) throw new Exception("Живой отряд с нулевым HP.");
                if (!state.InBounds(stack.Pos)) throw new Exception("Отряд вне поля.");
                if (state.Obstacles.Contains(stack.Pos)) throw new Exception("Отряд стоит на препятствии.");

                if (stack.Alive)
                {
                    if (!occupied.Add(stack.Pos)) throw new Exception("Два отряда на одном гексе.");
                }
            }

            bool anyAlive = false;
            for (int i = 0; i < state.Stacks.Count; i++) anyAlive |= state.Stacks[i].Alive;
            if (!anyAlive) throw new Exception("После боя не осталось ни одного отряда (так не должно быть).");

            int playerAlive = 0;
            int enemyAlive = 0;
            for (int i = 0; i < state.Stacks.Count; i++)
            {
                if (!state.Stacks[i].Alive) continue;
                if (state.Stacks[i].Side == Side.Player) playerAlive++;
                else enemyAlive++;
            }

            if (playerAlive > 0 && enemyAlive > 0) throw new Exception("Бой завершён, но обе стороны живы.");
            if (state.PlayerWon != (enemyAlive == 0)) throw new Exception("Неверно определён победитель.");
            if (state.PlayerWon && playerAlive == 0) throw new Exception("Победа без выживших отрядов.");
        }
    }
}
