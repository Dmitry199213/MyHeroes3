using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HeroesBattle
{
    /// <summary>Простой, но осмысленный ИИ: оценивает урон, ответку, приоритет стрелков и раненых.</summary>
    public class BattleAI
    {
        readonly BattleState state;

        public BattleAI(BattleState state)
        {
            this.state = state;
        }

        public BattleAction Decide(UnitStack actor)
        {
            if (actor == null || !actor.Alive) return BattleAction.Simple(BattleActionType.Skip);

            var enemies = state.EnemiesOf(actor.Side);
            if (enemies.Count == 0) return BattleAction.Simple(BattleActionType.Skip);

            var spell = ConsiderSpell(actor, enemies);
            if (spell != null) return spell;

            if (actor.CanShoot && !state.HasAdjacentEnemy(actor))
            {
                UnitStack bestTarget = null;
                float bestScore = float.MinValue;

                for (int i = 0; i < enemies.Count; i++)
                {
                    var enemy = enemies[i];
                    int damage = state.EstimateDamage(actor, enemy);
                    float score = damage * 10f / Mathf.Max(1, enemy.TotalHp);
                    if (damage >= enemy.TotalHp) score += 30f;
                    if (enemy.Def.Ranged) score += 12f;
                    if (enemy.Count <= 3) score += 6f;
                    score -= HexMath.Distance(actor.Pos, enemy.Pos) * 0.3f;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestTarget = enemy;
                    }
                }

                if (bestTarget != null) return BattleAction.Shoot(bestTarget);
            }

            BattleAction bestMelee = null;
            float bestValue = float.MinValue;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                var path = state.FindAttackPath(actor, enemy);
                if (path == null) continue;

                int damage = state.EstimateDamage(actor, enemy);
                int retaliation = enemy.Alive ? state.EstimateDamage(enemy, actor) : 0;
                float value = damage - retaliation * 0.85f;
                if (damage >= enemy.TotalHp) value += 40f;
                if (enemy.Def.Ranged) value += 10f;
                value -= path.Count * 0.4f;

                if (value > bestValue)
                {
                    bestValue = value;
                    bestMelee = BattleAction.Melee(enemy, path);
                }
            }

            if (bestMelee != null) return bestMelee;

            // Никого не достать — идём к ближайшему врагу.
            var reachable = state.GetReachable(actor);
            Hex bestHex = new Hex(-1, -1);
            float bestDistance = float.MaxValue;

            foreach (var hex in reachable)
            {
                int nearest = 999;
                for (int i = 0; i < enemies.Count; i++)
                {
                    nearest = Mathf.Min(nearest, HexMath.Distance(hex, enemies[i].Pos));
                }

                if (nearest < bestDistance)
                {
                    bestDistance = nearest;
                    bestHex = hex;
                }
            }

            if (bestHex.Col >= 0)
            {
                var path = state.FindPath(actor, bestHex);
                if (path != null && path.Count > 0) return BattleAction.Move(path);
            }

            return BattleAction.Simple(BattleActionType.Defend);
        }

        BattleAction ConsiderSpell(UnitStack actor, List<UnitStack> enemies)
        {
            var hero = state.HeroOf(actor.Side);
            if (hero == null || hero.CastThisRound || hero.Def == null) return null;

            var arrow = Spellbook.Get(SpellId.MagicArrow);
            int arrowDamage = 10 + 10 * hero.MagicPower;

            if (hero.Mana >= arrow.Cost)
            {
                UnitStack best = null;
                float bestScore = float.MinValue;

                for (int i = 0; i < enemies.Count; i++)
                {
                    var enemy = enemies[i];
                    float score = arrowDamage * 60f / Mathf.Max(1, enemy.TotalHp);
                    if (arrowDamage >= enemy.TotalHp) score += 60f;
                    if (enemy.Def.Ranged) score += 15f;
                    if (enemy.Count <= 3) score += 5f;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = enemy;
                    }
                }

                bool worthIt = best != null && (arrowDamage >= best.TotalHp || arrowDamage >= 30);
                if (worthIt) return BattleAction.SpellCast(SpellId.MagicArrow, best);
            }

            var friends = state.FriendsOf(actor.Side);

            if (hero.Mana >= Spellbook.Get(SpellId.Bless).Cost)
            {
                UnitStack best = null;
                float bestPower = 0f;

                for (int i = 0; i < friends.Count; i++)
                {
                    var friend = friends[i];
                    if (friend.Blessed) continue;
                    float power = friend.Count * friend.Def.AverageDamage * (1f + 0.05f * friend.EffectiveAttack);
                    if (power > bestPower)
                    {
                        bestPower = power;
                        best = friend;
                    }
                }

                if (best != null && best.Count >= 2 && best.Def.AverageDamage >= 2.5f)
                {
                    return BattleAction.SpellCast(SpellId.Bless, best);
                }
            }

            if (hero.Mana >= Spellbook.Get(SpellId.Haste).Cost)
            {
                UnitStack best = null;
                int bestSpeed = int.MinValue;

                for (int i = 0; i < friends.Count; i++)
                {
                    var friend = friends[i];
                    if (friend.SpeedBonus > 0) continue;
                    if (friend.Def.Flying) continue;
                    if (friend.Def.Speed > bestSpeed)
                    {
                        bestSpeed = friend.Def.Speed;
                        best = friend;
                    }
                }

                if (best != null) return BattleAction.SpellCast(SpellId.Haste, best);
            }

            return null;
        }
    }

    /// <summary>Прогон боя без отображения (для тестов и отладки).</summary>
    public static class BattleSimulator
    {
        public static void Run(BattleState state, int maxActions = 6000)
        {
            var ai = new BattleAI(state);
            int guard = 0;

            while (!state.GameOver && guard++ < maxActions)
            {
                var actor = state.NextActor();
                if (actor == null) break;

                var action = ai.Decide(actor);
                if (action == null) action = BattleAction.Simple(BattleActionType.Skip);

                var result = state.Execute(actor, action);
                if (!result.Success)
                {
                    result = state.Execute(actor, BattleAction.Simple(BattleActionType.Defend));
                }

                if (result == null || !result.Success)
                {
                    result = state.Execute(actor, BattleAction.Simple(BattleActionType.Skip));
                }

                if (!result.Success)
                {
                    throw new System.Exception("Не удалось выполнить ход: " + result.Error);
                }

                if (result.TurnConsumed)
                {
                    state.EndTurn();
                }
            }

            if (!state.GameOver)
            {
                throw new System.Exception("Бой не завершился за " + maxActions + " действий (раунд " + state.Round + ").");
            }
        }
    }
}
