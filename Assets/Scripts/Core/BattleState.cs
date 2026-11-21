using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HeroesBattle
{
    /// <summary>Полное состояние пошагового боя. Не зависит от отображения.</summary>
    public class BattleState
    {
        public const int Width = 10;
        public const int Height = 8;

        public readonly HashSet<Hex> Obstacles = new HashSet<Hex>();
        public readonly List<UnitStack> Stacks = new List<UnitStack>();
        public readonly List<UnitStack> Queue = new List<UnitStack>();
        public readonly List<string> Log = new List<string>();

        public HeroState PlayerHero;
        public HeroState EnemyHero;

        public int Round = 1;
        public int QueueIndex;
        public bool GameOver;
        public bool PlayerWon;
        public int Seed;
        public int ActionCount;

        public System.Random Rng;

        public UnitStack Current
        {
            get { return QueueIndex >= 0 && QueueIndex < Queue.Count ? Queue[QueueIndex] : null; }
        }

        public IEnumerable<UnitStack> AliveStacks
        {
            get { return Stacks.Where(s => s.Alive); }
        }

        public static BattleState Create(ArmySetup setup, int seed)
        {
            var state = new BattleState();
            state.Seed = seed;
            state.Rng = new System.Random(seed);
            state.PlayerHero = setup.PlayerHero != null ? new HeroState(setup.PlayerHero, Side.Player) : null;
            state.EnemyHero = setup.EnemyHero != null ? new HeroState(setup.EnemyHero, Side.Enemy) : null;

            state.PlaceArmies(setup);
            state.PlaceObstacles();
            state.BuildQueue(false);
            state.LogLine("— Раунд 1 —");
            return state;
        }

        void PlaceArmies(ArmySetup setup)
        {
            PlaceSide(setup.PlayerArmy, Side.Player);
            PlaceSide(setup.EnemyArmy, Side.Enemy);
        }

        void PlaceSide(List<ArmyStackSpec> army, Side side)
        {
            if (army == null || army.Count == 0) return;

            int col = side == Side.Player ? 1 : Width - 2;
            int n = Mathf.Min(army.Count, ArmyBuilder.MaxStacks);

            for (int i = 0; i < n; i++)
            {
                var spec = army[i];
                var def = UnitDb.Get(spec.Unit);
                if (def == null) continue;

                int row = n == 1 ? Height / 2 - 1 : Mathf.RoundToInt(1f + i * (Height - 3f) / (n - 1f));
                row = FindFreeRow(col, row);

                int count = Mathf.Max(1, spec.Count);
                var stack = new UnitStack
                {
                    Id = Stacks.Count,
                    Def = def,
                    Side = side,
                    Hero = side == Side.Player ? PlayerHero : EnemyHero,
                    InitialCount = count,
                    MaxTotalHp = count * def.Hp,
                    TotalHp = count * def.Hp,
                    Pos = new Hex(col, row),
                    ShotsLeft = def.Shots
                };

                Stacks.Add(stack);
            }
        }

        int FindFreeRow(int col, int row)
        {
            row = Mathf.Clamp(row, 0, Height - 1);
            for (int attempt = 0; attempt < Height; attempt++)
            {
                int up = row - attempt;
                if (up >= 0 && StackAt(new Hex(col, up)) == null) return up;

                int down = row + attempt;
                if (down < Height && StackAt(new Hex(col, down)) == null) return down;
            }

            return row;
        }

        void PlaceObstacles()
        {
            int count = 5 + Rng.Next(0, 3);
            int guard = 0;
            while (Obstacles.Count < count && guard++ < 200)
            {
                int col = Rng.Next(3, Width - 3);
                int row = Rng.Next(0, Height);
                var hex = new Hex(col, row);
                if (StackAt(hex) != null) continue;
                Obstacles.Add(hex);
            }
        }

        void BuildQueue(bool newRound)
        {
            if (newRound)
            {
                Round++;
                LogLine("— Раунд " + Round + " —");
            }

            Queue.Clear();
            QueueIndex = 0;

            var alive = Stacks.Where(s => s.Alive).ToList();
            for (int i = 0; i < alive.Count; i++)
            {
                alive[i].Retaliated = false;
                alive[i].Waited = false;
            }

            if (PlayerHero != null) PlayerHero.CastThisRound = false;
            if (EnemyHero != null) EnemyHero.CastThisRound = false;

            var keyed = new List<KeyValuePair<int, UnitStack>>();
            for (int i = 0; i < alive.Count; i++)
            {
                keyed.Add(new KeyValuePair<int, UnitStack>(alive[i].EffectiveSpeed * 1000 + Rng.Next(0, 1000), alive[i]));
            }

            var ordered = keyed.OrderByDescending(k => k.Key).Select(k => k.Value);
            Queue.AddRange(ordered);
        }

        public UnitStack NextActor()
        {
            if (GameOver) return null;
            if (!Stacks.Any(s => s.Alive))
            {
                GameOver = true;
                return null;
            }

            for (int guard = 0; guard < 500; guard++)
            {
                if (Queue.Count == 0 || QueueIndex >= Queue.Count)
                {
                    BuildQueue(true);
                    continue;
                }

                var stack = Queue[QueueIndex];
                if (!stack.Alive)
                {
                    QueueIndex++;
                    continue;
                }

                // Бонус защиты действует до следующего хода отряда.
                stack.Defended = false;
                return stack;
            }

            return null;
        }

        public void EndTurn()
        {
            QueueIndex++;
        }

        public HeroState HeroOf(Side side)
        {
            return side == Side.Player ? PlayerHero : EnemyHero;
        }

        public bool InBounds(Hex hex)
        {
            return hex.Col >= 0 && hex.Col < Width && hex.Row >= 0 && hex.Row < Height;
        }

        public UnitStack StackAt(Hex hex)
        {
            for (int i = 0; i < Stacks.Count; i++)
            {
                if (Stacks[i].Alive && Stacks[i].Pos == hex) return Stacks[i];
            }

            return null;
        }

        public bool IsFree(Hex hex)
        {
            if (!InBounds(hex)) return false;
            if (Obstacles.Contains(hex)) return false;
            return StackAt(hex) == null;
        }

        public bool HasAdjacentEnemy(UnitStack stack)
        {
            for (int i = 0; i < Stacks.Count; i++)
            {
                var other = Stacks[i];
                if (!other.Alive || other.Side == stack.Side) continue;
                if (HexMath.Distance(stack.Pos, other.Pos) == 1) return true;
            }

            return false;
        }

        public List<UnitStack> EnemiesOf(Side side)
        {
            return Stacks.Where(s => s.Alive && s.Side != side).ToList();
        }

        public List<UnitStack> FriendsOf(Side side)
        {
            return Stacks.Where(s => s.Alive && s.Side == side).ToList();
        }

        /// <summary>Гексы, куда отряд может переместиться (без текущего).</summary>
        public HashSet<Hex> GetReachable(UnitStack stack)
        {
            var result = new HashSet<Hex>();
            if (stack == null || !stack.Alive) return result;

            int maxSteps = Mathf.Max(1, stack.EffectiveSpeed);

            if (stack.Def.Flying)
            {
                for (int row = 0; row < Height; row++)
                {
                    for (int col = 0; col < Width; col++)
                    {
                        var hex = new Hex(col, row);
                        if (hex == stack.Pos) continue;
                        if (HexMath.Distance(stack.Pos, hex) > maxSteps) continue;
                        if (IsFree(hex)) result.Add(hex);
                    }
                }

                return result;
            }

            var visited = new HashSet<Hex> { stack.Pos };
            var distances = new Dictionary<Hex, int> { { stack.Pos, 0 } };
            var frontier = new Queue<Hex>();
            frontier.Enqueue(stack.Pos);

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                int distance = distances[current];
                if (distance >= maxSteps) continue;

                foreach (var neighbor in HexMath.Neighbors(current))
                {
                    if (!IsFree(neighbor)) continue;
                    int nextDistance = distance + 1;
                    int known;
                    if (distances.TryGetValue(neighbor, out known) && known <= nextDistance) continue;
                    distances[neighbor] = nextDistance;
                    visited.Add(neighbor);
                    result.Add(neighbor);
                    frontier.Enqueue(neighbor);
                }
            }

            return result;
        }

        public List<Hex> FindPath(UnitStack stack, Hex dest)
        {
            if (stack == null || !stack.Alive) return null;
            if (!IsFree(dest)) return null;

            if (stack.Def.Flying)
            {
                if (HexMath.Distance(stack.Pos, dest) > stack.EffectiveSpeed) return null;
                return new List<Hex> { dest };
            }

            var start = stack.Pos;
            if (start == dest) return new List<Hex>();

            var parents = new Dictionary<Hex, Hex> { { start, start } };
            var distances = new Dictionary<Hex, int> { { start, 0 } };
            var queue = new Queue<Hex>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == dest)
                {
                    return ReconstructPath(parents, start, dest);
                }

                int distance = distances[current];
                if (distance >= stack.EffectiveSpeed) continue;

                foreach (var neighbor in HexMath.Neighbors(current))
                {
                    if (!IsFree(neighbor) || parents.ContainsKey(neighbor)) continue;
                    parents[neighbor] = current;
                    distances[neighbor] = distance + 1;
                    queue.Enqueue(neighbor);
                }
            }

            return null;
        }

        static List<Hex> ReconstructPath(Dictionary<Hex, Hex> parents, Hex start, Hex end)
        {
            var path = new List<Hex>();
            var current = end;
            int guard = 0;
            while (current != start && guard++ < 100)
            {
                path.Add(current);
                current = parents[current];
            }

            path.Reverse();
            return path;
        }

        /// <summary>Путь до гекса, с которого можно атаковать цель (пустой список — цель уже рядом).</summary>
        public List<Hex> FindAttackPath(UnitStack stack, UnitStack target)
        {
            if (stack == null || !stack.Alive || target == null || !target.Alive) return null;
            if (stack.Side == target.Side) return null;

            if (HexMath.Distance(stack.Pos, target.Pos) == 1) return new List<Hex>();

            int maxSteps = Mathf.Max(1, stack.EffectiveSpeed);

            if (stack.Def.Flying)
            {
                Hex best = new Hex(-1, -1);
                int bestDistance = int.MaxValue;
                for (int row = 0; row < Height; row++)
                {
                    for (int col = 0; col < Width; col++)
                    {
                        var hex = new Hex(col, row);
                        if (!IsFree(hex)) continue;
                        if (HexMath.Distance(hex, target.Pos) != 1) continue;
                        int distance = HexMath.Distance(stack.Pos, hex);
                        if (distance > maxSteps) continue;
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            best = hex;
                        }
                    }
                }

                return bestDistance == int.MaxValue ? null : new List<Hex> { best };
            }

            var start = stack.Pos;
            var parents = new Dictionary<Hex, Hex> { { start, start } };
            var distances = new Dictionary<Hex, int> { { start, 0 } };
            var queue = new Queue<Hex>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                int distance = distances[current];

                if (HexMath.Distance(current, target.Pos) == 1)
                {
                    return ReconstructPath(parents, start, current);
                }

                if (distance >= maxSteps) continue;

                foreach (var neighbor in HexMath.Neighbors(current))
                {
                    if (!IsFree(neighbor) || parents.ContainsKey(neighbor)) continue;
                    parents[neighbor] = current;
                    distances[neighbor] = distance + 1;
                    queue.Enqueue(neighbor);
                }
            }

            return null;
        }

        public int EstimateDamage(UnitStack attacker, UnitStack defender)
        {
            if (attacker == null || defender == null) return 0;
            float average = (attacker.Def.MinDamage + attacker.Def.MaxDamage) * 0.5f;
            return ComputeDamage(attacker, defender, average, attacker.Count);
        }

        int ComputeDamage(UnitStack attacker, UnitStack defender, float roll, int count)
        {
            float raw = roll * count;
            int attack = attacker.EffectiveAttack;
            int defense = defender.EffectiveDefense;
            float modifier = attack >= defense
                ? 1f + 0.05f * (attack - defense)
                : 1f / (1f + 0.05f * (defense - attack));
            return Mathf.Max(1, Mathf.RoundToInt(raw * modifier));
        }

        DamageInfo Strike(UnitStack attacker, UnitStack defender, bool ranged, bool retaliation)
        {
            int roll = attacker.Blessed
                ? attacker.Def.MaxDamage
                : Rng.Next(attacker.Def.MinDamage, attacker.Def.MaxDamage + 1);

            int damage = ComputeDamage(attacker, defender, roll, attacker.Count);
            int before = defender.Count;
            defender.TotalHp = Mathf.Max(0, defender.TotalHp - damage);
            int killed = before - defender.Count;

            var info = new DamageInfo
            {
                Attacker = attacker,
                Target = defender,
                Damage = damage,
                Killed = killed,
                Remaining = defender.Count,
                Ranged = ranged,
                Retaliation = retaliation
            };

            LogDamage(info, null);
            return info;
        }

        void LogDamage(DamageInfo info, HeroState caster)
        {
            string attacker = info.Attacker != null ? info.Attacker.Def.Name : "Магия";
            if (info.Spell)
            {
                string heroName = caster != null && caster.Def != null ? caster.Def.Name : "Герой";
                LogLine(heroName + " → " + info.Target.Def.Name + ": " + info.Damage + " урона, потери " + info.Killed + ". Осталось " + info.Remaining + ".");
                return;
            }

            string prefix = info.Retaliation ? "Ответный удар " + attacker : attacker;
            string verb = info.Ranged ? " стреляют по " : " атакуют ";
            if (info.Retaliation) verb = " по ";
            string line = prefix + (info.Retaliation ? string.Empty : " (" + info.Attacker.Count + ")") + verb + info.Target.Def.Name +
                          " (было " + (info.Target.Count + info.Killed) + "): " + info.Damage + " урона, потери " + info.Killed + ".";
            if (info.Ranged && info.Attacker != null)
            {
                line += " Патронов: " + info.Attacker.ShotsLeft + ".";
            }

            LogLine(line);
        }

        public BattleActionResult Execute(UnitStack actor, BattleAction action)
        {
            var result = new BattleActionResult
            {
                Type = action.Type,
                Actor = actor,
                Target = action.Target,
                From = actor != null ? actor.Pos : new Hex(-1, -1),
                To = actor != null ? actor.Pos : new Hex(-1, -1)
            };

            if (GameOver)
            {
                result.Error = "Бой уже окончен.";
                return result;
            }

            if (actor == null || !actor.Alive)
            {
                result.Error = "Отряд не может действовать.";
                return result;
            }

            ActionCount++;
            switch (action.Type)
            {
                case BattleActionType.Move:
                    return ExecuteMove(actor, action, result);
                case BattleActionType.Melee:
                    return ExecuteMelee(actor, action, result);
                case BattleActionType.Shoot:
                    return ExecuteShoot(actor, action, result);
                case BattleActionType.Defend:
                    actor.Defended = true;
                    LogLine(actor.Def.Name + " (" + actor.Count + ") занимают оборону.");
                    result.Success = true;
                    return result;
                case BattleActionType.Wait:
                    return ExecuteWait(actor, result);
                case BattleActionType.Skip:
                    LogLine(actor.Def.Name + " (" + actor.Count + ") пропускают ход.");
                    result.Success = true;
                    return result;
                case BattleActionType.Spell:
                    return ExecuteSpell(actor, action, result);
                default:
                    result.Error = "Неизвестное действие.";
                    return result;
            }
        }

        BattleActionResult ExecuteMove(UnitStack actor, BattleAction action, BattleActionResult result)
        {
            if (action.Dest == actor.Pos)
            {
                result.Error = "Отряд уже стоит на этом гексе.";
                return result;
            }

            var reachable = GetReachable(actor);
            if (!reachable.Contains(action.Dest))
            {
                result.Error = "Гекс недосягаем.";
                return result;
            }

            var path = FindPath(actor, action.Dest);
            if (path == null)
            {
                result.Error = "Нет пути.";
                return result;
            }

            actor.Pos = action.Dest;
            result.Path = path;
            result.To = action.Dest;
            result.Success = true;
            return result;
        }

        BattleActionResult ExecuteMelee(UnitStack actor, BattleAction action, BattleActionResult result)
        {
            var target = action.Target;
            if (target == null || !target.Alive || target.Side == actor.Side)
            {
                result.Error = "Неверная цель атаки.";
                return result;
            }

            var path = FindAttackPath(actor, target);
            if (path == null)
            {
                result.Error = "Цель недосягаема.";
                return result;
            }

            if (path.Count > 0)
            {
                actor.Pos = path[path.Count - 1];
            }

            result.Path = path;
            result.To = actor.Pos;
            result.Success = true;
            result.Hit = Strike(actor, target, false, false);

            if (target.Alive && !target.Retaliated)
            {
                target.Retaliated = true;
                result.Retaliation = Strike(target, actor, false, true);
            }

            CheckVictory();
            return result;
        }

        BattleActionResult ExecuteShoot(UnitStack actor, BattleAction action, BattleActionResult result)
        {
            var target = action.Target;
            if (!actor.CanShoot)
            {
                result.Error = "Стрел нет.";
                return result;
            }

            if (HasAdjacentEnemy(actor))
            {
                result.Error = "Стрелок скован ближним боем.";
                return result;
            }

            if (target == null || !target.Alive || target.Side == actor.Side)
            {
                result.Error = "Неверная цель выстрела.";
                return result;
            }

            actor.ShotsLeft--;
            result.Success = true;
            result.Hit = Strike(actor, target, true, false);
            CheckVictory();
            return result;
        }

        BattleActionResult ExecuteWait(UnitStack actor, BattleActionResult result)
        {
            if (actor.Waited)
            {
                result.Error = "Этот отряд уже ждал в этом раунде.";
                return result;
            }

            actor.Waited = true;
            int index = Queue.IndexOf(actor);
            if (index >= 0)
            {
                Queue.RemoveAt(index);
                Queue.Add(actor);
            }

            if (QueueIndex >= Queue.Count - 1)
            {
                // Ждать больше некого — ход просто заканчивается.
                result.TurnConsumed = true;
                LogLine(actor.Def.Name + " (" + actor.Count + ") ждут конца раунда.");
                result.Success = true;
                return result;
            }

            result.TurnConsumed = false;
            LogLine(actor.Def.Name + " (" + actor.Count + ") ждут.");
            result.Success = true;
            return result;
        }

        public bool CanCast(HeroState hero, SpellId spellId, UnitStack target, out string error)
        {
            error = null;
            if (hero == null)
            {
                error = "У этой стороны нет героя.";
                return false;
            }

            if (hero.CastThisRound)
            {
                error = "Герой уже использовал заклинание в этом раунде.";
                return false;
            }

            var spell = Spellbook.Get(spellId);
            if (hero.Mana < spell.Cost)
            {
                error = "Недостаточно маны.";
                return false;
            }

            if (target == null || !target.Alive)
            {
                error = "Нет цели.";
                return false;
            }

            if (spell.Target == SpellTarget.Enemy && target.Side == hero.Side)
            {
                error = "Нужна вражеская цель.";
                return false;
            }

            if (spell.Target == SpellTarget.Friendly && target.Side != hero.Side)
            {
                error = "Нужен свой отряд.";
                return false;
            }

            return true;
        }

        BattleActionResult ExecuteSpell(UnitStack actor, BattleAction action, BattleActionResult result)
        {
            var hero = HeroOf(actor.Side);
            string error;
            if (!CanCast(hero, action.Spell, action.Target, out error))
            {
                result.Error = error;
                return result;
            }

            var spell = Spellbook.Get(action.Spell);
            var target = action.Target;

            hero.Mana -= spell.Cost;
            hero.CastThisRound = true;
            result.Success = true;
            result.Spell = action.Spell;
            result.Target = target;

            switch (action.Spell)
            {
                case SpellId.MagicArrow:
                {
                    int damage = 10 + 10 * hero.MagicPower;
                    int before = target.Count;
                    target.TotalHp = Mathf.Max(0, target.TotalHp - damage);
                    var info = new DamageInfo
                    {
                        Attacker = null,
                        Target = target,
                        Damage = damage,
                        Killed = before - target.Count,
                        Remaining = target.Count,
                        Spell = true
                    };
                    result.Hit = info;
                    LogDamage(info, hero);
                    break;
                }
                case SpellId.Bless:
                    target.Blessed = true;
                    LogLine(hero.Def.Name + " накладывает «Благословение» на " + target.Def.Name + " (" + target.Count + ").");
                    break;
                case SpellId.Haste:
                    target.SpeedBonus = Mathf.Min(3, target.SpeedBonus + 3);
                    LogLine(hero.Def.Name + " ускоряет " + target.Def.Name + " (" + target.Count + "). Скорость: " + target.EffectiveSpeed + ".");
                    break;
            }

            CheckVictory();
            return result;
        }

        void CheckVictory()
        {
            bool playerAlive = false;
            bool enemyAlive = false;
            for (int i = 0; i < Stacks.Count; i++)
            {
                if (!Stacks[i].Alive) continue;
                if (Stacks[i].Side == Side.Player) playerAlive = true;
                else enemyAlive = true;
            }

            if (playerAlive && enemyAlive) return;

            GameOver = true;
            PlayerWon = !enemyAlive;
            LogLine(PlayerWon
                ? "Победа! Армия игрока уничтожила противника."
                : "Поражение. Ваша армия уничтожена.");
        }

        public void LogLine(string line)
        {
            Log.Add(line);
            if (Log.Count > 300) Log.RemoveAt(0);
        }
    }
}
