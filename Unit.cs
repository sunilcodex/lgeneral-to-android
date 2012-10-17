/*
 * Creado por SharpDevelop.
 * Usuario: asantos
 * Fecha: 09/01/2009
 * Hora: 16:06
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using DataFile;

namespace EngineA
{
    /// <summary>
    /// Embark types for a unit.
    /// </summary>
    public enum UnitEmbarkTypes
    {
        EMBARK_NONE = 0,
        EMBARK_GROUND,
        EMBARK_SEA,
        EMBARK_AIR,
        DROP_BY_PARACHUTE
    }


    /// <summary>
    /// Looking direction of units (used as icon id).
    /// </summary>
    public enum UnitLookingDirection
    {
        UNIT_ORIENT_RIGHT = 0,
        UNIT_ORIENT_LEFT,
        UNIT_ORIENT_UP = 0,
        UNIT_ORIENT_RIGHT_UP,
        UNIT_ORIENT_RIGHT_DOWN,
        UNIT_ORIENT_DOWN,
        UNIT_ORIENT_LEFT_DOWN,
        UNIT_ORIENT_LEFT_UP
    }



    /// <summary>
    /// Tactical unit.
    /// We allow unit merging so the basic properties may change and
    /// are therefore no pointers into the library but shallow copies
    /// (this means an entry's id, name, icons, sounds ARE pointers 
    /// and will not be touched).
    /// To determine wether a unit has a transporter unit.trsp_prop.id
    /// is checked. ( 0 = no transporter, 1 = has transporter )
    /// </summary>
	[Serializable]
   	public class Unit
    {
        /// <summary>
        /// unit life bar stuff */
        /// there aren't used colors but bitmaps with small colored tiles
        /// </summary>

        public const int BAR_WIDTH = 31;
        public const int BAR_HEIGHT = 4;
        public const int BAR_TILE_WIDTH = 3;
        public const int BAR_TILE_HEIGHT = 4;

        [XmlIgnore]
        public Unit_Lib_Entry prop;        /* properties */
        public Unit_Lib_Entry trsp_prop;   /* transporters properties */
        [XmlIgnore]
        public Unit_Lib_Entry sel_prop;   /* selected props: either prop or trsp_prop */
        [XmlIgnore]
        public Unit backup;                /* used to backup various unit values that may temporaly change (e.g. expected losses) */
        public string name;                /* unit name */
        [XmlIgnore]
        public Player player;              /* controlling player */
        public Nation nation;              /* nation unit belongs to */
        [XmlIgnore]
        public Terrain_Type terrain;        /* terrain the unit is currently on */
        public int x, y;                   /* map position */
        public int str;                    /* strength */
        public int entr;                   /* entrenchment */
		[XmlIgnore]
        public int exp;                    /* experience */
        public int exp_level;              /* exp level computed from exp */
        public int delay;                  /* delay in turns before unit becomes avaiable
	                                   as reinforcement */
        [XmlIgnore]
        public UnitEmbarkTypes embark;                 /* embark type */
        [XmlIgnore]
        public UnitLookingDirection orient;  /* current orientation */
        [XmlIgnore]
        public int icon_offset;            /* offset in unit's sel_prop.icon */
        [XmlIgnore]
        public int icon_tiny_offset;       /* offset in sep_prop.tiny_icon */
        [XmlIgnore]
        public int supply_level;           /* in percent; military targets are centers of supply */
        [XmlIgnore]
        public int cur_fuel;               /* current fuel */
        [XmlIgnore]
        public int cur_ammo;               /* current ammo */
        [XmlIgnore]
        public int cur_mov;                /* movement points this turn */
        [XmlIgnore]
        public int cur_atk_count;          /* number of attacks this turn */
        [XmlIgnore]
        public bool unused;                 /* unit didn't take an action this turn so far */
        [XmlIgnore]
        public int damage_bar_width;       /* current life bar width in map.life_icons */
        [XmlIgnore]
        public int damage_bar_offset;      /* offset in map.damage_icons */
        [XmlIgnore]
        public int suppr;                  /* unit suppression for a single fight 
	                                   (caused by artillery, cleared after fight) */
        [XmlIgnore]
        public int turn_suppr;             /* unit suppression for whole turn
	                                   caused by tactical bombing, low fuel(< 20) or 
	                                   low ammo (< 2) */
        [XmlIgnore]
        public bool is_guarding;            /* do not include to list when cycling units */
        [XmlIgnore]
        public int killed;                 /* 1: remove from map & delete this unit
					                   2: delete only */
        [XmlIgnore]
        public bool fresh_deploy;           /* if this is true this unit was deployed in this turn
	                                   and as long as it is unused it may be undeployed in the 
	                                   same turn */
        public string tag; /* if tag is set the unit belongs to a unit group that
	                                   is monitored by the victory conditions units_killed()
	                                   and units_saved() */
        [XmlIgnore]
        public int eval_score;             /* between 0 - 1000 indicating the worth of the unit */
        /* AI eval */
        [XmlIgnore]
        public int target_score;           /* when targets of an AI unit are gathered this value is set
	                                   to the result score for attack of unit on this target */
		
        /*
        ====================================================================
        CreateAction a unit by passing a Unit struct with the following stuff set:
          x, y, str, entr, exp, delay, orient, nation, player.
        This function will use the passed values to create a Unit struct
        with all values set then.
        ====================================================================
        */
        public static Unit CreateUnit(Unit_Lib_Entry prop, Unit_Lib_Entry trsp_prop, Unit unit_base)
        {
            Unit unit;
            if (prop == null) return null;
            unit = new Unit();
            /* shallow copy of properties */
            unit.prop = prop;
            unit.sel_prop = unit.prop;
            unit.embark = UnitEmbarkTypes.EMBARK_NONE;
            /* assign the passed transporter without any check */
            if (trsp_prop != null && (prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING && (prop.flags & UnitFlags.SWIMMING) != UnitFlags.SWIMMING)
            {
                unit.trsp_prop = trsp_prop;
                /* a sea/air ground transporter is active per default */
                if ((trsp_prop.flags & UnitFlags.SWIMMING) == UnitFlags.SWIMMING)
                {
                    unit.embark = UnitEmbarkTypes.EMBARK_SEA;
                    unit.sel_prop = unit.trsp_prop;
                }
                if ((trsp_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
                {
                    unit.embark = UnitEmbarkTypes.EMBARK_AIR;
                    unit.sel_prop = unit.trsp_prop;
                }
            }
            /* copy the base values */
            unit.delay = unit_base.delay;
            unit.x = unit_base.x; unit.y = unit_base.y;
            unit.str = unit_base.str; unit.entr = unit_base.entr;
            unit.player = unit_base.player;
            unit.nation = unit_base.nation;
            unit.name = unit_base.name;
            unit.AddExperience(unit_base.exp_level * 100);
            unit.orient = unit_base.orient;
            unit.AdjustIcon();
            unit.unused = true;
            unit.supply_level = 100;
            unit.cur_ammo = unit.prop.ammo;
            unit.cur_fuel = unit.prop.fuel;
            if ((unit.cur_fuel == 0) && (unit.trsp_prop != null) &&
                (!string.IsNullOrEmpty(unit.trsp_prop.id)) && unit.trsp_prop.fuel > 0)
                unit.cur_fuel = unit.trsp_prop.fuel;
            unit.tag = unit_base.tag;
            /* update life bar properties */
            update_bar(unit);
            /* allocate backup mem */
            unit.backup = new Unit();
            return unit;
        }


        /*
        ====================================================================
        Delete a unit. Pass the pointer as void* to allow usage as 
        callback for a list.
        ====================================================================
        */
        public void DeleteUnit(Unit ptr)
        {
            throw new NotImplementedException();
        }

        /*
        ====================================================================
        Give unit a generic name.
        ====================================================================
        */
        public void SetGenericName(int number, string stem)
        {
            this.name = Ordinal(number) + " " + stem;
        }
		
		public string DeleteOrdinal(string name){
			string str ="";
			string[] aux = name.Split();
			for (int i=1; i<aux.Length-1; i++){
				str = str + aux[i]+" ";
			}
			str = str + aux[aux.Length-1];
			return str;
		}
        protected static string Ordinal(int number)
        {
            string suffix = String.Empty;

            int ones = number % 10;
            int tens = (int)Math.Floor(number / 10M) % 10;

            if (tens == 1)
            {
                suffix = "th";
            }
            else
            {
                switch (ones)
                {
                    case 1:
                        suffix = "st";
                        break;

                    case 2:
                        suffix = "nd";
                        break;

                    case 3:
                        suffix = "rd";
                        break;

                    default:
                        suffix = "th";
                        break;
                }
            }
            return String.Format("{0}{1}", number, suffix);
        }

        /*
        ====================================================================
        Update unit icon according to it's orientation.
        ====================================================================
        */
        public void AdjustIcon()
        {
            this.icon_offset = this.sel_prop.icon_w * (int)this.orient;
            this.icon_tiny_offset = this.sel_prop.icon_tiny_w * (int)this.orient;
        }

        /*
        ====================================================================
        Adjust orientation (and adjust icon) of unit if looking towards x,y.
        ====================================================================
        */
        public void AdjustOrient(int x, int y)
        {

            if (this.prop.icon_type == UnitIconStyle.UNIT_ICON_SINGLE)
            {
                if (x < this.x)
                {
                    this.orient = UnitLookingDirection.UNIT_ORIENT_LEFT;
                    this.icon_offset = this.sel_prop.icon_w;
                    this.icon_tiny_offset = this.sel_prop.icon_tiny_w;
                }
                else
                    if (x > this.x)
                    {
                        this.orient = UnitLookingDirection.UNIT_ORIENT_RIGHT;
                        this.icon_offset = 0;
                        this.icon_tiny_offset = 0;
                    }
            }
            else
            {
                /* not implemented yet */
            }
        }

        /*
        ====================================================================
        Check if unit can supply something (ammo, fuel, anything) and 
        return the amount that is supplyable.
        ====================================================================
        */
        public enum UNIT_SUPPLY
        {
            UNIT_SUPPLY_AMMO = 1,
            UNIT_SUPPLY_FUEL,
            UNIT_SUPPLY_ANYTHING,
            UNIT_SUPPLY_ALL
        }

        public bool CheckSupply(UNIT_SUPPLY type, out int missing_ammo, out int missing_fuel)
        {
            bool ret = false;
            int max_fuel = this.sel_prop.fuel;
            missing_ammo = 0;
            missing_fuel = 0;
            /* no supply near or already moved? */
            if (this.embark == UnitEmbarkTypes.EMBARK_SEA || this.embark == UnitEmbarkTypes.EMBARK_AIR) return false;
            if (this.supply_level == 0) return false;
            if (!this.unused) return false;
            /* supply ammo? */
            if (type == UNIT_SUPPLY.UNIT_SUPPLY_AMMO || type == UNIT_SUPPLY.UNIT_SUPPLY_ANYTHING)
                if (this.cur_ammo < this.prop.ammo)
                {
                    ret = true;
                    missing_ammo = this.prop.ammo - this.cur_ammo;
                }
            if (type == UNIT_SUPPLY.UNIT_SUPPLY_AMMO) return ret;
            /* if we have a ground transporter assigned we need to use it's fuel as max */
            if (this.CheckFuelUsage() && max_fuel == 0)
                max_fuel = this.trsp_prop.fuel;
            /* supply fuel? */
            if (type == UNIT_SUPPLY.UNIT_SUPPLY_FUEL || type == UNIT_SUPPLY.UNIT_SUPPLY_ANYTHING)
                if (this.cur_fuel < max_fuel)
                {
                    ret = true;
                    missing_fuel = max_fuel - this.cur_fuel;
                }
            return ret;
        }

        /*
        ====================================================================
        Supply percentage of maximum fuel/ammo/both
        Return True if unit was supplied.
        ====================================================================
        */
        public bool SupplyIntern(UNIT_SUPPLY type)
        {
            int amount_ammo, amount_fuel, max, supply_amount;
            bool supplied = false;
            /* ammo */
            if (type == UNIT_SUPPLY.UNIT_SUPPLY_AMMO || type == UNIT_SUPPLY.UNIT_SUPPLY_ALL)
                if (this.CheckSupply(UNIT_SUPPLY.UNIT_SUPPLY_AMMO, out amount_ammo, out amount_fuel))
                {
                    max = this.cur_ammo + amount_ammo;
                    supply_amount = this.supply_level * max / 100;
                    if (supply_amount == 0) supply_amount = 1; /* at least one */
                    this.cur_ammo += supply_amount;
                    if (this.cur_ammo > max) this.cur_ammo = max;
                    supplied = true;
                }
            /* fuel */
            if (type == UNIT_SUPPLY.UNIT_SUPPLY_FUEL || type == UNIT_SUPPLY.UNIT_SUPPLY_ALL)
                if (this.CheckSupply(UNIT_SUPPLY.UNIT_SUPPLY_FUEL, out amount_ammo, out amount_fuel))
                {
                    max = this.cur_fuel + amount_fuel;
                    supply_amount = this.supply_level * max / 100;
                    if (supply_amount == 0) supply_amount = 1; /* at least one */
                    this.cur_fuel += supply_amount;
                    if (this.cur_fuel > max) this.cur_fuel = max;
                    supplied = true;
                }
            return supplied;
        }

        public bool Supply(UNIT_SUPPLY type)
        {
            bool supplied = this.SupplyIntern(type);
            if (supplied)
            {
                /* no other actions allowed */
                this.unused = false;
                this.cur_mov = 0;
                this.cur_atk_count = 0;
            }
            return supplied;
        }

        /*
        ====================================================================
        Check if a unit uses fuel in it's current state (embarked or not).
        ====================================================================
        */
        public bool CheckFuelUsage()
        {
            if (this.embark == UnitEmbarkTypes.EMBARK_SEA || this.embark == UnitEmbarkTypes.EMBARK_AIR)
                return false;
            if (this.prop.fuel > 0)
                return true;
            if (this.trsp_prop != null && this.trsp_prop.id != null && this.trsp_prop.fuel > 0) return true;
            return false;
        }

        /*
        ====================================================================
        Add experience and compute experience level. 
        Return True if levelup.
        ====================================================================
        */
        public bool AddExperience(int exp)
        {
            int old_level = this.exp_level;
            this.exp += exp;
            if (this.exp >= 500) this.exp = 500;
            this.exp_level = this.exp / 100;
            return (old_level != this.exp_level);
        }

        /*
        ====================================================================
        Mount/unmount unit to ground transporter.
        ====================================================================
        */
        public void Mount()
        {
            if (this.trsp_prop == null || string.IsNullOrEmpty(this.trsp_prop.id) || this.embark != UnitEmbarkTypes.EMBARK_NONE) return;
            /* set prop pointer */
            this.sel_prop = this.trsp_prop;
            this.embark = UnitEmbarkTypes.EMBARK_GROUND;
            /* adjust pic offset */
            this.AdjustIcon();
            /* no entrenchment when mounting */
            this.entr = 0;
        }

        public void Unmount()
        {
            if (this.embark != UnitEmbarkTypes.EMBARK_GROUND) return;
            /* set prop pointer */
            this.sel_prop = this.prop;
            this.embark = UnitEmbarkTypes.EMBARK_NONE;
            /* adjust pic offset */
            this.AdjustIcon();
            /* no entrenchment when mounting */
            this.entr = 0;
        }

        /*
        ====================================================================
        Check if units are close to each other. This means on neighbored
        hex tiles.
        ====================================================================
        */
#if TODO_RR
        public bool IsClose(Unit target)
        {
            return Misc.is_close(this.x, this.y, target.x, target.y);
        }
#endif
        /*
        ====================================================================
        Check if unit may activly attack (unit initiated attack) or
        passivly attack (target initated attack, unit defenses) the target.
        ====================================================================
        */
        public enum UNIT_ATTACK
        {
            UNIT_ACTIVE_ATTACK = 0,
            UNIT_PASSIVE_ATTACK,
            UNIT_DEFENSIVE_ATTACK
        }
#if TODO_RR
        public bool CheckAttack(Unit target, UNIT_ATTACK type)
        {
            if (target == null || this == target) return false;
            if (Player.player_is_ally(this.player, target.player)) return false;
            if (((this.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) &&
                (target.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING)
                if (this.sel_prop.rng == 0)
                    if (this.x != target.x || this.y != target.y)
                        return false; /* range 0 means above unit for an aircraft */
            /* if the target flys and the unit is ground with a range of 0 the aircraft
               may only be harmed when above unit */
            if (((this.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING) &&
                (target.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
                if (this.sel_prop.rng == 0)
                    if (this.x != target.x || this.y != target.y)
                        return false;
            /* only destroyers may harm submarines */
            if (((target.sel_prop.flags & UnitFlags.DIVING) == UnitFlags.DIVING) &&
                (this.sel_prop.flags & UnitFlags.DESTROYER) != UnitFlags.DESTROYER) return false;
            if ((Engine.terrain.weatherTypes[Scenario.cur_weather].flags & WEATHER_FLAGS.NO_AIR_ATTACK) == WEATHER_FLAGS.NO_AIR_ATTACK)
            {
                if ((this.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) return false;
                if ((target.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) return false;
            }
            if (type == UNIT_ATTACK.UNIT_ACTIVE_ATTACK)
            {
                /* agressor */
                if (this.cur_ammo <= 0) return false;
                if (this.sel_prop.atks[target.sel_prop.trgt_type] <= 0) return false;
                if (this.cur_atk_count == 0) return false;
                if (!this.IsClose(target) && Misc.get_dist(this.x, this.y, target.x, target.y) > this.sel_prop.rng) return false;
            }
            else
                if (type == UNIT_ATTACK.UNIT_DEFENSIVE_ATTACK)
                {
                    /* defensive fire */
                    if (this.sel_prop.atks[target.sel_prop.trgt_type] <= 0) return false;
                    if (this.cur_ammo <= 0) return false;
                    if ((this.sel_prop.flags & (UnitFlags.INTERCEPTOR | UnitFlags.ARTILLERY | UnitFlags.AIR_DEFENSE)) == 0) return false;
                    if ((target.sel_prop.flags & (UnitFlags.ARTILLERY | UnitFlags.AIR_DEFENSE | UnitFlags.SWIMMING)) != 0) return false;
                    if ((this.sel_prop.flags & UnitFlags.INTERCEPTOR) == UnitFlags.INTERCEPTOR)
                    {
                        /* the interceptor is propably not beside the attacker so the range check is different
                         * can't be done here because the unit the target attacks isn't passed so 
                         *  GetDefensiveFireUnits() must have a look itself 
                         */
                    }
                    else
                        if (Misc.get_dist(this.x, this.y, target.x, target.y) > this.sel_prop.rng) return false;
                }
                else
                {
                    /* counter-attack */
                    if (this.cur_ammo <= 0) return false;
                    if (!this.IsClose(target) && Misc.get_dist(this.x, this.y, target.x, target.y) > this.sel_prop.rng) return false;
                    if (this.sel_prop.atks[target.sel_prop.trgt_type] == 0) return false;
                    /* artillery may only defend against close units */
                    if ((this.sel_prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY)
                        if (!this.IsClose(target))
                            return false;
                    /* you may defend against artillery only when close */
                    if ((target.sel_prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY)
                        if (!this.IsClose(target))
                            return false;
                }
            return true;
        }
#endif
        /*
        ====================================================================
        Compute damage/supression the target takes when unit attacks
        the target. No properties will be changed. If 'real' is set
        the dices are rolled else it's a stochastical prediction.
        ====================================================================
        */
#if TODO_RR
        public static void GetDamage(Unit aggressor, Unit unit, Unit target,
                              UNIT_ATTACK type,
                              bool real, bool rugged_def,
                              out int damage, out int suppr)
        {
            int atk_strength, max_roll, min_roll, die_mod;
            int atk_grade, def_grade, diff, result;
            float suppr_chance, kill_chance;
            /* use PG's formula to compute the attack/defense grade*/
            /* basic attack */
            atk_grade = Math.Abs(unit.sel_prop.atks[target.sel_prop.trgt_type]);
#if DEBUG
            if (real) Console.WriteLine("{0} attacks:", unit.name);
            if (real) Console.WriteLine("  base:   {0}", atk_grade);
            if (real) Console.WriteLine("  exp:    +{0}", unit.exp_level);
#endif
            /* experience */
            atk_grade += unit.exp_level;
            /* target on a river? */
            if ((target.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING)
                if ((target.terrain.flags[Scenario.cur_weather] & Terrain_flags.RIVER) == Terrain_flags.RIVER)
                {
                    atk_grade += 4;
#if DEBUG
                    if (real) Console.WriteLine("  river:  +4");
#endif
                }
            /* counterattack of rugged defense unit? */
            if (type == UNIT_ATTACK.UNIT_PASSIVE_ATTACK && rugged_def)
            {
                atk_grade += 4;
#if DEBUG
                if (real) Console.WriteLine("  rugdef: +4");
#endif
            }
#if DEBUG
            if (real) Console.WriteLine("---\n{0} defends:", target.name);
#endif
            /* basic defense */
            if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
                def_grade = target.sel_prop.def_air;
            else
            {
                def_grade = target.sel_prop.def_grnd;
                /* apply close defense? */
                if ((unit.sel_prop.flags & UnitFlags.INFANTRY) == UnitFlags.INFANTRY)
                    if ((target.sel_prop.flags & UnitFlags.INFANTRY) != UnitFlags.INFANTRY)
                        if ((target.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING)
                            if ((target.sel_prop.flags & UnitFlags.SWIMMING) != UnitFlags.SWIMMING)
                            {
                                if (target == aggressor)
                                    if ((unit.terrain.flags[Scenario.cur_weather] & Terrain_flags.INF_CLOSE_DEF) == Terrain_flags.INF_CLOSE_DEF)
                                        def_grade = target.sel_prop.def_cls;
                                if (unit == aggressor)
                                    if ((target.terrain.flags[Scenario.cur_weather] & Terrain_flags.INF_CLOSE_DEF) == Terrain_flags.INF_CLOSE_DEF)
                                        def_grade = target.sel_prop.def_cls;
                            }
            }
#if DEBUG
            if (real) Console.WriteLine("  base:   {0}", def_grade);
            if (real) Console.WriteLine("  exp:    +{0}", target.exp_level);
#endif
            /* experience */
            def_grade += target.exp_level;
            /* attacker on a river or swamp? */
            if ((unit.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING)
                if ((unit.sel_prop.flags & UnitFlags.SWIMMING) != UnitFlags.SWIMMING)
                    if ((target.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING)
                    {
                        if ((unit.terrain.flags[Scenario.cur_weather] & Terrain_flags.SWAMP) == Terrain_flags.SWAMP)
                        {
                            def_grade += 4;
#if DEBUG
                            if (real) Console.WriteLine("  swamp:  +4");
#endif
                        }
                        else
                            if ((unit.terrain.flags[Scenario.cur_weather] & Terrain_flags.RIVER) == Terrain_flags.RIVER)
                            {
                                def_grade += 4;
#if DEBUG
                                if (real) Console.WriteLine("  river:  +4");
#endif
                            }
                    }
            /* rugged defense? */
            if (type == UNIT_ATTACK.UNIT_ACTIVE_ATTACK && rugged_def)
            {
                def_grade += 4;
#if DEBUG
                if (real) Console.WriteLine("  rugdef: +4");
#endif
            }
            /* entrenchment */
            if ((unit.sel_prop.flags & UnitFlags.IGNORE_ENTR) == UnitFlags.IGNORE_ENTR)
                def_grade += 0;
            else
            {
                if ((unit.sel_prop.flags & UnitFlags.INFANTRY) == UnitFlags.INFANTRY)
                    def_grade += target.entr / 2;
                else
                    def_grade += target.entr;
#if DEBUG
                if (real) Console.WriteLine("  entr:   +{0}",
                ((unit.sel_prop.flags & UnitFlags.INFANTRY) == UnitFlags.INFANTRY) ? target.entr / 2 : target.entr);
#endif
            }
            /* naval vs ground unit */
            if ((unit.sel_prop.flags & UnitFlags.SWIMMING) != UnitFlags.SWIMMING)
                if ((unit.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING)
                    if ((target.sel_prop.flags & UnitFlags.SWIMMING) == UnitFlags.SWIMMING)
                    {
                        def_grade += 8;
#if DEBUG
                        if (real) Console.WriteLine("  naval: +8");
#endif
                    }
            /* bad weather? */
            if (unit.sel_prop.rng > 0)
                if ((Engine.terrain.weatherTypes[Scenario.cur_weather].flags & WEATHER_FLAGS.BAD_SIGHT) == WEATHER_FLAGS.BAD_SIGHT)
                {
                    def_grade += 3;
#if DEBUG
                    if (real) Console.WriteLine("  sight: +3");
#endif
                }
            /* initiating attack against artillery? */
            if (type == UNIT_ATTACK.UNIT_PASSIVE_ATTACK)
                if ((unit.sel_prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY)
                {
                    def_grade += 3;
#if DEBUG
                    if (real) Console.WriteLine("  def vs art: +3");
#endif
                }
            /* infantry versus anti_tank? */
            if ((target.sel_prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY)
                if ((unit.sel_prop.flags & UnitFlags.ANTI_TANK) == UnitFlags.ANTI_TANK)
                {
                    def_grade += 2;
#if DEBUG
                    if (real) Console.WriteLine("  antitnk:+2");
#endif
                }
            /* no fuel makes attacker less effective */
            if (unit.CheckFuelUsage() && unit.cur_fuel == 0)
            {
                def_grade += 4;
#if DEBUG
                if (real) Console.WriteLine("  lowfuel:+4");
#endif
            }
            /* attacker strength */
            atk_strength = unit_get_cur_str(unit);
#if DEBUG
            if (real && atk_strength != unit_get_cur_str(unit))
                Console.WriteLine("---\n{0} with half strength", unit.name);
#endif
            /*  PG's formula:
        get difference between attack and defense
        strike for each strength point with 
          if ( diff <= 4 ) 
              D20 + diff
          else
              D20 + 4 + 0.4 * ( diff - 4 )
        suppr_fire flag set: 1-10 miss, 11-18 suppr, 19+ kill
        normal: 1-10 miss, 11-12 suppr, 13+ kill */
            diff = atk_grade - def_grade; if (diff < -7) diff = -7;
            damage = 0;
            suppr = 0;
#if DEBUG
            if (real)
            {
                Console.WriteLine("---\n{0} x {1} -. {2} x {3}",
                        atk_strength, atk_grade, unit_get_cur_str(target), def_grade);
            }
#endif
            /* get the chances for suppression and kills (computed here
       to use also for debug info */
            suppr_chance = kill_chance = 0;
            die_mod = (diff <= 4 ? diff : 4 + 2 * (diff - 4) / 5);
            min_roll = 1 + die_mod; max_roll = 20 + die_mod;
            /* get chances for suppression and kills */
            if ((unit.sel_prop.flags & UnitFlags.SUPPR_FIRE) == UnitFlags.SUPPR_FIRE)
            {
                int limit = (type == UNIT_ATTACK.UNIT_DEFENSIVE_ATTACK) ? 20 : 18;
                if (limit - min_roll >= 0)
                    suppr_chance = 0.05f * (Math.Min(limit, max_roll) - Math.Max(11, min_roll) + 1);
                if (max_roll > limit)
                    kill_chance = 0.05f * (max_roll - Math.Max(limit + 1, min_roll) + 1);
            }
            else
            {
                if (12 - min_roll >= 0)
                    suppr_chance = 0.05f * (Math.Min(12, max_roll) - Math.Max(11, min_roll) + 1);
                if (max_roll > 12)
                    kill_chance = 0.05f * (max_roll - Math.Max(13, min_roll) + 1);
            }
            if (suppr_chance < 0) suppr_chance = 0; if (kill_chance < 0) kill_chance = 0;
            if (real)
            {
#if DEBUG
                Console.WriteLine("Roll: D20 + {0} (Kill: {1}, Suppr: {2})",
                diff <= 4 ? diff : 4 + 2 * (diff - 4) / 5,
                (int)(100 * kill_chance), (int)(100 * suppr_chance));
#endif
                while (atk_strength-- > 0)
                {
                    if (diff <= 4)
                        result = Misc.DICE(20) + diff;
                    else
                        result = Misc.DICE(20) + 4 + 2 * (diff - 4) / 5;
                    if ((unit.sel_prop.flags & UnitFlags.SUPPR_FIRE) == UnitFlags.SUPPR_FIRE)
                    {
                        int limit = (type == UNIT_ATTACK.UNIT_DEFENSIVE_ATTACK) ? 20 : 18;
                        if (result >= 11 && result <= limit)
                            suppr++;
                        else
                            if (result >= limit + 1)
                                damage++;
                    }
                    else
                    {
                        if (result >= 11 && result <= 12)
                            suppr++;
                        else
                            if (result >= 13)
                                damage++;
                    }
                }
#if DEBUG
                Console.WriteLine("Kills: {0}, Suppression: {1}", damage, suppr);
#endif
            }
            else
            {
                suppr = (int)(suppr_chance * atk_strength);
                damage = (int)(kill_chance * atk_strength);
            }
        }
#endif
        /*
        ====================================================================
        Execute a single fight (no defensive fire check) with random values.
        SurpriseAttack() handles an attack with a surprising target
        (e.g. Out Of The Sun)
        If a rugged defense occured in a normal fight (surprise_attack is
        always rugged) 'rugged_def' is set.
        Ammo is decreased and experience gained.
        NormalAttack() accepts UNIT_ACTIVE_ATTACK or 
        UNIT_DEFENSIVE_ATTACK as 'type' depending if this unit supports
        or activly attacks.
        ====================================================================
        */
        [Flags]
        public enum FIGHT_TYPES
        {
            AR_NONE = 0,            /* nothing special */
            AR_UNIT_ATTACK_BROKEN_UP = (1 << 1),   /* target stroke first and unit broke up attack */
            AR_UNIT_SUPPRESSED = (1 << 2),   /* unit alive but fully suppressed */
            AR_TARGET_SUPPRESSED = (1 << 3),   /* dito */
            AR_UNIT_KILLED = (1 << 4),   /* real strength is 0 */
            AR_TARGET_KILLED = (1 << 5),   /* dito */
            AR_RUGGED_DEFENSE = (1 << 6),   /* target made rugged defense */
            AR_EVADED = (1 << 7),   /* unit evaded */
        }
#if TODO_RR
        public FIGHT_TYPES NormalAttack(Unit target, UNIT_ATTACK type)
        {
            return this.Attack(target, type, true, false);
        }
#endif
#if TODO_RR
        public FIGHT_TYPES SurpriseAttack(Unit target)
        {
            return this.Attack(target, UNIT_ATTACK.UNIT_ACTIVE_ATTACK, true, true);
        }
#endif
        /*
        ====================================================================
        Go through a complete battle unit vs. target including known(!)
        defensive support stuff and with no random modifications.
        Return the final damage taken by both units.
        As the terrain may have influence the id of the terrain the battle
        takes place (defending unit's hex) is provided.
        ====================================================================
        */
#if TODO_RR
        public static void GetExpectedLosses(Unit unit, Unit target, out int unit_damage, out int target_damage)
        {
            int damage, suppr;
            List<Unit> df_units = new List<Unit>();
#if DEBUG_ATTACK
    printf( "***********************\n" );
#endif
            unit.GetDefensiveFireUnits(target, Scenario.vis_units, ref df_units);
            unit_backup(unit);
            unit_backup(target);
            /* let defensive fire go to work (no chance to defend against this) */
            foreach (Unit df in df_units)
            {
                GetDamage(unit, df, unit, UNIT_ATTACK.UNIT_DEFENSIVE_ATTACK, false, false, out damage, out suppr);
                if (unit.ApplyDamage(damage, suppr, null) == 0) break;
            }
            /* actual fight if attack has strength remaining */
            if (unit_get_cur_str(unit) > 0)
                unit.Attack(target, UNIT_ATTACK.UNIT_ACTIVE_ATTACK, false, false);
            /* get done damage */
            unit_damage = unit.str;
            target_damage = target.str;
            unit_restore(ref unit);
            unit_restore(ref target);
            unit_damage = unit.str - unit_damage;
            target_damage = target.str - target_damage;
        }
#endif
        /*
        ====================================================================
        This function checks 'units' for supporters of 'target'
        that will give defensive fire to before the real battle
        'unit' vs 'target' takes place. These units are put to 'df_units'
        (which is not created here)
        ====================================================================
        */
#if TODO_RR
        public void GetDefensiveFireUnits(Unit target, List<Unit> units, ref List<Unit> df_units)
        {
            df_units.Clear();
            if ((this.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
            {
                foreach (Unit entry in units)
                {
                    if (entry.killed != 0) continue;
                    if (entry == target) continue;
                    if (entry == this) continue;
                    /* bombers -- intercepting impossibly covered by CheckAttack() */
                    if ((target.sel_prop.flags & UnitFlags.INTERCEPTOR) != UnitFlags.INTERCEPTOR)
                        if (target.IsClose(entry))
                            if ((entry.sel_prop.flags & UnitFlags.INTERCEPTOR) == UnitFlags.INTERCEPTOR)
                                if (Player.player_is_ally(entry.player, target.player))
                                    if (entry.cur_ammo > 0)
                                    {
                                        df_units.Add(entry);
                                        continue;
                                    }
                    /* air-defense */
                    if ((entry.sel_prop.flags & UnitFlags.AIR_DEFENSE) == UnitFlags.AIR_DEFENSE)
                        /* FlaK will not give support when an air-to-air attack is
                         * taking place. First, in reality it would prove distastrous,
                         * second, Panzer General doesn't allow it, either.
                         */
                        if ((target.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING)
                            if (target.IsClose(entry)) /* adjacent help only */
                                if (entry.CheckAttack(this, UNIT_ATTACK.UNIT_DEFENSIVE_ATTACK))
                                    df_units.Add(entry);
                }
            }
            else if (this.sel_prop.rng == 0)
            {
                /* artillery for melee combat; if unit attacks ranged, there is no 
                   support */
                foreach (Unit entry in units)
                {
                    if (entry.killed != 0) continue;
                    if (entry == target) continue;
                    if (entry == this) continue;
                    /* HACK: An artillery with range 1 cannot support adjacent units but
                       should do so. So we allow to give defensive fire on a range of 2
                       like a normal artillery */
                    if (((entry.sel_prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY) && entry.sel_prop.rng == 1)
                        if (target.IsClose(entry))
                            if (Player.player_is_ally(entry.player, target.player))
                                if (entry.cur_ammo > 0)
                                {
                                    df_units.Add(entry);
                                    continue;
                                }
                    /* normal artillery */
                    if ((entry.sel_prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY)
                        if (target.IsClose(entry)) /* adjacent help only */
                            if (entry.CheckAttack(this, UNIT_ATTACK.UNIT_DEFENSIVE_ATTACK))
                                df_units.Add(entry);
                }
            }
            /* randomly remove all support but one */
            if (df_units.Count > 0)
            {
                Unit entry = df_units[Misc.RANDOM(0, df_units.Count - 1)];
                df_units.Clear();
                df_units.Add(entry);
            }
        }
#endif


        /*
        ====================================================================
        Check if these two units are allowed to merge with each other.
        ====================================================================
        */
        public bool CheckMerge(Unit source)
        {
            /* units must not be sea/air embarked */
            if (this.embark != UnitEmbarkTypes.EMBARK_NONE || source.embark != UnitEmbarkTypes.EMBARK_NONE) return false;
            /* same class */
            if (this.prop.unit_class != source.prop.unit_class) return false;
            /* same player */
            if (!Player.player_is_ally(this.player, source.player)) return false;
            /* first unit must not have moved so far */
            if (!this.unused) return false;
            /* both units must have same movement type */
            if (this.prop.mov_type != source.prop.mov_type) return false;
            /* the unit strength must not exceed limit */
            if (this.str + source.str > 13) return false;
            /* fortresses (unit-class 7) could not merge */
            if (this.prop.unit_class == 7) return false;
            /* artillery with different ranges may not merge */
            if (((this.prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY) &&
                (this.prop.rng != source.prop.rng))
                return false;
            /* not failed so far: allow merge */
            return true;
        }


        /*
        ====================================================================
        Get the maximum strength the unit can give for a split in its
        current state. Unit must have at least strength 3 remaining.
        ====================================================================
        */
        public int GetSplitStrength()
        {
            if (this.embark != UnitEmbarkTypes.EMBARK_NONE) return 0;
            if (!this.unused) return 0;
            if (this.str <= 4) return 0;
            if (this.prop.unit_class == 7) return 0; /* fortress */
            return this.str - 4;
        }

        /*
        ====================================================================
        Merge these two units: unit is the new unit and source must be
        removed from map and memory after this function was called.
        ====================================================================
        */
#if TODO_RR
        public void Merge(Unit unit, Unit source)
        {

            /* units relative weight */
            float weight1, weight2, total;
            int i;
            /* compute weight */
            weight1 = unit.str; weight2 = source.str;
            total = unit.str + source.str;
            /* adjust so weight1 + weigth2 = 1 */
            weight1 /= total; weight2 /= total;
            /* no other actions allowed */
            unit.unused = false; unit.cur_mov = 0; unit.cur_atk_count = 0;
            /* repair damage */
            unit.str += source.str;
            /* reorganization costs some entrenchment: the new units are assumed to have
               entrenchment 0 since they come. new entr is rounded weighted sum */
            unit.entr = (int)Math.Floor((float)unit.entr * weight1 + 0.5); /* + 0 * weight2 */
            /* update experience */
            i = (int)(weight1 * unit.exp + weight2 * source.exp);
            unit.exp = 0; unit.AddExperience(i);
            /* update unit::prop */
            /* related initiative */
            unit.prop.ini = (int)(weight1 * unit.prop.ini + weight2 * source.prop.ini);
            /* minimum movement */
            if (source.prop.mov < unit.prop.mov)
                unit.prop.mov = source.prop.mov;
            /* maximum spotting */
            if (source.prop.spt > unit.prop.spt)
                unit.prop.spt = source.prop.spt;
            /* maximum range */
            if (source.prop.rng > unit.prop.rng)
                unit.prop.rng = source.prop.rng;
            /* relative attack count */
            unit.prop.atk_count = (int)(weight1 * unit.prop.atk_count + weight2 * source.prop.atk_count);
            if (unit.prop.atk_count == 0) unit.prop.atk_count = 1;
            /* relative attacks */
            /* if attack is negative simply use absolute value; only restore negative if both units are negative */
            for (i = 0; i < DB.UnitLib.trgt_type_count; i++)
            {
                bool neg = (unit.prop.atks[i] < 0 && source.prop.atks[i] < 0);
                unit.prop.atks[i] = (int)(weight1 * Math.Abs(unit.prop.atks[i]) + weight2 * (source.prop.atks[i]));
                if (neg) unit.prop.atks[i] *= -1;
            }
            /* relative defence */
            unit.prop.def_grnd = (int)(weight1 * unit.prop.def_grnd + weight2 * source.prop.def_grnd);
            unit.prop.def_air = (int)(weight1 * unit.prop.def_air + weight2 * source.prop.def_air);
            unit.prop.def_cls = (int)(weight1 * unit.prop.def_cls + weight2 * source.prop.def_cls);
            /* relative ammo */
            unit.prop.ammo = (int)(weight1 * unit.prop.ammo + weight2 * source.prop.ammo);
            unit.cur_ammo = (int)(weight1 * unit.cur_ammo + weight2 * source.cur_ammo);
            /* relative fuel */
            unit.prop.fuel = (int)(weight1 * unit.prop.fuel + weight2 * source.prop.fuel);
            unit.cur_fuel = (int)(weight1 * unit.cur_fuel + weight2 * source.cur_fuel);
            /* merge flags */
            unit.prop.flags |= source.prop.flags;
            /* sounds, picture are kept */
            /* unit::trans_prop isn't updated so far: */
            /* transporter of first unit is kept if any else second unit's transporter is used */
            if (string.IsNullOrEmpty(unit.trsp_prop.id) && !string.IsNullOrEmpty(source.trsp_prop.id))
            {
#if TODO
                memcpy(&unit.trsp_prop, &source.trsp_prop, sizeof(Unit_Lib_Entry));
                
#endif

                /* as this must be a ground transporter copy current fuel value */
                unit.cur_fuel = source.cur_fuel;
                throw new NotImplementedException();
            }
            update_bar(unit);


        }
#endif
        /*
        ====================================================================
        Return True if unit uses a ground transporter.
        ====================================================================
        */
        public bool CheckGroundTransporter()
        {
            if (this.trsp_prop == null || string.IsNullOrEmpty(this.trsp_prop.id)) return false;
            if ((this.trsp_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) return false;
            if ((this.trsp_prop.flags & UnitFlags.SWIMMING) == UnitFlags.SWIMMING) return false;
            return true;
        }

        /*
        ====================================================================
        Backup unit to its backup pointer (shallow copy)
        ====================================================================
        */
        public Unit unit_backup()
        {
            return (Unit)this.MemberwiseClone();
        }

        public static void unit_backup(Unit unit)
        {
            unit.backup = (Unit)unit.MemberwiseClone();
        }

        public static void unit_restore(ref Unit unit)
        {
            if (unit.backup != null && !string.IsNullOrEmpty(unit.backup.prop.id))
            {
                unit = (Unit)unit.backup.MemberwiseClone();
                unit.backup = null;
            }
            else
                Console.WriteLine("{0}: can't restore backup: not set", unit.name);
        }

        /*
        ====================================================================
        Check if target may do rugged defense
        ====================================================================
        */
#if TODO_RR
        public bool CheckRuggedDefense(Unit target)
        {
            if (((this.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) ||
                ((target.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING))
                return false;
            if (((this.sel_prop.flags & UnitFlags.SWIMMING) == UnitFlags.SWIMMING) ||
                ((target.sel_prop.flags & UnitFlags.SWIMMING) == UnitFlags.SWIMMING))
                return false;
            if ((this.sel_prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY) return false; /* no rugged def against range attack */
            if ((this.sel_prop.flags & UnitFlags.IGNORE_ENTR) == UnitFlags.IGNORE_ENTR) return false; /* no rugged def for pioneers and such */
            if (!this.IsClose(target)) return false;
            if (target.entr == 0) return false;
            return true;
        }
#endif
        /*
        ====================================================================
        Compute the rugged defense chance.
        ====================================================================
        */
        public int GetRuggedDefenseChance(Unit target)
        {
            /* PG's formula is
               5% * def_entr * 
               ( (def_exp_level + 2) / (atk_exp_level + 2) ) *
               ( (def_entr_rate + 1) / (atk_entr_rate + 1) ) */
            return (int)(5.0 * target.entr *
                   ((float)(target.exp_level + 2) / (this.exp_level + 2)) *
                   ((float)(target.sel_prop.entr_rate + 1) / (this.sel_prop.entr_rate + 1)));
        }

        /*
        ====================================================================
        Calculate the used fuel quantity. 'cost' is the base fuel cost to be
        deducted by terrain movement. The cost will be adjusted as needed.
        ====================================================================
        */
#if TODO_RR
        public int CalcFuelUsage(int cost)
        {
            int used = cost;

            /* air units use up *at least* the half of their initial movement points.
             */
            if ((this.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
            {
                int half = this.sel_prop.mov / 2;
                if (used < half) used = half;
            }

            /* ground units face a penalty during bad weather */
            if (!((this.sel_prop.flags & UnitFlags.SWIMMING) == UnitFlags.SWIMMING)
                 && !((this.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
                 && (Engine.terrain.weatherTypes[Scenario.scen_get_weather()].flags & WEATHER_FLAGS.DOUBLE_FUEL_COST) == WEATHER_FLAGS.DOUBLE_FUEL_COST)
                used *= 2;
            return used;
        }
#endif
        /*
        ====================================================================
        Update unit bar.
        ====================================================================
        */
        public void UpdateBar(Unit unit)
        {
            update_bar(unit);
        }

        /*
        ====================================================================
        Disable all actions.
        ====================================================================
        */
        public void SetAsUsed()
        {
            this.unused = false;
            this.cur_mov = 0;
            this.cur_atk_count = 0;
        }

        /*
        ====================================================================
        Duplicate the unit.
        ====================================================================
        */
#if TODO_RR
        public Unit Duplicate()
        {
            Unit newUnit = new Unit();
            newUnit = (Unit)this.MemberwiseClone();
            newUnit.SetGenericName(Scenario.units.Count + 1, this.prop.name);
            if (this.sel_prop == this.prop)
                newUnit.sel_prop = newUnit.prop;
            else
                newUnit.sel_prop = newUnit.trsp_prop;
            newUnit.backup = new Unit();
            /* terrain can't be updated here */
            return newUnit;
        }
#endif
        /*
        ====================================================================
        Check if unit has low ammo or fuel.
        ====================================================================
        */
        public bool CheckLowFuel()
        {
            if (!this.CheckFuelUsage())
                return false;
            if ((this.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
            {
                if (this.cur_fuel <= 20)
                    return true;
                return false;
            }
            if (this.cur_fuel <= 10)
                return true;
            return false;
        }

        public bool CheckLowAmmo()
        {
            /* a unit is low on ammo if it has less than twenty percent of its
             * class' ammo supply left, or less than two quantities,
             * whatever value is lower
             */
            int percentage = this.sel_prop.ammo / 5;
            return this.embark == UnitEmbarkTypes.EMBARK_NONE && this.cur_ammo <= Math.Min(percentage, 2);
        }

        /*
        ====================================================================
        Check whether unit can be considered for deployment.
        ====================================================================
        */
        public bool SupportsDeploy()
        {
            return ((this.prop.flags & UnitFlags.SWIMMING) != UnitFlags.SWIMMING)/* ships and */
                   && this.prop.mov > 0; /* fortresses cannot be deployed */
        }

        /// <summary>
        /// Update unit's bar info according to strength.
        /// </summary>
        /// <param name="unit"></param>
        static void update_bar(Unit unit)
        {
            /* bar width */
            unit.damage_bar_width = unit.str * BAR_TILE_WIDTH;
            if (unit.damage_bar_width == 0 && unit.str > 0)
                unit.damage_bar_width = BAR_TILE_WIDTH;
            /* bar color is defined by vertical offset in map.life_icons */
            if (unit.str > 4)
                unit.damage_bar_offset = 0;
            else
                if (unit.str > 2)
                    unit.damage_bar_offset = BAR_TILE_HEIGHT;
                else
                    unit.damage_bar_offset = BAR_TILE_HEIGHT * 2;
        }

        /// <summary>
        /// Get the current unit strength which is:
        ///   max { 0, unit.str - unit.suppr - unit.turn_suppr }
        /// </summary>
        /// <param name="unit"></param>
        /// <returns></returns>
        static int unit_get_cur_str(Unit unit)
        {
            int cur_str = unit.str - unit.suppr - unit.turn_suppr;
            if (cur_str < 0) cur_str = 0;
            return cur_str;
        }

        /// <summary>
        /// Apply suppression and damage to unit. Return the remaining 
        /// actual strength.
        /// If attacker is a bomber the suppression is counted as turn
        /// suppression.
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="damage"></param>
        /// <param name="suppr"></param>
        /// <param name="attacker"></param>
        /// <returns></returns>
        public int ApplyDamage(int damage, int suppr, Unit attacker)
        {
            this.str -= damage;
            if (this.str < 0)
            {
                this.str = 0;
                return 0;
            }
            if (attacker != null && ((attacker.sel_prop.flags & UnitFlags.TURN_SUPPR) == UnitFlags.TURN_SUPPR))
            {
                this.turn_suppr += suppr;
                if (this.str - this.turn_suppr - this.suppr < 0)
                {
                    this.turn_suppr = this.str - this.suppr;
                    return 0;
                }
            }
            else
            {
                this.suppr += suppr;
                if (this.str - this.turn_suppr - this.suppr < 0)
                {
                    this.suppr = this.str - this.turn_suppr;
                    return 0;
                }
            }
            return unit_get_cur_str(this);
        }

        /*
        ====================================================================
        Execute a single fight (no defensive fire check) with random
        values. (only if 'luck' is set)
        If 'force_rugged is set'. Rugged defense will be forced.
        ====================================================================
        */
        private enum ATTACK_FLAGS
        {
            ATK_BOTH_STRIKE = 0,
            ATK_UNIT_FIRST,
            ATK_TARGET_FIRST,
            ATK_NO_STRIKE
        };
#if TODO_RR
        public FIGHT_TYPES Attack(Unit target, UNIT_ATTACK type, bool real, bool force_rugged)
        {
            int unit_old_str = this.str;//, target_old_str = target.str;
            int unit_old_ini = this.sel_prop.ini, target_old_ini = target.sel_prop.ini;
            int unit_dam = 0, unit_suppr = 0, target_dam = 0, target_suppr = 0;
            int rugged_chance;
            bool rugged_def = false;
            int exp_mod;
            FIGHT_TYPES ret = FIGHT_TYPES.AR_NONE; /* clear flags */
            ATTACK_FLAGS strike;
            /* check if rugged defense occurs */
            if (real && type == UNIT_ATTACK.UNIT_ACTIVE_ATTACK)
                if (this.CheckRuggedDefense(target) || (force_rugged && this.IsClose(target)))
                {
                    rugged_chance = this.GetRuggedDefenseChance(target);
                    if (Misc.DICE(100) <= rugged_chance || force_rugged)
                        rugged_def = true;
                }
            /* PG's formula for initiative is
               min { base initiative, terrain max initiative } +
               ( exp_level + 1 ) / 2 + D3 */
            /* against aircrafts the initiative is used since terrain does not matter */
            /* target's terrain is used for fight */
            if ((this.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING &&
                (target.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING)
            {
                this.sel_prop.ini = Math.Min(this.sel_prop.ini, target.terrain.max_ini);
                target.sel_prop.ini = Math.Min(target.sel_prop.ini, target.terrain.max_ini);
            }
            this.sel_prop.ini += (this.exp_level + 1) / 2;
            target.sel_prop.ini += (target.exp_level + 1) / 2;
            /* special initiative rules:
               antitank inits attack tank|recon: atk 0, def 99
               tank inits attack against anti-tank: atk 0, def 99
               defensive fire: atk 99, def 0
               submarine attacks: atk 99, def 0
               ranged attack: atk 99, def 0
               rugged defense: atk 0
               air unit attacks air defense: atk = def 
               non-art vs art: atk 0, def 99 */
            if ((this.sel_prop.flags & UnitFlags.ANTI_TANK) == UnitFlags.ANTI_TANK)
                if ((target.sel_prop.flags & UnitFlags.TANK) == UnitFlags.TANK)
                {
                    this.sel_prop.ini = 0;
                    target.sel_prop.ini = 99;
                }
            if (((this.sel_prop.flags & UnitFlags.DIVING) == UnitFlags.DIVING) ||
                 ((this.sel_prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY) ||
                 ((this.sel_prop.flags & UnitFlags.AIR_DEFENSE) == UnitFlags.AIR_DEFENSE) ||
                 type == UNIT_ATTACK.UNIT_DEFENSIVE_ATTACK
            )
            {
                this.sel_prop.ini = 99;
                target.sel_prop.ini = 0;
            }
            if ((this.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
                if ((target.sel_prop.flags & UnitFlags.AIR_DEFENSE) == UnitFlags.AIR_DEFENSE)
                    this.sel_prop.ini = target.sel_prop.ini;
            if (rugged_def)
                this.sel_prop.ini = 0;
            if (force_rugged)
                target.sel_prop.ini = 99;
            /* the dice is rolled after these changes */
            if (real)
            {
                this.sel_prop.ini += Misc.DICE(3);
                target.sel_prop.ini += Misc.DICE(3);
            }
#if DEBUG
            if (real)
            {
                Console.WriteLine("{0} Initiative: {1}", this.name, this.sel_prop.ini);
                Console.WriteLine("{0} Initiative: {1}", target.name, target.sel_prop.ini);
                if (this.CheckRuggedDefense(target))
                    Console.WriteLine("Rugged Defense: {0} ({1})",
                            rugged_def ? "yes" : "no",
                            this.GetRuggedDefenseChance(target));
            }
#endif
            /* in a real combat a submarine may evade */
            if (real && type == UNIT_ATTACK.UNIT_ACTIVE_ATTACK && ((target.sel_prop.flags & UnitFlags.DIVING) == UnitFlags.DIVING))
            {
                if (Misc.DICE(10) <= 7 + (target.exp_level - this.exp_level) / 2)
                {
                    strike = ATTACK_FLAGS.ATK_NO_STRIKE;
                    ret |= FIGHT_TYPES.AR_EVADED;
                }
                else
                    strike = ATTACK_FLAGS.ATK_UNIT_FIRST;
#if DEBUG
                Console.WriteLine("\nSubmarine Evasion: {0} ({1}%)\n",
                 (strike == ATTACK_FLAGS.ATK_NO_STRIKE) ? "yes" : "no",
                 10 * (7 + (target.exp_level - this.exp_level) / 2));
#endif
            }
            else
                /* who is first? */
                if (this.sel_prop.ini == target.sel_prop.ini)
                    strike = ATTACK_FLAGS.ATK_BOTH_STRIKE;
                else
                    if (this.sel_prop.ini > target.sel_prop.ini)
                        strike = ATTACK_FLAGS.ATK_UNIT_FIRST;
                    else
                        strike = ATTACK_FLAGS.ATK_TARGET_FIRST;
            /* the one with the highest initiative begins first if not defensive fire or artillery */
            if (strike == ATTACK_FLAGS.ATK_BOTH_STRIKE)
            {
                /* both strike at the same time */
                GetDamage(this, this, target, type, real, rugged_def, out target_dam, out target_suppr);
                if (target.CheckAttack(this, UNIT_ATTACK.UNIT_PASSIVE_ATTACK))
                    GetDamage(this, target, this, UNIT_ATTACK.UNIT_PASSIVE_ATTACK, real, rugged_def, out unit_dam, out unit_suppr);
                target.ApplyDamage(target_dam, target_suppr, this);
                this.ApplyDamage(unit_dam, unit_suppr, target);
            }
            else
                if (strike == ATTACK_FLAGS.ATK_UNIT_FIRST)
                {
                    /* unit strikes first */
                    GetDamage(this, this, target, type, real, rugged_def, out target_dam, out target_suppr);
                    if (target.ApplyDamage(target_dam, target_suppr, this) != 0)
                        if (target.CheckAttack(this, UNIT_ATTACK.UNIT_PASSIVE_ATTACK) && type != UNIT_ATTACK.UNIT_DEFENSIVE_ATTACK)
                        {
                            GetDamage(this, target, this, UNIT_ATTACK.UNIT_PASSIVE_ATTACK, real, rugged_def, out unit_dam, out unit_suppr);
                            this.ApplyDamage(unit_dam, unit_suppr, target);
                        }
                }
                else
                    if (strike == ATTACK_FLAGS.ATK_TARGET_FIRST)
                    {
                        /* target strikes first */
                        if (target.CheckAttack(this, UNIT_ATTACK.UNIT_PASSIVE_ATTACK))
                        {
                            GetDamage(this, target, this, UNIT_ATTACK.UNIT_PASSIVE_ATTACK, real, rugged_def, out unit_dam, out unit_suppr);
                            if (this.ApplyDamage(unit_dam, unit_suppr, target) == 0)
                                ret |= FIGHT_TYPES.AR_UNIT_ATTACK_BROKEN_UP;
                        }
                        if (unit_get_cur_str(this) > 0)
                        {
                            GetDamage(this, this, target, type, real, rugged_def, out target_dam, out target_suppr);
                            target.ApplyDamage(target_dam, target_suppr, this);
                        }
                    }
            /* check return value */
            if (this.str == 0)
                ret |= FIGHT_TYPES.AR_UNIT_KILLED;
            else
                if (unit_get_cur_str(this) == 0)
                    ret |= FIGHT_TYPES.AR_UNIT_SUPPRESSED;
            if (target.str == 0)
                ret |= FIGHT_TYPES.AR_TARGET_KILLED;
            else
                if (unit_get_cur_str(target) == 0)
                    ret |= FIGHT_TYPES.AR_TARGET_SUPPRESSED;
            if (rugged_def)
                ret |= FIGHT_TYPES.AR_RUGGED_DEFENSE;
            if (real)
            {
                /* cost ammo */
                if (Config.supply)
                {
                    //if (DICE(10)<=target_old_str)
                    this.cur_ammo--;
                    if (target.CheckAttack(this, UNIT_ATTACK.UNIT_PASSIVE_ATTACK) && target.cur_ammo > 0)
                        //if (DICE(10)<=unit_old_str)
                        target.cur_ammo--;
                }
                /* costs attack */
                if (this.cur_atk_count > 0) this.cur_atk_count--;
                /* target: loose entrenchment if damage was taken or with a unit.str*10% chance */
                if (target.entr > 0)
                    if (target_dam > 0 || Misc.DICE(10) <= unit_old_str)
                        target.entr--;
                /* attacker looses entrenchment if it got hurt */
                if (this.entr > 0 && unit_dam > 0)
                    this.entr--;
                /* gain experience */
                exp_mod = target.exp_level - this.exp_level;
                if (exp_mod < 1) exp_mod = 1;
                this.AddExperience(exp_mod * target_dam + unit_dam);
                exp_mod = this.exp_level - target.exp_level;
                if (exp_mod < 1) exp_mod = 1;
                target.AddExperience(exp_mod * unit_dam + target_dam);
                if (this.IsClose(target))
                {
                    this.AddExperience(10);
                    target.AddExperience(10);
                }
                /* adjust life bars */
                update_bar(this);
                update_bar(target);
            }
            this.sel_prop.ini = unit_old_ini;
            target.sel_prop.ini = target_old_ini;
            return ret;
        }
#endif
#if TODO_RR
        public bool CheckMove(int x, int y, int stage)
        {
            return (Misc.get_dist(this.x, this.y, x, y) <= this.cur_mov);
        }
#endif
    }
}
