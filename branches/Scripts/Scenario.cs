using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using DataFile;
using UnityEngine;
using Miscellaneous;

namespace EngineA
{
    /*
    ====================================================================
    Engine setup. 'name' is the name of the Scenario or savegame 
    file. 'type' specifies what to do with the remaining data in this
    struct:
      INIT_CAMP: load whole campaign and use default values of the
                 scenarios
      INIT_SCEN: use setup info to overwrite Scenario's defaults
      DEFAULT:   use default values and set 'setup'
      LOAD:      load game and set 'setup'
      CAMP_BRIEFING: show campaign briefing dialog
      RUN_TITLE: show title screen and run the menu
    'ctrl' is the player control (PLAYER_CTRL_HUMAN, PLAYER_CTRL_CPU)  
    ====================================================================
    */
    public enum SETUP
    {
        SETUP_UNKNOWN = 0,
        SETUP_INIT_CAMP,
        SETUP_INIT_SCEN,
        SETUP_LOAD_GAME,
        SETUP_DEFAULT_SCEN,
        SETUP_CAMP_BRIEFING,
        SETUP_RUN_TITLE
    };


    public class Setup
    {
        public string fname;    /* resource file for loading type */
        public SETUP type;
        public int slot_id; /* in case of LOAD_GAME this is the slot id */
        /* campaign specific information, must be set for SETUP_CAMP_INIT */
        public string scen_state; /* Scenario state to begin campaign with */
        /* Scenario specific information which is loaded by scen_load_info() */
        public int player_count;
        public PLAYERCONTROL[] ctrl;
        public string[] modules;
        public string[] names;
    }


    /*
    ====================================================================
    Scenario Info
    ====================================================================
    */
	[Serializable]
    public class Scen_Info
    {
		[XmlIgnore]
        public string fname;    /* Scenario knows it's own file_name in the Scenario path */
        public string name;     /* Scenario's name */
        public string desc;     /* description */
        public string authors;  /* a string with all author names */
        public System.DateTime start_date;/* starting date of Scenario */
        public int turn_limit;     /* Scenario is finished after this number of turns at the latest */
        public int days_per_turn;
        public int turns_per_day;  /* the date string of a turn is computed from these to values 
                           and the inital date */
        public int player_count;   /* number of players */
    } ;

    /*
    ====================================================================
    Victory conditions
    ====================================================================
    */
    public enum VCOND_CHECK
    {
        VCOND_CHECK_EVERY_TURN = 0,
        VCOND_CHECK_LAST_TURN
    };

    public enum VSUBCOND
    {
        VSUBCOND_CTRL_ALL_HEXES = 0,
        VSUBCOND_CTRL_HEX,
        VSUBCOND_TURNS_LEFT,
        VSUBCOND_CTRL_HEX_NUM,
        VSUBCOND_UNITS_KILLED,
        VSUBCOND_UNITS_SAVED
    };

	[Serializable]
    public class VSubCond
    {
        public VSUBCOND type;           /* type as above */
        public Player player;     /* player this condition is checked for */
        public int x, y;            /* special */
        public int count;
        public string tag;       /* tag of unit group */
    }

    public struct VCond
    {
        public VSubCond[] subconds_or;    /* sub conditions linked with logical or */
        public VSubCond[] subconds_and;   /* sub conditions linked with logical and */
        public int sub_or_count;
        public int sub_and_count;
        public string result;
        public string message;
    }

    public class Scenario
    {
        /*
        ====================================================================
        Scenario data.
        ====================================================================
        */
        public Setup setup;            /* engine setup with Scenario information */
        public static Scen_Info scen_info;
        public static int[] weather;       /* weather type for each turn as id to weatherTypes */
        public static int cur_weather;        /* weather of current turn */
        public static int turn;               /* current turn id */
        public static List<Unit> units;        /* active units */
        public static List<Unit> vis_units;    /* all units spotted by current player */
        public static List<Unit> avail_units;  /* available units for current player to deploy */
        public static List<Unit> reinf;        /* reinforcements -- units with a delay (if delay is 0
                                   a unit is made available in avail_units if the player
                                   controls it. */
        public static Player player;     /* current player */
        int log_x, log_y;       /* position where to draw log info */
        string scen_domain;	/* domain this Scenario was loaded under */
        /* VICTORY CONDITIONS */
        public static string scen_result = "";  /* the Scenario result is saved here */
        public static string scen_message = ""; /* the final Scenario message is saved here */
        public static VCOND_CHECK vcond_check_type = 0;   /* test victory conditions this turn */
        public static VCond[] vconds;          /* victory conditions */
        public static int vcond_count = 0;
        static int[,] casualties;	/* sum of casualties grouped by unit class and player */
        public static string map_fname;
        public static List<string> unitsToAdd;
        public static List<Flag> list_flags;
		public static Map mapScen;

        /*
        ====================================================================
        Load a Scenario. 
        ====================================================================
        */
		public static bool scen_load(string fname){
			if (string.IsNullOrEmpty(fname)) return false;
			try
            {
				
				string path = "Assets/Scenarios/"+fname;
				XmlSerializer SerializerObj = new XmlSerializer(typeof(Scenario_Data_File));
				// Create a new file stream for reading the XML file
        		FileStream ReadFileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        		// Load the object saved above by using the Deserialize function
        		Scenario_Data_File scen_data = (Scenario_Data_File)SerializerObj.Deserialize(ReadFileStream);
        		// Cleanup
        		ReadFileStream.Close();
				scen_info = scen_data.scen_info;
				scen_info.fname = path;
				/* nations */
				string nations = scen_data.nation_db_name;
				if (Nation.nations_load(nations)==-1)
					return false;
				string unitlib = scen_data.unit_db_name;
				Unit_Lib_Entry unit_lib = DB.UnitLib;
				if (unit_lib.UnitLibLoad(unitlib)==-1)
					return false;
				if (scen_data.unitsToAdd.Count>0){
					unitsToAdd = scen_data.unitsToAdd;
				}
				/* map and weather */
				map_fname = scen_data.map_fname;
				if (map_fname==null){
					map_fname = scen_data.scen_info.fname;
					mapScen = scen_data.map;
				}
				else{
					Map map = Engine.map;
					if (map.map_load(map_fname)==-1)
						return false;
				}
				weather = scen_data.weather;
				if (weather==null){
					throw new Exception("weather not found");
				}
				/* players */
				Player.players = scen_data.list_players;
				if (Player.players==null){
					throw new Exception("players not found");
				}
				/* set alliances */
				player = scen_data.player;
				if (player==null){
					throw new Exception("player controller not found");
				}
				/* flags */
				list_flags = scen_data.list_flags;
				if (list_flags==null){
					throw new Exception("flags not found");
				}
				foreach (Flag flag in list_flags){
					Nation nation = Nation.nation_find(flag.name);
					Engine.map.map[flag.x, flag.y].nation = nation;
                    Engine.map.map[flag.x, flag.y].player = Player.player_get_by_nation(nation);
					if (Engine.map.map[flag.x, flag.y].nation != null)
                        Engine.map.map[flag.x, flag.y].deploy_center = 1;
                    Engine.map.map[flag.x, flag.y].obj = (flag.obj != 0);
				}
				/* victory conditions */
				/* check type */
				vcond_check_type = scen_data.vcond_check_type;
				if (vcond_check_type==null)
					vcond_check_type = VCOND_CHECK.VCOND_CHECK_EVERY_TURN;
				/* count conditions */
				vcond_count = scen_data.vcond_count;
				if (vcond_count==0){
					throw new Exception("victory conditions count is malformed");
				}
				/* create conditions */
				vconds = scen_data.vconds;
				if (vconds==null){
					throw new Exception("victory conditions not found");
				}
				for (int i=1;i<vcond_count;i++){
					/* no sub conditions at all? */
                    if (vconds[i].sub_and_count == 0 && vconds[i].sub_or_count == 0)
                    {
                       	throw new Exception("No subconditions specified!");
                    }    
				}
				/* units */
				if (scen_data.units.Count==0 || scen_data.units == null){
					throw new Exception("units not found");
				}
				units = new List<Unit>();
				reinf = new List<Unit>();
                avail_units = new List<Unit>();
                vis_units = new List<Unit>();
				
				bool unit_delayed = false;
				Unit unit;
				int unit_ref = 0;
				foreach (Unit unit_base in scen_data.units){
					/* unit type */
					Unit_Lib_Entry unit_prop = unit_lib.unit_lib_find_by_name(unit_base.name);
					if (unit_prop == null)
                    {
                        throw new Exception(unit_base.name + ": unit entry not found");
                    }
					/* nation & player */
					if (unit_base.nation == null)
                    {
                        throw new Exception("nation: not a nation");
                    }
					else if (Nation.nation_find(unit_base.nation.ID)==null){
						throw new Exception(unit_base.nation.ID + ": not a nation");
					}
					unit_base.player = Player.player_get_by_nation(unit_base.nation);
					if (unit_base.player == null)
                    {
                        throw new Exception(unit_base.nation.ID + ": no player controls this nation");
                    }
					/* name */
                    unit_base.SetGenericName(unit_ref + 1, unit_prop.name);
					/* delay */
					unit_delayed = (unit_base.delay != 0);
					/* position */
					if (string.IsNullOrEmpty(unit_base.x.ToString()) && !unit_delayed)
                        throw new Exception("Unit has wrong x");
					if (string.IsNullOrEmpty(unit_base.y.ToString()) && !unit_delayed)
                        throw new Exception("Unit has wrong y");
					if (!unit_delayed && (unit_base.x <= 0 || unit_base.y <= 0 ||
                        unit_base.x >= Engine.map.map_w - 1 || unit_base.y >= Engine.map.map_h - 1))
                    {
                        throw new Exception(unit_base.name + ": out of map: ignored");
                    }
					/* orientation */
                    unit_base.orient = unit_base.player.orient;
					/* tag if set */
					if (!string.IsNullOrEmpty(unit_base.tag))
                    {
                        /* check all subconds for UNITS_SAVED and increase the counter
                           if this unit is allied */
                        for (int i = 1; i < vcond_count; i++)
                        {
                            for (int j = 0; j < vconds[i].sub_and_count; j++)
                                if (vconds[i].subconds_and[j].type == VSUBCOND.VSUBCOND_UNITS_SAVED)
                                    if (unit_base.tag == vconds[i].subconds_and[j].tag)
                                        vconds[i].subconds_and[j].count++;
                            for (int j = 0; j < vconds[i].sub_or_count; j++)
                                if (vconds[i].subconds_or[j].type == VSUBCOND.VSUBCOND_UNITS_SAVED)
                                    if (unit_base.tag == vconds[i].subconds_or[j].tag)
                                        vconds[i].subconds_or[j].count++;
                        }
                    }
					/* actual unit */
                    unit = Unit.CreateUnit(unit_prop, unit_base.trsp_prop, unit_base);
					/* put unit to active or reinforcements list */
                    if (!unit_delayed)
                    {
                        /* add unit to map */
						units.Add(unit);
                        Engine.map.map_insert_unit(unit);
                    }
					else
                        reinf.Add(unit);
					/* adjust transporter count */
                    if (unit.embark == UnitEmbarkTypes.EMBARK_SEA)
                    {
                        unit.player.sea_trsp_count++;
                        unit.player.sea_trsp_used++;
                    }
                    else
                        if (unit.embark == UnitEmbarkTypes.EMBARK_AIR)
                        {
                            unit.player.air_trsp_count++;
                            unit.player.air_trsp_used++;
                        }
					unit_ref++;
					
				}
				casualties = new int[DB.UnitLib.unit_class_count, scen_info.player_count];
                Engine.deploy_turn = Config.deploy;
				return true;
			}
			catch (Exception e)
            {
                Debug.LogError("exception: " + e);
                return false;
            }
			
		}


        /*
        ====================================================================
        Load a Scenario description (newly allocated string)
        and setup the setup :) except the type which is set when the 
        engine performs the load action.
        ====================================================================
        */
        public static string scen_load_info(string fname)
        {
            throw new NotImplementedException();
        }


        /*
        ====================================================================
        Fill the Scenario part in 'setup' with the loaded player 
        information.
        ====================================================================
        */

        public static void scen_set_setup()
        {
            scen_clear_setup();
            Engine.setup.player_count = Player.players.Count;
            Engine.setup.ctrl = new PLAYERCONTROL[Engine.setup.player_count];
            Engine.setup.names = new string[Engine.setup.player_count];
            Engine.setup.modules = new string[Engine.setup.player_count];
            for (int i = 0; i < Engine.setup.player_count; i++)
            {
                Player player = Player.players[i];
                Engine.setup.ctrl[i] = player.ctrl;
                Engine.setup.names[i] = player.name;
                Engine.setup.modules[i] = player.ai_fname;
            }
        }

        /*
        ====================================================================
        Clear the Scenario stuff pointers in 'setup' 
        (loaded by scen_load_info())
        ====================================================================
        */

        public static void scen_clear_setup()
        {
            if (Engine.setup.ctrl != null)
            {
                Engine.setup.ctrl = null;
            }
            if (Engine.setup.names != null)
            {
                Engine.setup.names = null;
            }
            if (Engine.setup.modules != null)
            {
                Engine.setup.modules = null;
            }
            Engine.setup.player_count = 0;
        }

        /*
        ====================================================================
        Delete Scenario
        ====================================================================
        */
        public void scen_delete()
        {
            throw new NotImplementedException();
        }


        /*
        ====================================================================
        Check if unit is destroyed, set the current attack and movement.
        If SCEN_PREP_UNIT_FIRST is passed the entrenchment is not modified.
        ====================================================================
        */
        public enum SCEN_PREP
        {
            SCEN_PREP_UNIT_FIRST = 0,
            SCEN_PREP_UNIT_NORMAL
        };

        public static void scen_prep_unit(Unit unit, SCEN_PREP type)
        {
            int min_entr, max_entr;
            /* unit may not be undeployed */
            unit.fresh_deploy = false;
            /* remove turn suppression */
            unit.turn_suppr = 0;
            /* allow actions */
            unit.unused = true;
            unit.cur_mov = unit.sel_prop.mov;
            if (unit.CheckGroundTransporter())
                unit.cur_mov = unit.trsp_prop.mov;
            /* if we have bad weather the relation between mov : fuel is 1 : 2 
             * for ground units
             */
            if (((unit.sel_prop.flags & UnitFlags.SWIMMING) != UnitFlags.SWIMMING) &&
                ((unit.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING)
                 && ((Engine.terrain.weatherTypes[Scenario.scen_get_weather()].flags & WEATHER_FLAGS.DOUBLE_FUEL_COST) == WEATHER_FLAGS.DOUBLE_FUEL_COST))
            {
                if (unit.CheckFuelUsage() && unit.cur_fuel < unit.cur_mov * 2)
                    unit.cur_mov = unit.cur_fuel / 2;
            }
            else
                if (unit.CheckFuelUsage() && unit.cur_fuel < unit.cur_mov)
                    unit.cur_mov = unit.cur_fuel;
            unit.Unmount();
            unit.cur_atk_count = unit.sel_prop.atk_count;
            /* if unit is preparded normally check entrenchment */
            if (type == SCEN_PREP.SCEN_PREP_UNIT_NORMAL && ((unit.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING))
            {
                min_entr = Engine.map.map_tile(unit.x, unit.y).terrain.min_entr;
                max_entr = Engine.map.map_tile(unit.x, unit.y).terrain.max_entr;
                if (unit.entr < min_entr)
                    unit.entr = min_entr;
                else
                    if (unit.entr < max_entr)
                        unit.entr++;
            }
        }

        /*
        ====================================================================
        Check if subcondition is fullfilled.
        ====================================================================
        */
        static bool subcond_check(VSubCond cond)
        {
            int x, y, count;
            if (cond == null) return false;
            switch (cond.type)
            {
                case VSUBCOND.VSUBCOND_CTRL_ALL_HEXES:
                    for (x = 0; x < Engine.map.map_w; x++)
                        for (y = 0; y < Engine.map.map_h; y++)
                            if (Engine.map.map[x, y].player != null && Engine.map.map[x, y].obj)
                                if (!Player.player_is_ally(cond.player, Engine.map.map[x, y].player))
                                    return false;
                    return true;
                case VSUBCOND.VSUBCOND_CTRL_HEX:
                    if (Engine.map.map[cond.x, cond.y].player != null)
                        if (Player.player_is_ally(cond.player, Engine.map.map[cond.x, cond.y].player))
                            return true;
                    return false;
                case VSUBCOND.VSUBCOND_TURNS_LEFT:
                    if (scen_info.turn_limit - turn > cond.count)
                        return true;
                    else
                        return false;
                case VSUBCOND.VSUBCOND_CTRL_HEX_NUM:
                    count = 0;
                    for (x = 0; x < Engine.map.map_w; x++)
                        for (y = 0; y < Engine.map.map_h; y++)
                            if (Engine.map.map[x, y].player != null && Engine.map.map[x, y].obj)
                                if (Player.player_is_ally(cond.player, Engine.map.map[x, y].player))
                                    count++;
                    if (count >= cond.count)
                        return true;
                    return false;
                case VSUBCOND.VSUBCOND_UNITS_KILLED:
                    /* as long as there are units out using this tag this condition
                       is not fullfilled */
                    foreach (Unit unit in units)
                        if (unit.killed == 0 && !Player.player_is_ally(cond.player, unit.player))
                            if (unit.tag[0] != 0 && (unit.tag == cond.tag))
                                return false;
                    return true;
                case VSUBCOND.VSUBCOND_UNITS_SAVED:
                    /* we counted the number of units with this tag so count again
                       and if one is missing: bingo */
                    count = 0;
                    foreach (Unit unit in units)
                        if (Player.player_is_ally(cond.player, unit.player))
                            if (unit.tag[0] != 0 && (unit.tag == cond.tag))
                                count++;
                    if (count == cond.count)
                        return true;
                    return false;
            }
            return false;
        }

        /*
        ====================================================================
        Check if the victory conditions are fullfilled and if so
        return True. 'result' is used then
        to determine the next Scenario in the campaign.
        If 'after_last_turn' is set this is the check called by end_turn().
        If no condition is fullfilled the else condition is used (very 
        first one).
        ====================================================================
        */
        public static bool scen_check_result(bool after_last_turn)
        {
            bool and_okay, or_okay;
#if DEBUG_CAMPAIGN
    char fname[512];
    FILE *f;
    snprintf(fname, sizeof fname, "%s/.lgames/.scenresult", getenv("HOME"));
    f = fopen(fname, "r");
    if (f) {
        unsigned len;
        scen_result[0] = '\0';
        fgets(scen_result, sizeof scen_result, f);
        fclose(f);
        len = strlen(scen_result);
        if (len > 0 && scen_result[len-1] == '\n') scen_result[len-1] = '\0';
        strcpy(scen_message, scen_result);
        if (len > 0) return 1;
    }
#endif
            if (vcond_check_type == VCOND_CHECK.VCOND_CHECK_EVERY_TURN || after_last_turn)
            {
                for (int i = 1; i < vcond_count; i++)
                {
                    /* AND binding */
                    and_okay = true;
                    for (int j = 0; j < vconds[i].sub_and_count; j++)
                        if (!subcond_check(vconds[i].subconds_and[j]))
                        {
                            and_okay = false;
                            break;
                        }
                    /* OR binding */
                    or_okay = false;
                    for (int j = 0; j < vconds[i].sub_or_count; j++)
                        if (subcond_check(vconds[i].subconds_or[j]))
                        {
                            or_okay = true;
                            break;
                        }
                    if (vconds[i].sub_or_count == 0)
                        or_okay = true;
                    if (or_okay && and_okay)
                    {
                        scen_result = vconds[i].result;
                        scen_message = vconds[i].message;
                        return true;
                    }
                }
            }
            if (after_last_turn)
            {
                scen_result = vconds[0].result;
                scen_message = vconds[0].message;
                return true;
            }
            return false;
        }

        /*
        ====================================================================
        Return True if Scenario is done.
        ====================================================================
        */
        public static bool scen_done()
        {
            return !string.IsNullOrEmpty(scen_result);
        }

        /*
        ====================================================================
        Return result string.
        ====================================================================
        */
        public static string scen_get_result()
        {
            return scen_result;
        }

        /*
        ====================================================================
        Return result message
        ====================================================================
        */
        public static string scen_get_result_message()
        {
            return scen_message;
        }

        /*
        ====================================================================
        Clear result and message
        ====================================================================
        */
        public static void scen_clear_result()
        {
            scen_result = null;
            scen_message = null;
        }


        /*
        ====================================================================
        Check the supply level of a unit. (hex tiles with SUPPLY_GROUND
        have 100% supply.
        ====================================================================
        */
        public static void scen_adjust_unit_supply_level(Unit unit)
        {
            unit.supply_level = Engine.map.map_get_unit_supply_level(unit.x, unit.y, unit);
        }
        /*
        ====================================================================
        Get current weather/forecast
        ====================================================================
        */

        public static int scen_get_weather()
        {
            if (turn < scen_info.turn_limit && Config.weather)
                return weather[turn];
            else
                return 0;
        }

        public static int scen_get_forecast()
        {
            if (turn + 1 < scen_info.turn_limit)
                return weather[turn + 1];
            else
                return 0;
        }


        /*
        ====================================================================
        Get date string of current date.
        ====================================================================
        */
        public static string scen_get_date()
        {
            DateTime date = scen_info.start_date;
            if (scen_info.days_per_turn > 0)
            {
                date = date.AddDays(scen_info.days_per_turn * turn);
                return date.ToLongDateString();
            }
            else
            {
                date = date.AddDays(turn / scen_info.turns_per_day);

                int phase = turn % scen_info.turns_per_day;
                int hour = 8 + phase * 6;
                return date.ToLongDateString() + hour + ":00";
            }
        }

        /*
        ====================================================================
        Get/Add casualties for unit class and player.
        ====================================================================
        */
#if TODO_RR
        public static int scen_get_casualties(int player, int unitclass)
        {
            if (casualties == null || player < 0 || player >= scen_info.player_count
                || unitclass < 0 || unitclass >= DB.UnitLib.unit_class_count)
                return 0;
            return casualties[unitclass, player];
        }

        public static int scen_inc_casualties(int player, int unitclass)
        {
            if (casualties == null || player < 0 || player >= scen_info.player_count
                 || unitclass < 0 || unitclass >= DB.UnitLib.unit_class_count)
                return 0;
            return casualties[unitclass, player]++;
        }
#endif
        /*
        ====================================================================
        Add casualties for unit. Regard unit and transport classes.
        ====================================================================
        */
#if TODO_RR
        public static int scen_inc_casualties_for_unit(Unit unit)
        {
            int player = Player.player_get_index(unit.player);
            int cnt = scen_inc_casualties(player, unit.prop.unit_class);
            if (unit.trsp_prop != null)
                cnt = scen_inc_casualties(player, unit.trsp_prop.unit_class);
            return cnt;

        }
#endif
    }
}
