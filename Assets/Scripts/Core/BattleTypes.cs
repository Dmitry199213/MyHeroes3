using System.Collections.Generic;
using UnityEngine;

namespace HeroesBattle
{
    public class HeroState
    {
        public HeroDef Def;
        public Side Side;
        public int Mana;

        public bool CastThisRound;

        public HeroState(HeroDef def, Side side)
        {
            Def = def;
            Side = side;
            Mana = def != null ? def.MaxMana : 0;
        }

        public int ManaMax
        {
            get { return Def != null ? Def.MaxMana : 0; }
        }

        public int MagicPower
        {
            get { return Def != null ? Def.MagicPower : 0; }
        }

        public int AttackBonus
        {
            get { return Def != null ? Def.AttackBonus : 0; }
        }
    }

    public class UnitStack
    {
        public int Id;
        public UnitDef Def;
        public Side Side;
        public HeroState Hero;

        public int InitialCount;
        public int MaxTotalHp;
        public int TotalHp;

        public Hex Pos;
        public int ShotsLeft;

        public bool Retaliated;
        public bool Waited;
        public bool Defended;
        public bool Blessed;
        public int SpeedBonus;

        public int Count
        {
            get
            {
                if (TotalHp <= 0) return 0;
                return (TotalHp + Def.Hp - 1) / Def.Hp;
            }
        }

        public bool Alive
        {
            get { return TotalHp > 0; }
        }

        public int EffectiveSpeed
        {
            get { return Mathf.Max(1, Def.Speed + SpeedBonus); }
        }

        public int EffectiveAttack
        {
            get { return Def.Attack + (Hero != null ? Hero.AttackBonus : 0); }
        }

        public int EffectiveDefense
        {
            get
            {
                int defense = Def.Defense;
                if (Defended)
                {
                    defense += Mathf.Max(1, Mathf.RoundToInt(Def.Defense * 0.3f));
                }

                return defense;
            }
        }

        public bool CanShoot
        {
            get { return Def.Ranged && ShotsLeft > 0; }
        }

        public float HpRatio
        {
            get { return MaxTotalHp <= 0 ? 0f : Mathf.Clamp01((float)TotalHp / MaxTotalHp); }
        }

        public string DisplayName
        {
            get { return Def.Name + " (" + Count + ")"; }
        }
    }

    public class DamageInfo
    {
        public UnitStack Attacker;
        public UnitStack Target;
        public int Damage;
        public int Killed;
        public int Remaining;
        public bool Ranged;
        public bool Retaliation;
        public bool Spell;
    }

    public enum BattleActionType
    {
        Move,
        Melee,
        Shoot,
        Defend,
        Wait,
        Skip,
        Spell
    }

    public class BattleAction
    {
        public BattleActionType Type;
        public UnitStack Target;
        public Hex Dest;
        public List<Hex> Path;
        public SpellId Spell;

        public static BattleAction Move(List<Hex> path)
        {
            return new BattleAction
            {
                Type = BattleActionType.Move,
                Path = path,
                Dest = path != null && path.Count > 0 ? path[path.Count - 1] : new Hex(-1, -1)
            };
        }

        public static BattleAction Melee(UnitStack target, List<Hex> path)
        {
            return new BattleAction { Type = BattleActionType.Melee, Target = target, Path = path };
        }

        public static BattleAction Shoot(UnitStack target)
        {
            return new BattleAction { Type = BattleActionType.Shoot, Target = target };
        }

        public static BattleAction SpellCast(SpellId spell, UnitStack target)
        {
            return new BattleAction { Type = BattleActionType.Spell, Spell = spell, Target = target };
        }

        public static BattleAction Simple(BattleActionType type)
        {
            return new BattleAction { Type = type };
        }
    }

    public class BattleActionResult
    {
        public bool Success;
        public bool TurnConsumed = true;
        public string Error = string.Empty;
        public BattleActionType Type;
        public UnitStack Actor;
        public UnitStack Target;
        public Hex From;
        public Hex To;
        public List<Hex> Path;
        public DamageInfo Hit;
        public DamageInfo Retaliation;
        public SpellId Spell;
    }
}
