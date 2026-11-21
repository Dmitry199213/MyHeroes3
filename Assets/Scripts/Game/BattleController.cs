using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HeroesBattle
{
    /// <summary>Связывает правила боя, отображение и ввод игрока.</summary>
    public class BattleController : MonoBehaviour
    {
        BattleState state;
        BattleView view;
        BattleHUD hud;
        GameRoot game;

        UnitStack selected;
        readonly HashSet<Hex> reachable = new HashSet<Hex>();
        readonly Dictionary<UnitStack, List<Hex>> meleeTargets = new Dictionary<UnitStack, List<Hex>>();
        readonly List<UnitStack> shootTargets = new List<UnitStack>();
        readonly List<UnitStack> spellTargets = new List<UnitStack>();

        SpellId? pendingSpell;
        BattleAction pendingAction;
        bool waitingForInput;
        bool lastTurnConsumed = true;
        bool finished;

        public void Init(BattleState battleState, BattleView battleView, BattleHUD battleHud, GameRoot root)
        {
            state = battleState;
            view = battleView;
            hud = battleHud;
            game = root;

            hud.OnWait = CmdWait;
            hud.OnDefend = CmdDefend;
            hud.OnSkip = CmdSkip;
            hud.OnSpell = CmdSpell;

            StartCoroutine(RunBattle());
        }

        void Update()
        {
            if (state == null || state.GameOver) return;

            var cam = Camera.main;
            if (cam == null) return;

            Vector3 world = cam.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0f;
            Hex hex = view.HexAt(world);
            UnitStack hover = hex.Col >= 0 ? view.StackAt(hex) : null;

            if (hover != null) hud.ShowTooltip(hover, Input.mousePosition);
            else hud.HideTooltip();

            if (!waitingForInput || pendingAction != null) return;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                CmdWait();
                return;
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                CmdDefend();
                return;
            }

            if (Input.GetKeyDown(KeyCode.P))
            {
                CmdSkip();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                CmdSpell(0);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                CmdSpell(1);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                CmdSpell(2);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelSpell();
                return;
            }

            if (Input.GetMouseButtonDown(1))
            {
                CancelSpell();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                HandleClick(hex, hover);
            }
        }

        // ------------------------------------------------------------------ input

        public void CmdWait()
        {
            if (!waitingForInput || selected == null) return;
            if (selected.Waited) return;
            Submit(BattleAction.Simple(BattleActionType.Wait));
        }

        public void CmdDefend()
        {
            if (!waitingForInput || selected == null) return;
            Submit(BattleAction.Simple(BattleActionType.Defend));
        }

        public void CmdSkip()
        {
            if (!waitingForInput || selected == null) return;
            Submit(BattleAction.Simple(BattleActionType.Skip));
        }

        public void CmdSpell(int index)
        {
            if (!waitingForInput || selected == null) return;
            if (index < 0 || index >= Spellbook.All.Length) return;

            var spell = Spellbook.All[index];
            var hero = state.HeroOf(Side.Player);
            if (hero == null || hero.CastThisRound || hero.Mana < spell.Cost) return;

            if (pendingSpell.HasValue && pendingSpell.Value == spell.Id)
            {
                CancelSpell();
                return;
            }

            pendingSpell = spell.Id;
            view.ClearHighlights();

            spellTargets.Clear();
            var targets = spell.Target == SpellTarget.Enemy
                ? state.EnemiesOf(selected.Side)
                : state.FriendsOf(selected.Side);
            spellTargets.AddRange(targets);

            view.SetSpellTargets(spellTargets, spell.Color);
            hud.SetSpellMode(spell.Id);
            hud.SetHint("«" + spell.Name + "» (" + spell.Cost + " маны): выберите цель. ПКМ — отмена.");
        }

        void CancelSpell()
        {
            if (!pendingSpell.HasValue) return;

            pendingSpell = null;
            hud.SetSpellMode(null);
            view.ClearHighlights();

            if (selected != null && waitingForInput)
            {
                PrepareHighlights(selected);
                hud.SetHint(HintFor(selected));
            }
        }

        void Submit(BattleAction action)
        {
            if (!waitingForInput || action == null) return;

            pendingAction = action;
            waitingForInput = false;
            pendingSpell = null;
            hud.SetSpellMode(null);
            view.ClearHighlights();
        }

        void SubmitSpell(SpellId spell, UnitStack target)
        {
            Submit(BattleAction.SpellCast(spell, target));
        }

        void HandleClick(Hex hex, UnitStack stack)
        {
            if (selected == null) return;

            if (pendingSpell.HasValue)
            {
                var spell = Spellbook.Get(pendingSpell.Value);
                bool rightSide = stack != null &&
                                 (spell.Target == SpellTarget.Enemy ? stack.Side != selected.Side : stack.Side == selected.Side);
                if (rightSide) SubmitSpell(pendingSpell.Value, stack);
                else CancelSpell();
                return;
            }

            if (stack != null)
            {
                List<Hex> meleePath;
                if (meleeTargets.TryGetValue(stack, out meleePath))
                {
                    Submit(BattleAction.Melee(stack, meleePath));
                    return;
                }

                if (shootTargets.Contains(stack))
                {
                    Submit(BattleAction.Shoot(stack));
                    return;
                }
            }

            if (hex.Col >= 0 && reachable.Contains(hex))
            {
                var path = state.FindPath(selected, hex);
                if (path != null && path.Count > 0)
                {
                    Submit(BattleAction.Move(path));
                    return;
                }
            }

            if (stack == null)
            {
                CancelSpell();
            }
        }

        void PrepareHighlights(UnitStack actor)
        {
            reachable.Clear();
            meleeTargets.Clear();
            shootTargets.Clear();
            spellTargets.Clear();

            var reach = state.GetReachable(actor);
            foreach (var hex in reach) reachable.Add(hex);
            view.HighlightMove(reachable);

            bool archerFree = actor.CanShoot && !state.HasAdjacentEnemy(actor);
            if (archerFree)
            {
                shootTargets.AddRange(state.EnemiesOf(actor.Side));
                view.SetAttackTargets(shootTargets);
            }
            else
            {
                foreach (var enemy in state.EnemiesOf(actor.Side))
                {
                    var path = state.FindAttackPath(actor, enemy);
                    if (path != null) meleeTargets[enemy] = path;
                }

                view.SetAttackTargets(meleeTargets.Keys);
            }
        }

        string HintFor(UnitStack actor)
        {
            if (actor == null) return string.Empty;

            string hint = "Ваш ход: " + actor.Def.Name + " (" + actor.Count + "). ";
            if (actor.CanShoot && !state.HasAdjacentEnemy(actor))
            {
                hint += "Стрелок: клик по врагу — выстрел, по синему гексу — движение.";
            }
            else if (actor.Def.Ranged)
            {
                hint += "Стрелок скован ближним боем: клик по врагу — атака.";
            }
            else
            {
                hint += "Клик по синему гексу — движение, по врагу — атака.";
            }

            return hint;
        }

        // ------------------------------------------------------------------ turn flow

        IEnumerator RunBattle()
        {
            yield return new WaitForSeconds(0.35f);

            while (!state.GameOver)
            {
                var actor = state.NextActor();
                if (actor == null) break;

                view.SetCurrent(actor);
                hud.Refresh();

                lastTurnConsumed = true;

                if (actor.Side == Side.Player) yield return PlayerTurn(actor);
                else yield return EnemyTurn(actor);

                if (!state.GameOver && lastTurnConsumed) state.EndTurn();

                view.SetCurrent(null);
                view.SyncTokens();
                hud.Refresh();
                yield return new WaitForSeconds(0.1f);
            }

            hud.SetHint("Бой окончен.");
            view.SetCurrent(null);
            view.ShowSelectionRing(null);
            hud.HideTooltip();
            hud.Refresh();
            yield return new WaitForSeconds(0.8f);

            if (!finished)
            {
                finished = true;
                if (game != null) game.OnBattleFinished(state.PlayerWon);
            }
        }

        IEnumerator PlayerTurn(UnitStack actor)
        {
            selected = actor;
            view.ShowSelectionRing(actor);
            Sfx.Play(SfxId.Turn, 0.28f);

            if (game != null && game.AutoPlay)
            {
                hud.SetHint("Автобой: " + actor.Def.Name + " (" + actor.Count + ")…");
                yield return new WaitForSeconds(0.12f);

                var autoAi = new BattleAI(state);
                var autoAction = autoAi.Decide(actor);
                if (autoAction == null) autoAction = BattleAction.Simple(BattleActionType.Skip);

                selected = null;
                view.ShowSelectionRing(null);
                hud.SetSpellMode(null);
                yield return ExecuteAction(actor, autoAction);
                yield break;
            }

            PrepareHighlights(actor);
            hud.SetHint(HintFor(actor));

            pendingAction = null;
            waitingForInput = true;

            while (waitingForInput && !state.GameOver) yield return null;

            waitingForInput = false;
            var action = pendingAction;
            pendingAction = null;
            selected = null;

            view.ClearHighlights();
            view.ShowSelectionRing(null);
            hud.SetSpellMode(null);
            hud.HideTooltip();

            if (action != null) yield return ExecuteAction(actor, action);
        }

        IEnumerator EnemyTurn(UnitStack actor)
        {
            hud.SetHint("Ход противника: " + actor.Def.Name + " (" + actor.Count + ")…");
            view.ShowSelectionRing(actor);
            yield return new WaitForSeconds(0.45f);

            var ai = new BattleAI(state);
            var action = ai.Decide(actor);
            if (action == null) action = BattleAction.Simple(BattleActionType.Skip);

            yield return ExecuteAction(actor, action);

            view.ShowSelectionRing(null);
            yield return new WaitForSeconds(0.25f);
        }

        IEnumerator ExecuteAction(UnitStack actor, BattleAction action)
        {
            BattleActionResult result = null;

            if (action.Type == BattleActionType.Move) Sfx.Play(SfxId.Move, 0.4f);
            else if (action.Type == BattleActionType.Defend) Sfx.Play(SfxId.Move, 0.5f);
            else if (action.Type == BattleActionType.Wait || action.Type == BattleActionType.Skip) Sfx.Play(SfxId.Click, 0.4f);

            switch (action.Type)
            {
                case BattleActionType.Move:
                    if (action.Path != null && action.Path.Count > 0)
                    {
                        yield return view.MoveUnit(actor, action.Path);
                    }

                    result = state.Execute(actor, action);
                    break;

                case BattleActionType.Melee:
                    if (action.Path != null && action.Path.Count > 0)
                    {
                        yield return view.MoveUnit(actor, action.Path);
                    }

                    result = state.Execute(actor, action);
                    if (result.Success)
                    {
                        yield return view.AttackLunge(actor, result.Target);
                        view.FloatingDamage(result.Target, result.Hit);

                        if (result.Retaliation != null && result.Retaliation.Damage > 0)
                        {
                            yield return new WaitForSeconds(0.1f);
                            yield return view.AttackLunge(result.Target, result.Actor);
                            view.FloatingDamage(result.Actor, result.Retaliation);
                        }
                    }

                    break;

                case BattleActionType.Shoot:
                    if (action.Target != null)
                    {
                        yield return view.ShootArrow(actor, action.Target);
                    }

                    result = state.Execute(actor, action);
                    if (result.Success) view.FloatingDamage(result.Target, result.Hit);
                    break;

                case BattleActionType.Spell:
                    result = state.Execute(actor, action);
                    if (result.Success && result.Target != null)
                    {
                        yield return view.CastSpellFx(result.Target, action.Spell);
                        if (result.Hit != null)
                        {
                            view.FloatingDamage(result.Target, result.Hit);
                        }
                        else
                        {
                            var spell = Spellbook.Get(action.Spell);
                            view.FloatingMessage(result.Target, spell.Name, spell.Color);
                        }
                    }

                    break;

                default:
                    result = state.Execute(actor, action);
                    yield return new WaitForSeconds(0.2f);
                    break;
            }

            lastTurnConsumed = result == null || result.TurnConsumed;

            if (result != null && !result.Success && !string.IsNullOrEmpty(result.Error))
            {
                Sfx.Play(SfxId.Error, 0.7f);
                Debug.LogWarning("[Battle] " + result.Error);
                hud.SetHint("Нельзя: " + result.Error);
            }

            if (result != null && result.Success)
            {
                yield return view.PlayDeaths();
                view.SyncTokens();
            }

            hud.Refresh();
            yield return new WaitForSeconds(0.15f);
        }
    }
}
