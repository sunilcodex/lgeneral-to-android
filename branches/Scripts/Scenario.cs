using System;
using System.Collections.Generic;
using System.Text;

namespace Engine
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
    public class Scen_Info
    {
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


        /*
        ====================================================================
        Load a Scenario. 
        ====================================================================
        */
#if TODO_RR
        public static bool scen_load(string fname)
        {
            if (string.IsNullOrEmpty(fname)) return false;
            string path = Util.MakeGamedirFile("scenarios", fname);
            scen_info = new Scen_Info();
            scen_info.fname = path;
            try
            {

                Script script = Script.CreateScript(path);
                //Console.WriteLine(script);

                Console.WriteLine("Loading Scenario Info");
                scen_info.name = script.GetProperty("name");
                scen_info.desc = script.GetProperty("desc");
                scen_info.authors = script.GetProperty("authors");
                string date = script.GetProperty("date");
                scen_info.start_date = System.DateTime.Parse(date);
                scen_info.turn_limit = int.Parse(script.GetProperty("turns"));
                scen_info.turns_per_day = int.Parse(script.GetProperty("turns_per_day"));
                scen_info.days_per_turn = int.Parse(script.GetProperty("days_per_turn"));
                string players = script.GetProperty("players");
                //scen_info.player_count = entries.count;
                /* nations */
                Console.WriteLine("Loading Nations");
                string nations = script.GetProperty("nation_db");
                Nation.nations_load(nations);
                List<Block> listblocks = script.GetBlock("unit_db");
                Unit_Lib_Entry unit_lib = DB.UnitLib;
                if (listblocks.Count == 0)
                {
                    unit_lib.UnitLibLoad(path, Unit_Lib_Entry.UNIT_LOAD.UNIT_LIB_MAIN);
                }
                else
                {
                    string unitlib = listblocks[0].GetProperty("main");
                    unit_lib.UnitLibLoad(unitlib, Unit_Lib_Entry.UNIT_LOAD.UNIT_LIB_MAIN);
                    string addUnits = listblocks[0].GetProperty("add");
                    if (addUnits != null)
                        foreach (string unitToadd in addUnits.Split('?'))
                        {
                            unit_lib.UnitLibLoad(unitToadd, Unit_Lib_Entry.UNIT_LOAD.UNIT_LIB_ADD);
                        }
                }

                /* map and weather */
                string mapPath = script.GetProperty("map");
                if (mapPath == null)
                {
                    mapPath = Util.MakeGamedirFile("scenarios", fname); /* check the Scenario file itself */
                }
                Console.WriteLine("Loading Map" + mapPath);
                int lindex = mapPath.LastIndexOf('/');
                map_fname = mapPath.Substring(lindex+1);
                Map map = Engine.map;
                map.map_load(mapPath);
                weather = new int[scen_info.turn_limit];
                string weatherStr = script.GetProperty("weather");
                if (weatherStr != null)
                {
                    int i = 0;
                    foreach (string str in weatherStr.Split('°'))
                    {
                        if (i == scen_info.turn_limit) break;
                        for (int j = 0; j < Engine.terrain.weatherTypeCount; j++)
                            if (str == Engine.terrain.weatherTypes[j].id)
                            {
                                weather[i] = j;
                                break;
                            }
                        i++;
                    }
                }
                /* players */
                Console.WriteLine("Loading Players");
                List<Block> playersEntries = script.GetBlock("players")[0].Blocks;
                foreach (Block sub in playersEntries)
                {
                    /* create player */
                    player = new Player();
                    player.id = sub.Name;
                    player.name = sub.GetProperty("name");
                    string[] nationsVal = sub.GetProperty("nations").Split('°');
                    player.nation_count = nationsVal.Length;
                    player.nations = new Nation[player.nation_count];
                    for (int i = 0; i < player.nation_count; i++)
                        player.nations[i] = Nation.nation_find(nationsVal[i]);
                    string strVal = sub.GetProperty("orientation");
                    if (strVal == "right") /* alldirs not implemented yet */
                        player.orient = UnitLookingDirection.UNIT_ORIENT_RIGHT;
                    else
                        player.orient = UnitLookingDirection.UNIT_ORIENT_LEFT;
                    strVal = sub.GetProperty("control");
                    if (strVal == "cpu")
                        player.ctrl = PLAYERCONTROL.PLAYER_CTRL_CPU;
                    else
                        player.ctrl = PLAYERCONTROL.PLAYER_CTRL_HUMAN;
                    player.ai_fname = sub.GetProperty("ai_module");
                    if (player.ai_fname == null)
                        player.ai_fname = "default";
                    player.strat = int.Parse(sub.GetProperty("strategy"));
                    Block subTransporters = sub.GetBlock("transporters")[0];
                    if (subTransporters != null)
                    {
                        List<Block> subsub = subTransporters.GetBlock("transporters");
                        if (subsub != null && subsub.Count == 1)
                        {
                            string str = subsub[0].GetProperty("unit");
                            player.air_trsp = unit_lib.unit_lib_find(str);
                            player.air_trsp_count = int.Parse(subsub[0].GetProperty("count"));
                        }
                    }
                    /* PURCHASE: 
                       set starting prestige to 200 as default if no entry found,
                       set prestige_per_turn to 0 as default */
                    player.prestige = 200;
                    string strPrestige = sub.GetProperty("start_prestige");
                    if (!string.IsNullOrEmpty(strPrestige))
                        player.prestige = int.Parse(strPrestige);
                    player.prestige_per_turn = 0;
                    strPrestige = sub.GetProperty("prestige_per_turn");
                    if (!string.IsNullOrEmpty(strPrestige))
                        player.prestige_per_turn = int.Parse(strPrestige);
                    Player.player_add(player);
                }
                /* set alliances */
                //List<Block> playersEntries = script.GetBlock("players")[0].Blocks;
                for (int i = 0; i < Player.players.Count; i++)
                {
                    player = Player.players[i];
                    player.allies = new List<Player>();
                    string allied = playersEntries[i].GetProperty("allied_players");
                    if (!string.IsNullOrEmpty(allied))
                    {
                        foreach (string id in allied.Split('°'))
                            player.allies.Add(Player.player_get_by_id(id));
                    }
                }
                /* flags */
                Console.WriteLine("Loading Flags");
                List<Block> flags = script.GetBlock("flags")[0].Blocks;
                foreach (Block flag in flags)
                {
                    int x = int.Parse(flag.GetProperty("x"));
                    int y = int.Parse(flag.GetProperty("y"));
                    int obj;
                    string objStr = flag.GetProperty("obj");
                    if (string.IsNullOrEmpty(objStr))
                        obj = 0;
                    else
                        obj = int.Parse(objStr);
                    string str = flag.GetProperty("nation");
                    Nation nation = Nation.nation_find(str);
                    Engine.map.map[x, y].nation = nation;
                    Engine.map.map[x, y].player = Player.player_get_by_nation(nation);
                    if (Engine.map.map[x, y].nation != null)
                        Engine.map.map[x, y].deploy_center = 1;
                    Engine.map.map[x, y].obj = (obj != 0);
                }
                /* victory conditions */
                //scen_result[0] = 0;
                Console.WriteLine("Loading Victory Conditions");
                /* check type */
                vcond_check_type = VCOND_CHECK.VCOND_CHECK_EVERY_TURN;
                List<Block> entries = script.GetBlock("result");
                string checkStr = null;
                if (entries != null && entries.Count > 0)
                    checkStr = entries[0].GetProperty("check");
                if (!string.IsNullOrEmpty(checkStr))
                {
                    if (checkStr == "last_turn")
                        vcond_check_type = VCOND_CHECK.VCOND_CHECK_LAST_TURN;
                }
                /* reset vic conds may not be done in scen_delete() as this is called
                   before the check */

                /* count conditions */
                int ind = 0;
                foreach (Block pd_vcond in entries[0].Blocks)
                    if (pd_vcond.Name == "cond")
                        ind++;
                vcond_count = ind + 1;
                /* create conditions */
                vconds = new VCond[vcond_count];
                ind = 1; /* the very first condition is the else condition */
                foreach (Block pd_vcond in entries[0].Blocks)
                {
                    if (pd_vcond.Name == "cond")
                    {
                        /* result & message */
                        string str = pd_vcond.GetProperty("result");
                        if (!string.IsNullOrEmpty(str))
                            vconds[ind].result = str;
                        else
                            vconds[ind].result = "undefined";
                        str = pd_vcond.GetProperty("message");
                        if (!string.IsNullOrEmpty(str))
                            vconds[ind].message = str;
                        else
                            vconds[ind].message = "undefined";
                        /* and linkage */
                        List<Block> pd_bind = pd_vcond.GetBlock("and");
                        if (pd_bind != null && pd_bind.Count > 0)
                        {
                            vconds[ind].sub_and_count = pd_bind[0].Blocks.Count;
                            /* create subconditions */
                            vconds[ind].subconds_and = new VSubCond[vconds[ind].sub_and_count];
                            for (int i = 0; i < vconds[ind].subconds_and.Length; i++)
                                vconds[ind].subconds_and[i] = new VSubCond();
                            int j = 0;
                            foreach (Block pd_vsubcond in pd_bind[0].Blocks)
                            {
                                /* get subconds */
                                if (pd_vsubcond.Name == "control_all_hexes")
                                {
                                    vconds[ind].subconds_and[j].type = VSUBCOND.VSUBCOND_CTRL_ALL_HEXES;
                                    str = pd_vsubcond.GetProperty("player");
                                    vconds[ind].subconds_and[j].player = Player.player_get_by_id(str);
                                }
                                else
                                    if (pd_vsubcond.Name == "control_hex")
                                    {
                                        vconds[ind].subconds_and[j].type = VSUBCOND.VSUBCOND_CTRL_HEX;
                                        str = pd_vsubcond.GetProperty("player");
                                        vconds[ind].subconds_and[j].player = Player.player_get_by_id(str);
                                        vconds[ind].subconds_and[j].x = int.Parse(pd_vsubcond.GetProperty("x"));
                                        vconds[ind].subconds_and[j].y = int.Parse(pd_vsubcond.GetProperty("y"));
                                    }
                                    else // 630723846
                                        if (pd_vsubcond.Name == "turns_left")
                                        {
                                            vconds[ind].subconds_and[j].type = VSUBCOND.VSUBCOND_TURNS_LEFT;
                                            vconds[ind].subconds_and[j].count = int.Parse(pd_vsubcond.GetProperty("count"));
                                        }
                                        else
                                            if (pd_vsubcond.Name == "control_hex_num")
                                            {
                                                vconds[ind].subconds_and[j].type = VSUBCOND.VSUBCOND_CTRL_HEX_NUM;
                                                str = pd_vsubcond.GetProperty("player");
                                                vconds[ind].subconds_and[j].player = Player.player_get_by_id(str);
                                                vconds[ind].subconds_and[j].count = int.Parse(pd_vsubcond.GetProperty("count")); ;
                                            }
                                            else
                                                if (pd_vsubcond.Name == "units_killed")
                                                {
                                                    vconds[ind].subconds_and[j].type = VSUBCOND.VSUBCOND_UNITS_KILLED;
                                                    str = pd_vsubcond.GetProperty("player");
                                                    vconds[ind].subconds_and[j].player = Player.player_get_by_id(str);
                                                    str = pd_vsubcond.GetProperty("tag");
                                                    vconds[ind].subconds_and[j].tag = str;
                                                }
                                                else
                                                    if (pd_vsubcond.Name == "units_saved")
                                                    {
                                                        vconds[ind].subconds_and[j].type = VSUBCOND.VSUBCOND_UNITS_SAVED;
                                                        str = pd_vsubcond.GetProperty("player");
                                                        vconds[ind].subconds_and[j].player = Player.player_get_by_id(str);
                                                        str = pd_vsubcond.GetProperty("tag");
                                                        vconds[ind].subconds_and[j].tag = str;
                                                        vconds[ind].subconds_and[j].count = 0; /* units will be counted */
                                                    }
                                j++;
                            }
                        }
                        /* or linkage */
                        pd_bind = pd_vcond.GetBlock("or");
                        if (pd_bind != null && pd_bind.Count > 0)
                        {
                            vconds[ind].sub_or_count = pd_bind.Count;
                            /* create subconditions */
                            vconds[ind].subconds_or = new VSubCond[vconds[ind].sub_or_count];
                            int j = 0;
                            foreach (Block pd_vsubcond in pd_bind)
                            {
                                /* get subconds */
                                if (pd_vsubcond.Name == "control_all_hexes")
                                {
                                    vconds[ind].subconds_or[j].type = VSUBCOND.VSUBCOND_CTRL_ALL_HEXES;
                                    str = pd_vsubcond.GetProperty("player");
                                    vconds[ind].subconds_or[j].player = Player.player_get_by_id(str);
                                }
                                else
                                    if (pd_vsubcond.Name == "control_hex")
                                    {
                                        vconds[ind].subconds_or[j].type = VSUBCOND.VSUBCOND_CTRL_HEX;
                                        str = pd_vsubcond.GetProperty("player");
                                        vconds[ind].subconds_or[j].player = Player.player_get_by_id(str);
                                        vconds[ind].subconds_or[j].x = int.Parse(pd_vsubcond.GetProperty("x"));
                                        vconds[ind].subconds_or[j].y = int.Parse(pd_vsubcond.GetProperty("y"));
                                    }
                                    else
                                        if (pd_vsubcond.Name == "turns_left")
                                        {
                                            vconds[ind].subconds_or[j].type = VSUBCOND.VSUBCOND_TURNS_LEFT;
                                            vconds[ind].subconds_or[j].count = int.Parse(pd_vsubcond.GetProperty("count"));
                                        }
                                        else
                                            if (pd_vsubcond.Name == "control_hex_num")
                                            {
                                                vconds[ind].subconds_or[j].type = VSUBCOND.VSUBCOND_CTRL_HEX_NUM;
                                                str = pd_vsubcond.GetProperty("player");
                                                vconds[ind].subconds_or[j].player = Player.player_get_by_id(str);
                                                vconds[ind].subconds_or[j].count = int.Parse(pd_vsubcond.GetProperty("count"));
                                            }
                                            else
                                                if (pd_vsubcond.Name == "units_killed")
                                                {
                                                    vconds[ind].subconds_or[j].type = VSUBCOND.VSUBCOND_UNITS_KILLED;
                                                    str = pd_vsubcond.GetProperty("player");
                                                    vconds[ind].subconds_or[j].player = Player.player_get_by_id(str);
                                                    str = pd_vsubcond.GetProperty("tag");
                                                    vconds[ind].subconds_or[j].tag = str;
                                                }
                                                else
                                                    if (pd_vsubcond.Name == "units_saved")
                                                    {
                                                        vconds[ind].subconds_or[j].type = VSUBCOND.VSUBCOND_UNITS_SAVED;
                                                        str = pd_vsubcond.GetProperty("player");
                                                        vconds[ind].subconds_or[j].player = Player.player_get_by_id(str);
                                                        str = pd_vsubcond.GetProperty("tag");
                                                        vconds[ind].subconds_or[j].tag = str;
                                                        vconds[ind].subconds_or[j].count = 0; /* units will be counted */
                                                    }
                                j++;
                            }
                        }
                        /* no sub conditions at all? */
                        if (vconds[ind].sub_and_count == 0 && vconds[ind].sub_or_count == 0)
                        {
                            Console.WriteLine("No subconditions specified!");
                            throw new Exception("No subconditions specified!");
                        }
                        /* next condition */
                        ind++;
                    }
                }
                /* else condition (used if no other condition is fullfilled and Scenario ends) */
                vconds[0].result = "defeat";
                vconds[0].message = "Defeat";
                Block pd_vcond_else = entries[0].GetBlock("else")[0];
                if (pd_vcond_else != null)
                {
                    string str = pd_vcond_else.GetProperty("result");
                    if (!string.IsNullOrEmpty(str))
                        vconds[0].result = str;
                    str = pd_vcond_else.GetProperty("message");
                    if (!string.IsNullOrEmpty(str))
                        vconds[0].message = str;
                }
                /* units */
                Console.WriteLine("Loading Units");
                units = new List<Unit>();
                reinf = new List<Unit>();
                avail_units = new List<Unit>();
                vis_units = new List<Unit>();
                List<Block> unitsBlocks = script.GetBlock("units");
                int unit_ref = 0;
                bool unit_delayed = false;
                Unit unit;
                foreach (Block sub in unitsBlocks[0].Blocks)
                {
                    /* unit type */
                    string str = sub.GetProperty("id");
                    Unit_Lib_Entry unit_prop = unit_lib.unit_lib_find(str);
                    if (unit_prop == null)
                    {
                        Console.WriteLine(str + ": unit entry not found");
                        throw new Exception(str + ": unit entry not found");
                    }
                    /* nation & player */
                    str = sub.GetProperty("nation");
                    Unit unit_base = new Unit();
                    unit_base.nation = Nation.nation_find(str);
                    if (unit_base.nation == null)
                    {
                        Console.WriteLine(str + ": not a nation");
                        throw new Exception(str + ": not a nation");
                    }
                    unit_base.player = Player.player_get_by_nation(unit_base.nation);
                    if (unit_base.player == null)
                    {
                        Console.WriteLine(str + ": no player controls this nation");
                        throw new Exception(str + ": no player controls this nation");
                    }
                    /* name */
                    unit_base.SetGenericName(unit_ref + 1, unit_prop.name);
                    /* delay */
                    str = sub.GetProperty("delay");
                    if (!string.IsNullOrEmpty(str))
                        unit_base.delay = int.Parse(str);
                    unit_delayed = (unit_base.delay != 0);
                    /* position */
                    str = sub.GetProperty("x");
                    if (string.IsNullOrEmpty(str) && !unit_delayed)
                        throw new Exception("Unit has wrong x");
                    if (!string.IsNullOrEmpty(str)) unit_base.x = int.Parse(str);
                    str = sub.GetProperty("y");
                    if (string.IsNullOrEmpty(str) && !unit_delayed)
                        throw new Exception("Unit has wrong y");
                    if (!string.IsNullOrEmpty(str)) unit_base.y = int.Parse(str);
                    if (!unit_delayed && (unit_base.x <= 0 || unit_base.y <= 0 ||
                        unit_base.x >= Engine.map.map_w - 1 || unit_base.y >= Engine.map.map_h - 1))
                    {
                        Console.WriteLine(unit_base.name + ": out of map: ignored");
                        throw new Exception(unit_base.name + ": out of map: ignored");
                    }
                    /* strengt, entrenchment, experience */
                    unit_base.str = int.Parse(sub.GetProperty("str"));
                    unit_base.entr = int.Parse(sub.GetProperty("entr"));
                    unit_base.exp_level = int.Parse(sub.GetProperty("exp"));
                    /* transporter */
                    Unit_Lib_Entry trsp_prop = null;
                    str = sub.GetProperty("trsp");
                    if (!string.IsNullOrEmpty(str) && (str != "none"))
                        trsp_prop = unit_lib.unit_lib_find(str);
                    /* orientation */
                    unit_base.orient = unit_base.player.orient;
                    /* tag if set */
                    unit_base.tag = null;
                    str = sub.GetProperty("tag");
                    if (!string.IsNullOrEmpty(str))
                    {
                        unit_base.tag = str;
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
                    unit = Unit.CreateUnit(unit_prop, trsp_prop, unit_base);
                    /* put unit to active or reinforcements list */
                    if (!unit_delayed)
                    {
                        units.Add(unit);
                        /* add unit to map */
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
                /* load deployment hexes */
                List<Block> deployfields = script.GetBlock("deployfields");
                if (deployfields != null && deployfields.Count > 0)
                {
                    foreach (Block sub in deployfields)
                    {
                        Player pl;
                        int plidx;
                        string str = sub.GetProperty("id");
                        pl = Player.player_get_by_id(str);
                        plidx = Player.player_get_index(pl);
                        string[] values = sub.GetProperty("coordinates").Split('°');
                        if (values.Length > 0)
                        {
                            foreach (string lib in values)
                            {
                                int x, y;
                                if (lib != "default")
                                {
                                    x = -1;
                                    y = -1;
                                }
                                else if (lib != "none")
                                {
                                    pl.no_init_deploy = true;
                                    continue;
                                }
                                else
                                    Misc.get_coord(lib, out x, out y);
                                Engine.map.map_set_deploy_field(x, y, plidx);
                            }
                        }
                    }
                }
                casualties = new int[DB.UnitLib.unit_class_count, scen_info.player_count];
                Engine.deploy_turn = Config.deploy;

            }
            catch (Exception e)
            {
                Console.Error.WriteLine("exception: " + e);
                return false;
            }
            return true;
        }
#endif

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
#if TODO_RR
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
#endif
        /*
        ====================================================================
        Clear the Scenario stuff pointers in 'setup' 
        (loaded by scen_load_info())
        ====================================================================
        */
#if TODO_RR
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
#endif
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
#if TODO_RR
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
#endif
        /*
        ====================================================================
        Check if subcondition is fullfilled.
        ====================================================================
        */
#if TODO_RR
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

#endif
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
#if TODO_RR
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
#endif
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
#if TODO_RR
        public static void scen_adjust_unit_supply_level(Unit unit)
        {
            unit.supply_level = Engine.map.map_get_unit_supply_level(unit.x, unit.y, unit);
        }
#endif
        /*
        ====================================================================
        Get current weather/forecast
        ====================================================================
        */
#if TODO_RR
        public static int scen_get_weather()
        {
            if (turn < scen_info.turn_limit && Config.weather)
                return weather[turn];
            else
                return 0;
        }
#endif
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
