/*
 * Creado por SharpDevelop.
 * Usuario: asantos
 * Fecha: 09/01/2009
 * Hora: 15:53
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Text;
//TODO_RR using System.Drawing;
using System.Collections.Generic;
using DataFile;
using Miscellaneous;
using UnityEngine;
using AI_Enemy;

namespace EngineA
{
    /* ACTION */
    public enum STATUS
    {
        STATUS_NONE = 0,           /* actions that are divided into different phases
                                  have this status set */
        STATUS_MOVE,               /* move unit along 'way' */
        STATUS_ATTACK,             /* unit attacks cur_target (inclusive defensive fire) */
        STATUS_MERGE,              /* human may merge with partners */
        STATUS_SPLIT,              /* human wants to split up a unit */
        STATUS_DEPLOY,             /* human may deploy units */
        STATUS_DROP,               /* select drop zone for parachutists */
        STATUS_INFO,               /* show full unit infos */
        STATUS_SCEN_INFO,          /* show Scenario info */
        STATUS_CONF,               /* run confirm window */
        STATUS_UNIT_MENU,          /* running the unit buttons */
        STATUS_GAME_MENU,          /* game menu */
        STATUS_DEPLOY_INFO,        /* full unit info while deploying */
        STATUS_STRAT_MAP,          /* showing the strategic map */
        STATUS_RENAME,             /* rename unit */
        STATUS_SAVE,               /* running the save edit */
        STATUS_TITLE,              /* show the background */
        STATUS_TITLE_MENU,         /* run title menu */
        STATUS_RUN_SCEN_DLG,       /* run Scenario dialogue */
        STATUS_RUN_CAMP_DLG,       /* run campaign dialogue */
        STATUS_RUN_SETUP,          /* run setup of Scenario */
        STATUS_RUN_MODULE_DLG,     /* select ai module */
        STATUS_CAMP_BRIEFING,      /* run campaign briefing dialogue */
    }
    public enum PHASE
    {
        PHASE_NONE = 0,
        /* COMBAT */
        PHASE_INIT_ATK,             /* initiate attack cross */
        PHASE_SHOW_ATK_CROSS,       /* attacker cross */
        PHASE_SHOW_DEF_CROSS,       /* defender cross */
        PHASE_COMBAT,               /* compute and take damage */
        PHASE_EVASION,              /* sub evades */
        PHASE_RUGGED_DEF,           /* stop the engine for some time and display the rugged defense message */
        PHASE_PREP_EXPLOSIONS,      /* setup the explosions */
        PHASE_SHOW_EXPLOSIONS,      /* animate both explosions */
        PHASE_FIGHT_MSG,		/* show fight status messages */
        PHASE_CHECK_RESULT,         /* clean up this fight and initiate next if any */
        PHASE_BROKEN_UP_MSG,        /* display broken up message if needed */
        PHASE_SURRENDER_MSG,        /* display surrender message */
        PHASE_END_COMBAT,           /* clear status and redraw */
        /* MOVEMENT */
        PHASE_INIT_MOVE,            /* initiate movement */
        PHASE_START_SINGLE_MOVE,    /* initiate movement to next way point from current position */
        PHASE_RUN_SINGLE_MOVE,      /* run single movement and call START_SINGLE_MOVEMENT when done */
        PHASE_CHECK_LAST_MOVE,      /* check last single move for suprise contact, flag capture, Scenario end */
        PHASE_END_MOVE              /* finalize movement */
    };

    /// <summary>
    /// Description of Engine.
    /// </summary>
    public sealed class Engine
    {

        public static Setup setup = DB.setup;
        public static STATUS status = DB.status;                    /* statuses defined in engine_tools.h */
		public static Scenario scen = DB.scen;
#if TODO_RR
        public static campaign camp = DB.camp;
#endif
        public static Terrain terrain = DB.terrain;
        public static Map map = DB.map;
        public static bool term_game = false;
#if TODO_RR
        public static Bitmap Sdl_screen;
        public static EngineStateMachine stateMachine;
#endif

        public static PHASE phase;
        static Way_Point[] way;             /* way points for movement */
        static int way_length = 0;
        static int way_pos = 0;
        public static int dest_x, dest_y;             /* ending point of the way */
        static SDL_Surface move_image;          /* image that contains the moving unit graphic */
        static float move_vel = 0.3f;           /* pixels per millisecond */
        //static Delay move_time;                /* time a single movement takes */
        public struct Vector
        {
            public float x, y;
        }
        static Vector unit_vector;             /* floating position of animation */
        static Vector move_vector;             /* vector the unit moves along */
        static bool surp_contact = false;           /* true if the current combat is the result of a surprise contact */
        static Unit.FIGHT_TYPES atk_result = Unit.FIGHT_TYPES.AR_NONE;             /* result of the attack */
        //static Delay msg_delay;                /* broken up message delay */
        static int atk_took_damage = 0;
        static int def_took_damage = 0;        /* if True an explosion is displayed when attacked */
        static int atk_damage_delta;		/* damage delta for attacking unit */
        static int atk_suppr_delta;		/* supression delta for attacking unit */
        static int def_damage_delta;		/* damage delta for defending unit */
        static int def_suppr_delta;		/* supression delta for defending unit */
        static bool has_danger_zone;	/* whether there are any danger zones */
        public static bool deploy_turn;		/* true if this is the deployment-turn */
        static AI_Enemy.Action top_committed_action;/* topmost action not to be removed */
        //static struct MessagePane *camp_pane; /* state of campaign message pane */
        static string last_debriefing;   /* text of last debriefing */

        public static bool modify_fog = false;      /* if this is False the fog initiated by
                            map_set_fog() is kept throughout the turn
                            else it's updated with movement mask etc */
        public static PLAYERCONTROL cur_ctrl = PLAYERCONTROL.PLAYER_CTRL_CPU;   /* current control type (equals player.ctrl 
                                                                    if set else it's PLAYER_CTRL_NOBODY) */
        public struct Move_Backup
        {         /* unit move backup */
            public bool used;
            public Unit unit;          /* shallow copy of unit */
            /* used to reset map flag if unit captured one */
            public bool flag_saved;     /* these to values used? */
            public Nation dest_nation;
            public Player dest_player;
        } ;
        static Move_Backup move_backup = new Move_Backup(); /* backup to undo last move */

        public static Player cur_player = null;  /* current player pointer */
        static int fleeing_unit = 0;    /* if this is true the unit's move is not backuped */
        static bool air_mode;        /* air units are primary */
        public static bool end_scen = false;        /* True if Scenario is finished or aborted */
        static List<Unit> left_deploy_units; /* list with unit pointers to avail_units of all
                                units that arent placed yet */
        static Unit deploy_unit;   /* current unit selected in deploy list */
        static Unit surrender_unit;/* unit that will surrender */
        public static Unit move_unit;     /* currently moving unit */
        static Unit surp_unit;     /* if set cur_unit has surprise_contact with this unit if moving */
        public static Unit cur_unit;      /* currently selected unit (by human) */
        public static Unit cur_target;    /* target of cur_unit */
        static Unit cur_atk;       /* current attacker - if not defensive fire it's
                            identical with cur_unit */
        static Unit cur_def;       /* is the current defender - identical with cur_target
                            if not defensive fire (then it's cur_unit) */
        public static List<Unit> df_units;      /* this is a list of defensive fire units giving support to 
                            cur_target. as long as this list isn't empty cur_unit
                            becomes the cur_def and cur_atk is the current defensive
                            unit. if this list is empty in the last step
                            cur_unit and cur_target actually do their fight
                            if attack wasn't broken up */
        static int defFire;        /* combat is supportive so switch casualties */
        static int merge_unit_count;
        static Unit[] merge_units = new Unit[Map.MAP_MERGE_UNIT_LIMIT];  /* list of merge partners for cur_unit */
        static int split_unit_count = 0;
        static Unit[] split_units = new Unit[Map.MAP_SPLIT_UNIT_LIMIT]; /* list of split partners */
        int cur_split_str = 0;

        /* DISPLAY */
        enum DISPLAY
        {
            SC_NONE = 0,
            SC_VERT,
            SC_HORI
        }
        static DISPLAY sc_type = DISPLAY.SC_NONE;
        static int sc_diff = 0;  /* screen copy type. used to speed up complete map updates */
        static SDL_Surface sc_buffer = null;    /* screen copy buffer */
        static int[,] hex_mask;             /* used to determine hex from pointer pos */
        static int map_x, map_y;              /* current position in map */
        static int map_sw, map_sh;            /* number of tiles drawn to screen */
        static int map_sx, map_sy;            /* position where to draw first tile */
        public static bool draw_map = false;              /* if this flag is true engine_update() calls engine_draw_map() */
        static bool blind_cpu_turn = false;        /* if this is true all movements are hidden */


        public class MapPoint
        {
            public int x, y;
        }
        public static MapPoint showCross = null;

        /*
        ====================================================================
        End the Scenario and display final message.
        ====================================================================
        */
#if TODO_RR
        public static void engine_finish_scenario()
        {
            /* finalize ai turn if any */
            if (cur_player != null && cur_player.ctrl == PLAYERCONTROL.PLAYER_CTRL_CPU)
                cur_player.ai_finalize();
            blind_cpu_turn = false;
            engine_show_final_message();
#if TODO
            group_set_active(gui.base_menu, ID_MENU, 0);
#endif
            draw_map = false;
#if TODO
            image_hide(gui.cursors, 0);
            gui_set_cursor(CURSOR_STD);
#endif
            engine_select_player(null, false);
            Scenario.turn = Scenario.scen_info.turn_limit;
            engine_set_status(STATUS.STATUS_NONE);
            phase = PHASE.PHASE_NONE;
        }
#endif
        /*
        ====================================================================
        Return the first human player.
        ====================================================================
        */
        static Player engine_human_player(out int human_count)
        {
            Player human = null;
            int count = 0;
            for (int i = 0; i < Player.players.Count; i++)
            {
                Player player = Player.players[i];
                if (player.ctrl == PLAYERCONTROL.PLAYER_CTRL_HUMAN)
                {
                    if (count == 0)
                        human = player;
                    count++;
                }
            }
            human_count = count;
            return human;
        }

        /*
        ====================================================================
        Clear danger zone.
        ====================================================================
        */
        public static void engine_clear_danger_mask()
        {
            if (has_danger_zone)
            {
                map.map_clear_mask(MAP_MASK.F_DANGER);
                has_danger_zone = false;
            }
        }

        /*
        ====================================================================
        Set wanted status.
        ====================================================================
        */
        public static void engine_set_status(STATUS newstat)
        {
            if (newstat == STATUS.STATUS_NONE && setup.type == SETUP.SETUP_RUN_TITLE)
            {
                status = STATUS.STATUS_TITLE;
                /* re-show main menu */
                if (!term_game) engine_show_game_menu(10, 10);
            }
            else
                status = newstat;
        }

        /*
====================================================================
Draw wallpaper and background.
====================================================================
*/
        static void engine_draw_bkgnd()
        {
#if TODO
            int i, j;
            for ( j = 0; j < sdl.screen.h; j += gui.wallpaper.h )
                for ( i = 0; i < sdl.screen.w; i += gui.wallpaper.w ) {
                    DEST( sdl.screen, i, j, gui.wallpaper.w, gui.wallpaper.h );
                    SOURCE( gui.wallpaper, 0, 0 );
                    blit_surf();
                }
            DEST( sdl.screen, 
                  ( sdl.screen.w - gui.bkgnd.w ) / 2,
                  ( sdl.screen.h - gui.bkgnd.h ) / 2,
                  gui.bkgnd.w, gui.bkgnd.h );
            SOURCE( gui.bkgnd, 0, 0 );
            blit_surf();
            throw new NotImplementedException();
#endif
        }

        /*
        ====================================================================
        Returns true when the status screen dismission events took place.
        ====================================================================
        */
        static int engine_status_screen_dismissed()
        {
#if TODO
            int dummy;
            return event_get_buttonup( &dummy, &dummy, &dummy )
                    || event_check_key(SDLK_SPACE)
                    || event_check_key(SDLK_RETURN)
                    || event_check_key(SDLK_ESCAPE);
#endif
            throw new NotImplementedException();

        }

        /*
        ====================================================================
        Store debriefing of last Scenario or an empty string.
        ====================================================================
        */
#if TODO_RR
        static void engine_store_debriefing(string result)
        {
            string str = campaign.camp_get_description(result);
            last_debriefing = null;
            if (!string.IsNullOrEmpty(str))
                last_debriefing = str;
        }
#endif
        /*
        ====================================================================
        Prepare display of next campaign briefing.
        ====================================================================
        */
#if TODO_RR
        static void engine_prep_camp_brief()
        {
#if TODO
            gui_delete_message_pane(camp_pane);
            camp_pane = 0;
            engine_draw_bkgnd();

            camp_pane = gui_create_message_pane();
#endif
            /* make up Scenario text */
            {
                StringBuilder txt = new StringBuilder();
                if (!string.IsNullOrEmpty(campaign.camp_cur_scen.title))
                {
                    txt.Append(campaign.camp_cur_scen.title);
                    txt.Append("##");
                }
                if (!string.IsNullOrEmpty(last_debriefing))
                {
                    txt.Append(last_debriefing);
                    txt.Append(" ");
                }
                if (!string.IsNullOrEmpty(campaign.camp_cur_scen.brief))
                {
                    txt.Append(campaign.camp_cur_scen.brief);
                    txt.Append("##");
                }
#if TODO
                gui_set_message_pane_text(camp_pane, txt);
#endif
            }

            /* provide options or default id */
            if (!string.IsNullOrEmpty(campaign.camp_cur_scen.scen))
            {
#if TODO
                gui_set_message_pane_default(camp_pane, "nextscen");
#endif
            }
            else
            {
                List<string> ids = campaign.camp_get_result_list();
                List<string> vals = new List<string>();

                foreach (string result in ids)
                {
                    string desc = campaign.camp_get_description(result);
                    vals.Add(!string.IsNullOrEmpty(desc) ? desc : result);
                }

                /* no ids means finishing state */
#if TODO

                if (ids.Count == 0)
                    gui_set_message_pane_default(camp_pane, " ");
                /* don't provide explicit selection if there is only one id */
                else if (ids.count == 1)
                    gui_set_message_pane_default(camp_pane, list_first(ids));
                else
                    gui_set_message_pane_options(camp_pane, ids, vals);
#endif
            }
#if TODO
            gui_draw_message_pane(camp_pane);
#endif
            engine_set_status(STATUS.STATUS_CAMP_BRIEFING);
        }
#endif
        /*
        ====================================================================
        Show turn info (done before turn)
        ====================================================================
        */
#if TODO_RR
        static void engine_show_turn_info()
        {
            StringBuilder str = new StringBuilder();
            if (cur_player.ctrl == PLAYERCONTROL.PLAYER_CTRL_HUMAN)
            {

                str.Append(Scenario.scen_get_date() + "\n");
                str.Append("Next Player: " + cur_player.name);
                if (deploy_turn)
                {
                    if (cur_player.ctrl == PLAYERCONTROL.PLAYER_CTRL_HUMAN)
                    {
                        str.Append("\nDeploy your troops");
                        form.ShowMessageAndWait(str.ToString());
                        return;
                    }
                    /* don't show screen for computer-controlled players */
                    return;
                }

                if (Scenario.turn + 1 < Scenario.scen_info.turn_limit)
                {
                    str.Append("\nRemaining Turns: " + (Scenario.scen_info.turn_limit - Scenario.turn));
                }
                str.Append("\nWeather: " + terrain.weatherTypes[Scenario.scen_get_weather()].name);
                if (Scenario.turn + 1 < Scenario.scen_info.turn_limit)
                    str.Append("\nTurn: " + (Scenario.turn + 1));
                else
                    str.Append("\nLast Turn");

                form.ShowMessageAndWait(str.ToString());
            }
        }
#endif
        /*
        ====================================================================
        Backup data that will be restored when unit move was undone.
        (destination flag, spot mask, unit position)
        If x != -1 the flag at x,y will be saved.
        ====================================================================
        */
#if TODO_RR
        public static void engine_backup_move(Unit unit, int x, int y)
        {
            if (!move_backup.used)
            {
                move_backup.used = true;
                move_backup.unit = unit;
                map.map_backup_spot_mask();
            }
            if (x != -1)
            {
                move_backup.dest_nation = map.map[x, y].nation;
                move_backup.dest_player = map.map[x, y].player;
                move_backup.flag_saved = true;
            }
            else
                move_backup.flag_saved = false;
        }
        static void engine_undo_move(Unit unit)
        {
            UnitEmbarkTypes new_embark;
            if (!move_backup.used) return;
            map.map_remove_unit(unit);
            if (move_backup.flag_saved)
            {
                map.map[unit.x, unit.y].player = move_backup.dest_player;
                map.map[unit.x, unit.y].nation = move_backup.dest_nation;
                move_backup.flag_saved = false;
            }
            /* get stuff before restoring pointer */
            new_embark = unit.embark;
            /* restore */
            unit = move_backup.unit;
            /* check debark/embark counters */
            if (unit.embark == UnitEmbarkTypes.EMBARK_NONE)
            {
                if (new_embark == UnitEmbarkTypes.EMBARK_AIR)
                    unit.player.air_trsp_used--;
                if (new_embark == UnitEmbarkTypes.EMBARK_SEA)
                    unit.player.sea_trsp_used--;
            }
            else
                if (unit.embark == UnitEmbarkTypes.EMBARK_SEA && new_embark == UnitEmbarkTypes.EMBARK_NONE)
                    unit.player.sea_trsp_used++;
                else
                    if (unit.embark == UnitEmbarkTypes.EMBARK_AIR && new_embark == UnitEmbarkTypes.EMBARK_NONE)
                        unit.player.air_trsp_used++;
            unit.AdjustIcon(); /* adjust picture as direction may have changed */
            map.map_insert_unit(unit);
            map.map_restore_spot_mask();
            if (modify_fog) map.map_set_fog(MAP_MASK.F_SPOT);
            move_backup.used = false;
        }
#endif
        public static void engine_clear_backup()
        {
            move_backup.used = false;
            move_backup.flag_saved = false;
        }

        /*
        ====================================================================
        Remove unit from map and unit list and clear it's influence.
        ====================================================================
        */
#if TODO_RR
        static void engine_remove_unit(Unit unit)
        {
            if (unit.killed >= 2) return;

            /* check if it's an enemy to the current player; if so the influence must be removed */
            if (!Player.player_is_ally(cur_player, unit.player))
                map.map_remove_unit_infl(unit);
            map.map_remove_unit(unit);
            /* from unit list */
            unit.killed = 2;
        }
#endif

        /*
        ====================================================================
        Select this unit and unselect old selection if nescessary.
        Clear the selection if NULL is passed as unit.
        ====================================================================
        */
        public static void engine_select_unit(Unit unit)
        {
            /* select unit */
            cur_unit = unit;
            engine_clear_danger_mask();
            if (cur_unit == null)
            {
                /* clear view */
                if (modify_fog) map.map_set_fog(MAP_MASK.F_SPOT);
                engine_clear_backup();
                return;
            }
            /* switch air/ground */
            if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
                air_mode = true;
            else
                air_mode = false;
			
            /* get merge partners and set merge_unit mask */
            map.map_get_merge_units(cur_unit, out merge_units, out merge_unit_count);
            /* moving range */
            map.map_get_unit_move_mask(unit);

            if (modify_fog && unit.cur_mov > 0)
            {
                map.map_set_fog(MAP_MASK.F_IN_RANGE);
                map.mask[unit.x, unit.y].fog = false;
            }
            else
                map.map_set_fog(MAP_MASK.F_SPOT);
            /* determine danger zone for air units */
            if (modify_fog && Config.supply && (unit.cur_mov != 0)
                 && ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) && (unit.sel_prop.fuel != 0))
                has_danger_zone = map.map_get_danger_mask(unit);

            return;
        }

        /*
        ====================================================================
        Return current units in avail_units to reinf list. Get all valid
        reinforcements for the current player from reinf and put them to
        avail_units. Aircrafts come first.
        ====================================================================
        */
        static void engine_update_avail_reinf_list()
        {
            Unit unit;
            /* available reinforcements */
            while (Scenario.avail_units.Count > 0)
                list_transfer(Scenario.avail_units, Scenario.reinf, Scenario.avail_units[0]);

            /* add all units from scen::reinf whose delay <= cur_turn */
            for (int i = 0; i < Scenario.reinf.Count; i++)
            {
                unit = Scenario.reinf[i];
                if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING &&
                     unit.player == cur_player && unit.delay <= Scenario.turn)
                {
                    list_transfer(Scenario.reinf, Scenario.avail_units, unit);
                    /* index must be reset if unit was added */
                    i--;
                }
            }
            for (int i = 0; i < Scenario.reinf.Count; i++)
            {
                unit = Scenario.reinf[i];
                if (((unit.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING) &&
                     unit.player == cur_player && unit.delay <= Scenario.turn)
                {
                    list_transfer(Scenario.reinf, Scenario.avail_units, unit);
                    /* index must be reset if unit was added */
                    i--;
                }
            }
        }

        /*
        ====================================================================
        Initiate player as current player and prepare its turn.
        If 'skip_unit_prep' is set scen_prep_unit() is not called.
        ====================================================================
        */
        public static void engine_select_player(Player player, bool skip_unit_prep)
        {
            Player human;
            int human_count;
            cur_player = player;
            if (player != null)
                cur_ctrl = player.ctrl;
            else
                cur_ctrl = PLAYERCONTROL.PLAYER_CTRL_NOBODY;
            if (!skip_unit_prep)
            {
                /* update available reinforcements */
                engine_update_avail_reinf_list();
                /* prepare units for turn -- fuel, mov-points, entr, weather etc */
                for (int i = 0; i < Scenario.units.Count; i++)
                {
                    Unit unit = Scenario.units[i];
                    if (unit.player == cur_player)
                    {
                        if (Scenario.turn == 0)
                            Scenario.scen_prep_unit(unit, Scenario.SCEN_PREP.SCEN_PREP_UNIT_FIRST);
                        else
                            Scenario.scen_prep_unit(unit, Scenario.SCEN_PREP.SCEN_PREP_UNIT_NORMAL);
                    }
                }
            }
            /* set fog */
            switch (cur_ctrl)
            {
                case PLAYERCONTROL.PLAYER_CTRL_HUMAN:
                    modify_fog = true;
                    map.map_set_spot_mask();
                    map.map_set_fog(MAP_MASK.F_SPOT);
                    break;
                case PLAYERCONTROL.PLAYER_CTRL_NOBODY:
                    for (int x = 0; x < map.map_w; x++)
                        for (int y = 0; y < map.map_h; y++)
                            map.mask[x, y].spot = true;
                    map.map_set_fog(0);
                    break;
                case PLAYERCONTROL.PLAYER_CTRL_CPU:
                    human = engine_human_player(out human_count);
                    if (human_count == 1)
                    {
                        modify_fog = false;
                        map.map_set_spot_mask();
                        map.map_set_fog_by_player(human);
                    }
                    else
                    {
                        modify_fog = true;
                        map.map_set_spot_mask();
                        map.map_set_fog(MAP_MASK.F_SPOT);
                    }
                    break;
            }
            /* count down deploy center delay's (1==deploy center again) */
            for (int x = 0; x < map.map_w; x++)
                for (int y = 0; y < map.map_h; y++)
                    if (map.map[x, y].deploy_center > 1 && map.map[x, y].player == cur_player)
                        map.map[x, y].deploy_center--;
            /* set influence mask */
            if (cur_ctrl != PLAYERCONTROL.PLAYER_CTRL_NOBODY)
                map.map_set_infl_mask();
            map.map_get_vis_units();
            if (!skip_unit_prep)
            {

                /* prepare deployment dialog on deployment-turn */
#if TODO
                if (deploy_turn
                     && cur_player && cur_player.ctrl == PLAYERCONTROL.PLAYER_CTRL_HUMAN)
                    engine_handle_button(ID_DEPLOY);
                else
                    group_hide(gui.deploy_window, 1);
#endif
                /* supply levels */
                foreach (Unit unit in Scenario.units)
                    if (unit.player == cur_player)
                        Scenario.scen_adjust_unit_supply_level(unit);

                /* mark unit as re-deployable in deployment-turn when it is
                 * located on a deployment-field and is allowed to be re-deployed.
                 */
                if (deploy_turn)
                {
                    map.map_get_deploy_mask(cur_player, null, true);
                    foreach (Unit unit in Scenario.units)
                    {
                        if (unit.player == cur_player)
                            if (map.mask[unit.x, unit.y].deploy)
                                if (unit.SupportsDeploy())
                                {
                                    unit.fresh_deploy = true;
                                    list_transfer(Scenario.units, Scenario.avail_units, unit);
                                }
                    }
                }
            }
            /* clear selections/actions */
            cur_unit = cur_target = cur_atk = cur_def = surp_unit = move_unit = deploy_unit = null;
            merge_unit_count = 0;
            if (df_units != null)
                df_units.Clear();
#if TODO_RR
            Action.actions_clear();
#endif
#if TODO
            scroll_block = 0;
#endif
        }

        /*
        ====================================================================
        Begin turn of next player. Therefore select next player or use
        'forced_player' if not NULL (then the next is the one after 
        'forced_player').
        If 'skip_unit_prep' is set scen_prep_unit() is not called.
        ====================================================================
        */

        public static void engine_begin_turn(Player forced_player, bool skip_unit_prep)
        {
            char[] text = new char[400];
            int new_turn = 0;
            Player player = null;
#if TODO
    /* clear various stuff that may be still set from last turn */
    group_set_active( gui.confirm, ID_OK, 1 );
    engine_hide_unit_menu();
    engine_hide_game_menu();
#endif
            /* clear undo */
            engine_clear_backup();
            /* clear hideous clicks */
#if TODO
            if ( !deploy_turn && cur_ctrl == PLAYERCONTROL.PLAYER_CTRL_HUMAN ) 
                event_wait_until_no_input();
#endif
			
            /* get player */
            if (forced_player == null)
            {
                /* next player and turn */
                player = Player.players_get_next(out new_turn);
                if (new_turn != 0)
                {
                    Scenario.turn++;
                }
                if (Scenario.turn == Scenario.scen_info.turn_limit)
                {
                    /* use else condition as Scenario result */
                    /* and take a final look */
                    Scenario.scen_check_result(true);
                    blind_cpu_turn = false;
                    engine_show_final_message();
                    draw_map = true;
#if TODO
                    image_hide(gui.cursors, 0);
                    gui_set_cursor(CURSOR_STD);
#endif
                    engine_select_player(null, skip_unit_prep);
                    engine_set_status(STATUS.STATUS_NONE);
                    phase = PHASE.PHASE_NONE;
                    return;
                }
                else
                {
                    Scenario.cur_weather = Scenario.scen_get_weather();
                    engine_select_player(player, skip_unit_prep);
                }
            }

            else
            {
                engine_select_player(forced_player, skip_unit_prep);
                Player.players_set_current(Player.player_get_index(forced_player));
            }
#if DEBUG_CAMPAIGN
            if ( scen_check_result(0) ) {
                blind_cpu_turn = 0;
                engine_show_final_message();
                draw_map = 1;
                image_hide( gui.cursors, 0 );
                gui_set_cursor( CURSOR_STD );
                engine_select_player( 0, skip_unit_prep );
                engine_set_status( STATUS_NONE ); 
                phase = PHASE_NONE;
                end_scen = 1;
                return;
            }
#endif

            /* init ai turn if any */
            if (cur_player != null && cur_player.ctrl == PLAYERCONTROL.PLAYER_CTRL_CPU)
                cur_player.ai_init();
#if TODO_RR
            /* turn info */
            engine_show_turn_info();
#endif
            engine_set_status(deploy_turn ? STATUS.STATUS_DEPLOY : STATUS.STATUS_NONE);
            phase = PHASE.PHASE_NONE;
            /* update screen */
            if (cur_ctrl != PLAYERCONTROL.PLAYER_CTRL_CPU || Config.show_cpu_turn)
            {
#if TODO
                if (cur_ctrl == PLAYERCONTROL.PLAYER_CTRL_CPU)
                    engine_update_info(0, 0, 0);
                else
                {
                    image_hide(gui.cursors, 0);
                }
                engine_draw_map();
#endif
                blind_cpu_turn = false;
            }
            else
            {
#if TODO
                engine_update_info(0, 0, 0);
                draw_map = false;
                FULL_DEST(sdl.screen);
                fill_surf(0x0);
                gui.font_turn_info.align = ALIGN_X_CENTER | ALIGN_Y_CENTER;
                sprintf(text, tr("CPU thinks..."));
                write_text(gui.font_turn_info, sdl.screen, sdl.screen.w >> 1, sdl.screen.h >> 1, text, OPAQUE);
                sprintf(text, tr("( Enable option 'Show Cpu Turn' if you want to see what it is doing. )"));
                write_text(gui.font_turn_info, sdl.screen, sdl.screen.w >> 1, (sdl.screen.h >> 1) + 20, text, OPAQUE);
                refresh_screen(0, 0, 0, 0);
#endif
                blind_cpu_turn = true;
            }
            if (cur_ctrl == PLAYERCONTROL.PLAYER_CTRL_CPU)
            {
                while (!cur_player.ai_run()) ;
            }

        }

        /*
        ====================================================================
        End turn of current player without selecting next player. Here 
        autosave happens, aircrafts crash, units get supplied.
        ====================================================================
        */
#if TODO_RR
        public static void engine_end_turn()
        {
            /* finalize ai turn if any */
            if (cur_player != null && cur_player.ctrl == PLAYERCONTROL.PLAYER_CTRL_CPU)
                cur_player.ai_finalize();
            /* if turn == scen_info.turn_limit this was a final look */
            if (Scenario.turn == Scenario.scen_info.turn_limit)
            {
                end_scen = true;
                return;
            }
            /* autosave game for a human */
            if (!deploy_turn && cur_player != null && cur_player.ctrl == PLAYERCONTROL.PLAYER_CTRL_HUMAN)
                Slots.slot_save(10 /* Autosave */, "Autosave");
            /* fuel up and kill crashed aircrafts*/
            foreach (Unit unit in Scenario.units)
            {
                if (unit.player != cur_player) continue;
                /* supply unused ground units just as if it were done manually and
                   any aircraft with the updated supply level */
                if (Config.supply && unit.CheckFuelUsage())
                {
                    if ((unit.prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
                    {
                        /* loose half fuel even if not moved */
                        if (unit.cur_mov > 0) /* FIXME: this goes wrong if units may move multiple times */
                        {
                            unit.cur_fuel -= unit.CalcFuelUsage(  0);
                            if (unit.cur_fuel < 0) unit.cur_fuel = 0;
                        }
                        /* supply? */
                        Scenario.scen_adjust_unit_supply_level(unit);
                        if (unit.supply_level > 0)
                        {
                            unit.unused = true; /* required to fuel up */
                            unit.Supply(Unit.UNIT_SUPPLY.UNIT_SUPPLY_ALL);
                        }
                        /* crash if no fuel */
                        if (unit.cur_fuel == 0)
                            unit.killed = 1;
                    }
                    else if (unit.unused && unit.supply_level > 0)
                        unit.Supply(Unit.UNIT_SUPPLY.UNIT_SUPPLY_ALL);
                }
            }
            /* remove all units that were killed in the last turn */
            for (int i = 0; i < Scenario.units.Count; i++)
            {
                Unit unit = Scenario.units[i];
                if (unit.killed != 0)
                {
                    engine_remove_unit(unit);
                    Scenario.units.Remove(unit);
                    i--; /* adjust index */
                }
            }
        }
#endif
        /*
        ====================================================================
        Get map/screen position from cursor/map position.
        ====================================================================
        */
#if TODO_RR
        public static bool engine_get_screen_pos(int mx, int my, out int sx, out int sy)
        {
            sx = sy = -1;
            int x = map_sx, y = map_sy;
            /* this is the starting position if x-pos of first tile on screen is not odd */
            /* if it is odd we must add the y_offset to the starting position */
            if (Misc.ODD(map_x))
                y += Engine.terrain.hex_y_offset;
            /* reduce to visible map tiles */
            mx -= map_x;
            my -= map_y;
            /* check range */
            if (mx < 0 || my < 0) return false;
            /* compute pos */
            x += mx * Engine.terrain.hex_x_offset;
            y += my * Engine.terrain.hex_h;
            /* if x_pos of first tile is even we must add y_offset to the odd tiles in screen */
            if (Misc.EVEN(map_x))
            {
                if (Misc.ODD(mx))
                    y += Engine.terrain.hex_y_offset;
            }
            else
            {
                /* we must substract y_offset from even tiles */
                if (Misc.ODD(mx))
                    y -= Engine.terrain.hex_y_offset;
            }
            /* check range */
            //if (x >= sdl.screen.w || y >= sdl.screen.h) return false;
            /* assign */
            sx = x;
            sy = y;
            return true;
        }
#endif

        public enum REGION
        {
            REGION_GROUND = 0,
            REGION_AIR,
            REGION_NONE
        };

        public static bool engine_get_map_pos(float sx, float sy, out int mx, out int my, out REGION region)
        {
            mx = my = -1;
			region = REGION.REGION_NONE;
            int x = 0, y = 0;
            if (map.isLoaded){
				int auxWidth = Engine.map.map_w-1;
				int auxHeight = Engine.map.map_h-1;
				int width = auxWidth*Config.hex_x_offset;
				int height = -Config.hex_h*auxHeight-Config.hex_y_offset;
				if (sx>=-Config.hex_w/2 && sx<=width+Config.hex_w/2 && sy<=Config.hex_h/2 && sy>=height-Config.hex_h/2){
					x = Misc.GetWidthtPosition(sx);
					y = Misc.GetHeightPosition(sy,x);
				}
				int tile_y = (Misc.IsEven(x))?-y*Config.hex_h:-y*Config.hex_h-Config.hex_y_offset;
				if (tile_y<sy)
					region = REGION.REGION_AIR;
            	else
                	region = REGION.REGION_GROUND;
				mx = x;
            	my = y;
				/* check range */
	            if (x < 0 || y < 0 || x >= Engine.map.map_w || y >= Engine.map.map_h) return false;
	            /* ok, tile exists */
	            return true;
			}
			else{
				return false;
			}
            
        }
        /*
        ====================================================================
        If x,y is not on screen center this map tile and check if 
        screencopy is possible (but only if use_sc is True)
        ====================================================================
        */
#if TODO_RR
        static bool engine_focus(int x, int y, bool use_sc)
        {
            int new_x, new_y;
            if (x <= map_x + 1 || y <= map_y + 1 || x >= map_x + map_sw - 1 - 2 || y >= map_y + map_sh - 1 - 2)
            {
                new_x = x - (map_sw >> 1);
                new_y = y - (map_sh >> 1);
                if (Misc.ODD(new_x)) new_x++;
                if (Misc.ODD(new_y)) new_y++;
                engine_goto_xy(new_x, new_y);
                if (!use_sc)
                    sc_type = DISPLAY.SC_NONE; /* no screencopy */
                return true;
            }
            return false;
        }
#endif
        /*
        ====================================================================
        Move to this position and set 'draw_map' if actually moved.
        ====================================================================
        */
#if TODO_RR
        static void engine_goto_xy(int x, int y)
        {
            int x_diff, y_diff;
            /* check range */
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            /* if more tiles are displayed then map has ( black space the rest ) no change in position allowed */
            if (map_sw >= map.map_w) x = 0;
            else
                if (x > map.map_w - map_sw)
                    x = map.map_w - map_sw;
            if (map_sh >= map.map_h) y = 0;
            else
                if (y > map.map_h - map_sh)
                    y = map.map_h - map_sh;
            /* check if screencopy is possible */
            x_diff = x - map_x;
            y_diff = y - map_y;
            /* if one diff is ==0 and one diff !=0 do it! */
            if (x_diff == 0 && y_diff != 0)
            {
                sc_type = DISPLAY.SC_VERT;
                sc_diff = y_diff;
            }
            else
                if (x_diff != 0 && y_diff == 0)
                {
                    sc_type = DISPLAY.SC_HORI;
                    sc_diff = x_diff;
                }
            /* actually moving? */
            if (x != map_x || y != map_y)
            {
                map_x = x; map_y = y;
                draw_map = true;
            }
        }
#endif
        /*
        ====================================================================
        Update full map.
        ====================================================================
        */
#if TODO_RR
        public static void engine_draw_map(SDL_Surface sdl)
        {
            int x, y, abs_y;
            int start_map_x, start_map_y, end_map_x, end_map_y;
            int buffer_height, buffer_width, buffer_offset;
            bool use_frame = (cur_ctrl != PLAYERCONTROL.PLAYER_CTRL_CPU);

            DrawStage stage = DrawStage.DrawTerrain;
            DrawStage top_stage = (has_danger_zone ? DrawStage.DrawDangerZone : DrawStage.DrawUnits);

            /* reset_timer(); */

            draw_map = false;
            /* reset engine's map size (number of tiles on screen) */
            map_sw = 0;
            for (int i = map_sx; i < sdl.w; i += terrain.hex_x_offset)
                map_sw++;
            map_sh = 0;
            for (int j = map_sy; j < sdl.h; j += terrain.hex_h)
                map_sh++;

            if (status == STATUS.STATUS_STRAT_MAP)
            {
                sc_type = DISPLAY.SC_NONE;
                strat_map.strat_map_draw();
                return;
            }

            if (status == STATUS.STATUS_TITLE)
            {
                sc_type = DISPLAY.SC_NONE;
                engine_draw_bkgnd();
                return;
            }
            /* screen copy? */
            start_map_x = map_x;
            start_map_y = map_y;
            end_map_x = map_x + map_sw;
            end_map_y = map_y + map_sh;
#if TODO
            if (sc_type == DISPLAY.SC_VERT)
            {
                /* clear flag */
                sc_type = DISPLAY.SC_NONE;
                /* set buffer offset and height */
                buffer_offset = Math.Abs(sc_diff) * hex_h;
                buffer_height = sdl.screen.h - buffer_offset;
                /* going down */
                if (sc_diff > 0)
                {
                    /* copy screen to buffer */
                    DEST(sc_buffer, 0, 0, sdl.screen.w, buffer_height);
                    SOURCE(sdl.screen, 0, buffer_offset);
                    blit_surf();
                    /* copy buffer to new pos */
                    DEST(sdl.screen, 0, 0, sdl.screen.w, buffer_height);
                    SOURCE(sc_buffer, 0, 0);
                    blit_surf();
                    /* set loop range to redraw lower lines */
                    start_map_y += map_sh - sc_diff - 2;
                }
                /* going up */
                else
                {
                    /* copy screen to buffer */
                    DEST(sc_buffer, 0, 0, sdl.screen.w, buffer_height);
                    SOURCE(sdl.screen, 0, 0);
                    blit_surf();
                    /* copy buffer to new pos */
                    DEST(sdl.screen, 0, buffer_offset, sdl.screen.w, buffer_height);
                    SOURCE(sc_buffer, 0, 0);
                    blit_surf();
                    /* set loop range to redraw upper lines */
                    end_map_y = map_y + abs(sc_diff) + 1;
                }
            }
            else
                if (sc_type == SC_HORI)
                {
                    /* clear flag */
                    sc_type = SC_NONE;
                    /* set buffer offset and width */
                    buffer_offset = abs(sc_diff) * hex_x_offset;
                    buffer_width = sdl.screen.w - buffer_offset;
                    buffer_height = sdl.screen.h;
                    /* going right */
                    if (sc_diff > 0)
                    {
                        /* copy screen to buffer */
                        DEST(sc_buffer, 0, 0, buffer_width, buffer_height);
                        SOURCE(sdl.screen, buffer_offset, 0);
                        blit_surf();
                        /* copy buffer to new pos */
                        DEST(sdl.screen, 0, 0, buffer_width, buffer_height);
                        SOURCE(sc_buffer, 0, 0);
                        blit_surf();
                        /* set loop range to redraw right lines */
                        start_map_x += map_sw - sc_diff - 2;
                    }
                    /* going left */
                    else
                    {
                        /* copy screen to buffer */
                        DEST(sc_buffer, 0, 0, buffer_width, buffer_height);
                        SOURCE(sdl.screen, 0, 0);
                        blit_surf();
                        /* copy buffer to new pos */
                        DEST(sdl.screen, buffer_offset, 0, buffer_width, buffer_height);
                        SOURCE(sc_buffer, 0, 0);
                        blit_surf();
                        /* set loop range to redraw right lines */
                        end_map_x = map_x + abs(sc_diff) + 1;
                    }
                }
#endif
            for (; stage <= top_stage; stage++)
            {
                /* start position for drawing */
                x = map_sx + (start_map_x - map_x) * terrain.hex_x_offset;
                y = map_sy + (start_map_y - map_y) * terrain.hex_h;
                /* end_map_xy must not exceed map's size */
                if (end_map_x >= map.map_w) end_map_x = map.map_w;
                if (end_map_y >= map.map_h) end_map_y = map.map_h;
                /* loop to draw map tile */
                for (int j = start_map_y; j < end_map_y; j++)
                {
                    for (int i = start_map_x; i < end_map_x; i++)
                    {
                        /* update each map tile */
                        if (i % 2 != 0) // it's odd
                            abs_y = y + terrain.hex_y_offset;
                        else
                            abs_y = y;
                        switch (stage)
                        {
                            case DrawStage.DrawTerrain:
                                map.map_draw_terrain(sdl, i, j, x, abs_y);
                                break;
                            case DrawStage.DrawUnits:

                                if (cur_unit != null && cur_unit.x == i && cur_unit.y == j && status != STATUS.STATUS_MOVE && Engine.map.mask[i, j].spot)
                                    map.map_draw_units(sdl, i, j, x, abs_y, !air_mode, use_frame);
                                else
                                    map.map_draw_units(sdl, i, j, x, abs_y, !air_mode, false);

                                break;
                            case DrawStage.DrawDangerZone:
                                if (map.mask[i, j].danger)
                                    map.map_apply_danger_to_tile(sdl, i, j, x, abs_y);
                                break;
                        }
                        x += terrain.hex_x_offset;
                    }
                    y += terrain.hex_h;
                    x = map_sx + (start_map_x - map_x) * terrain.hex_x_offset;
                }
            }
            if (showCross != null)
            {
                /* start position for drawing */
                x = map_sx + showCross.x * terrain.hex_x_offset;
                y = map_sy + showCross.y * terrain.hex_h;
                /* update each map tile */
                if (showCross.x % 2 != 0) // it's odd
                    abs_y = y + terrain.hex_y_offset;
                else
                    abs_y = y;

                SDL_Surface.copy_image(sdl,
                      x,
                      abs_y,
                      terrain.terrainIcons.cross.w, terrain.terrainIcons.cross.h,
                      terrain.terrainIcons.cross, 0, 0);
            }
            /* printf( "time needed: %i ms\n", get_time() ); */
        }
#endif
        /*
        ====================================================================
        Get primary unit on tile.
        ====================================================================
        */

        public static Unit engine_get_prim_unit(int x, int y, REGION region)
        {
            if (x < 0 || y < 0 || x >= map.map_w || y >= map.map_h) return null;
            if (region == REGION.REGION_AIR)
            {
                if (map.map[x, y].a_unit != null)
                    return map.map[x, y].a_unit;
                else
                    return map.map[x, y].g_unit;
            }
            else
            {
                if (map.map[x, y].g_unit != null)
                    return map.map[x, y].g_unit;
                else
                    return map.map[x, y].a_unit;
            }
        }

        /*
        ====================================================================
        Check if there is a target for current unit on x,y.
        ====================================================================
        */

        public static Unit engine_get_target(int x, int y, REGION region)
        {
            Unit unit;
            if (x < 0 || y < 0 || x >= map.map_w || y >= map.map_h) return null;
            if (!map.mask[x, y].spot) return null;
            if (cur_unit == null) return null;
            unit = engine_get_prim_unit(x, y, region);
            if (unit != null)
                if (cur_unit.CheckAttack(  unit, Unit.UNIT_ATTACK.UNIT_ACTIVE_ATTACK))
                    return unit;
            return null;
        }


        /*
        ====================================================================
        Check if there is a selectable unit for current player on x,y
        The currently selected unit is not counted as selectable. (though
        a primary unit on the same tile may be selected if it's not
        the current unit)
        ====================================================================
        */
        public static Unit engine_get_select_unit(int x, int y, REGION region)
        {
            if (x < 0 || y < 0 || x >= Engine.map.map_w || y >= Engine.map.map_h) return null;
#if TODO_RR
            if (!Engine.map.mask[x, y].spot) return null;
#endif
			
            if (region == REGION.REGION_AIR)
            {
                if (map.map[x, y].a_unit != null && map.map[x, y].a_unit.player == cur_player)
                {
                    if (cur_unit == map.map[x, y].a_unit)
                        return null;
                    else
                        return map.map[x, y].a_unit;
                }
                else
                    if (map.map[x, y].g_unit != null && map.map[x, y].g_unit.player == cur_player)
                        return map.map[x, y].g_unit;
                    else
                        return null;
            }
            else
            {
                if (map.map[x, y].g_unit != null && map.map[x, y].g_unit.player == cur_player)
                {
                    if (cur_unit == map.map[x, y].g_unit)
                        return null;
                    else
                        return map.map[x, y].g_unit;
                }
                else
                    if (map.map[x, y].a_unit != null && map.map[x, y].a_unit.player == cur_player)
                        return map.map[x, y].a_unit;
                    else
                        return null;
            }
        }
        /*
        ====================================================================
        Get next combatants assuming that cur_unit attacks cur_target.
        Set cur_atk and cur_def and return True if there are any more
        combatants.
        ====================================================================
        */
        public static bool engine_get_next_combatants()
        {
            bool fight = false;
            string str;
            /* check if there are supporting units; if so initate fight 
               between attacker and these units */
            if (df_units.Count > 0)
            {
                cur_atk = df_units[0];
                cur_def = cur_unit;
                fight = true;
                defFire = 1;
                /* set message if seen */
                if (!blind_cpu_turn)
                {
                    if ((cur_atk.sel_prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY)
                        str = "Defensive Fire";
                    else
                        if ((cur_atk.sel_prop.flags & UnitFlags.AIR_DEFENSE) == UnitFlags.AIR_DEFENSE)
                            str = "Air-Defense";
                        else
                            str = "Interceptors";
#if TODO
            label_write( gui.label, gui.font_error, str );
#endif
                }
            }
            else
            {
                /* clear info */
#if TODO
                if (!blind_cpu_turn)
                    label_hide(gui.label, 1);
#endif
                /* normal attack */
                cur_atk = cur_unit;
                cur_def = cur_target;
                fight = true;
                defFire = 0;
            }
            return fight;
        }



        /// <summary>
        /// CreateAction engine (load resources that are not modified by Scenario)
        /// </summary>
#if TODO_RR
        public static int engine_create()
        {
            Slots.slots_init();
#if TODO
			    gui_load( "default" );
#endif
            return 1;
        }
#endif
        /// <summary>
        /// Destroy the engine
        /// </summary>
        public static void engine_delete()
        {
            engine_shutdown();
#if TODO
			    scen_clear_setup();
			    gui_delete();
#endif
        }
#if TODO_RR
        private static IGuiSystem form;
#endif

        /// <summary>
        /// Initiate engine by loading Scenario either as saved game or
        /// new Scenario by the global 'setup'.
        /// </summary>

        public static int engine_init(String scen_name)
        {
			
		
            /* engine */
            /* tile mask */
            /*  1 = map tile directly hit
                0 = neighbor */
            /*
            hex_mask = new int[terrain.hex_w, terrain.hex_h];
            for (int j = 0; j < terrain.hex_h; j++)
                for (int i = 0; i < terrain.hex_w; i++)
                    if (SDL_Surface.GetPixel(terrain.terrainIcons.fog, i, j) != 0)
                        hex_mask[i, j] = 1;

            cur_player = Player.players_get_first();
            map.map_set_spot_mask();
            return 1;
            */
            Player player;

#if USE_DL
    char path[256];
#endif
            end_scen = false;
			
		
            /* Scenario&campaign or title*/
            if (setup.type == SETUP.SETUP_RUN_TITLE)
            {
                status = STATUS.STATUS_TITLE;
                return 1;
            }
            if (setup.type == SETUP.SETUP_CAMP_BRIEFING)
            {
                status = STATUS.STATUS_CAMP_BRIEFING;
                return 1;
            }
            else
                if (setup.type == SETUP.SETUP_INIT_CAMP)
                {
#if TODO_RR
                    if (campaign.camp_load(setup.fname) == 0) return 0;
                    campaign.camp_set_cur(setup.scen_state);
                    if (campaign.camp_cur_scen == null) return 0;
                    setup.type = SETUP.SETUP_CAMP_BRIEFING;
                    return 1;
#endif
                }
                else
                {
					setup.fname = scen_name;
                    if (!Scenario.scen_load(setup.fname)) return 0;
                    if (setup.type == SETUP.SETUP_INIT_SCEN)
                    {
                        /* player control */
                        for (int i = 0; i < setup.player_count; i++)
                        {
                            player = Player.players[i];
                            player.ctrl = setup.ctrl[i];
                            player.ai_fname = setup.modules[i];
                        }
                    }
                    /* select first player */
                    cur_player = Player.players_get_first();
                }
		
            /* store current settings to setup */
            Scenario.scen_set_setup();
		
            /* load the ai modules */
            for (int i = 0; i < Player.players.Count; i++)
            {
                player = Player.players[i];
                /* clear callbacks */
                player.ai_init = null;
                player.ai_run = null;
                player.ai_finalize = null;
                if (player.ctrl == PLAYERCONTROL.PLAYER_CTRL_CPU)
                {
#if USE_DL
            if ( strcmp( "default", player.ai_fname ) ) {
                sprintf( path, "%s/ai_modules/%s", get_gamedir(), player.ai_fname );
                if ( ( player.ai_mod_handle = dlopen( path, RTLD_GLOBAL | RTLD_NOW ) ) == 0 )
                    fprintf( stderr, "%s\n", dlerror() );
                else {
                    if ( ( player.ai_init = dlsym( player.ai_mod_handle, "ai_init" ) ) == 0 )
                        fprintf( stderr, "%s\n", dlerror() );
                    if ( ( player.ai_run = dlsym( player.ai_mod_handle, "ai_run" ) ) == 0 )
                        fprintf( stderr, "%s\n", dlerror() );
                    if ( ( player.ai_finalize = dlsym( player.ai_mod_handle, "ai_finalize" ) ) == 0 )
                        fprintf( stderr, "%s\n", dlerror() );
                }
                if ( player.ai_init == 0 || player.ai_run == 0 || player.ai_finalize == 0 ) {
                    fprintf( stderr, tr("%s: AI module '%s' invalid. Use built-in AI.\n"), player.name, player.ai_fname );
                    /* use the internal AI */
                    player.ai_init = ai_init;
                    player.ai_run = ai_run;
                    player.ai_finalize = ai_finalize;
                    if ( player.ai_mod_handle ) {
                        dlclose( player.ai_mod_handle ); 
                        player.ai_mod_handle = 0;
                    }
                }
            }
            else {
                player.ai_init = ai_init;
                player.ai_run = ai_run;
                player.ai_finalize = ai_finalize;
            }
#else
#if TODO_RR
                    player.ai_init = new AiInit(AI.ai_init);
                    player.ai_run = new AiRun(AI.ai_run);
                    player.ai_finalize = new AiFinalize(AI.ai_finalize);
#endif
#endif
                }
            }

            /* no unit selected */
            cur_unit = cur_target = cur_atk = cur_def = move_unit = surp_unit = deploy_unit = surrender_unit = null;
            df_units = new List<Unit>();
            /* engine */
            /* tile mask */
            /*  1 = map tile directly hit
                0 = neighbor */
            hex_mask = new int[Config.hex_w, Config.hex_h];

            for (int j = 0; j < Config.hex_h; j++)
                for (int i = 0; i < Config.hex_w; i++)
                    if (SDL_Surface.GetPixel(terrain.terrainIcons.fog, i, j) != Color.black)
                        hex_mask[i, j] = 1;
            /* screen copy buffer */
#if TODO
            sc_buffer = SDL_Surface.create_surf(sdl.screen.w, sdl.screen.h, SDL_SWSURFACE);
            sc_type = 0;
#endif
            /* map geometry */
            map_x = map_y = 0;
            map_sx = -Config.hex_x_offset;
            map_sy = -Config.hex_h;
#if TODO
            for (int i = map_sx, map_sw = 0; i < sdl.screen.w; i += terrain.hex_x_offset)
                map_sw++;
            for (int j = map_sy, map_sh = 0; j < sdl.screen.h; j += terrain.hex_h)
                map_sh++;
            /* reset scroll delay */
            set_delay(&scroll_delay, 0);
            scroll_block = 0;
            /* message delay */
            set_delay(&msg_delay, 1500 / config.anim_speed);
            /* hide animations */
            anim_hide(terrain_icons.cross, 1);
            anim_hide(terrain_icons.expl1, 1);
            anim_hide(terrain_icons.expl2, 1);
#endif
            /* remaining deploy units list */
            left_deploy_units = new List<Unit>();
            /* build strategic map */
#if TODO
            strat_map.strat_map_create();
#endif

            /* clear status */
            status = STATUS.STATUS_NONE;
	#if TODO_RR
            stateMachine = new EngineStateMachine();
	#endif
            /* weather */
            Scenario.cur_weather = Scenario.scen_get_weather();
		
            return 1;
        }


        /// <summary>
        /// Shutdown engine
        /// </summary>
        public static void engine_shutdown()
        {
        }

        /// <summary>
        /// Run the engine (starts with the title screen)
        /// </summary>
        public static void engine_run(bool fog)
        {
            bool reinit = true;
			Config.fog_of_war = fog;
            if (setup.type == SETUP.SETUP_UNKNOWN)
                setup.type = SETUP.SETUP_RUN_TITLE;
#if TODO
            while (true)
            {
                while (reinit)
                {
                    reinit = false;
                    if (engine_init() == 0)
                    {
                        /* if engine initialisation is unsuccesful */
                        /* stay with the title screen */
                        status = STATUS.STATUS_TITLE;
                        setup.type = SETUP.SETUP_RUN_TITLE;
                    }
                    if (scen.turn == 0 && camp.camp_loaded && setup.type == SETUP.SETUP_CAMP_BRIEFING)
                        engine_prep_camp_brief();
                    engine_main_loop(ref reinit);
                    if (term_game) break;
                    engine_shutdown();
                }
                
                if (scen.scen_done())
                {
                    if (camp.camp_loaded)
                    {
                        engine_store_debriefing(scen.scen_get_result());
                        /* determine next scenario in campaign */
                        if (!camp.camp_set_next(scen.scen_get_result()))
                            break;
                        if (camp.camp_cur_scen.nexts == null)
                        {
                            /* final message */
                            setup.type = SETUP.SETUP_CAMP_BRIEFING;
                            reinit = true;
                        }
                        else if (camp.camp_cur_scen.scen != null)
                        { /* options */
                            setup.type = SETUP.SETUP_CAMP_BRIEFING;
                            reinit = true;
                        }
                        else
                        {
                            /* next scenario */
                            setup.fname = camp.camp_cur_scen.scen;
                            setup.type = SETUP.SETUP_CAMP_BRIEFING;
                            reinit = true;
                        }
                    }
                    else
                    {
                        setup.type = SETUP.SETUP_RUN_TITLE;
                        reinit = true;
                    }
                }
                else
                    break;
                /* clear result before next loop (if any) */
                scen.scen_clear_result();
            }
#endif
        }

        /*
        ====================================================================
        Main game loop.
        If a restart is nescessary 'setup' is modified and 'reinit'
        is set True.
        ====================================================================
        */
#if TODO_RR
        static void engine_main_loop(ref bool reinit)
        {
            int ms;
            if (status == STATUS.STATUS_TITLE && !term_game)
            {
                engine_draw_bkgnd();
                engine_show_game_menu(10, 10);
                refresh_screen(0, 0, 0, 0);
            }
            else if (status == STATUS.STATUS_CAMP_BRIEFING)
                ;
            else
                engine_begin_turn(cur_player, setup.type == SETUP.SETUP_LOAD_GAME /* skip unit preps then */ );
#if TODO
            gui_get_bkgnds();
#endif
            reinit = false;
            Engine.engine_init(null);

#if TODO
            reset_timer();
            while (!end_scen && !term_game)
            {
                engine_begin_frame();
                /* check input/cpu events and put to action queue */
                engine_check_events(reinit);
                /* handle queued actions */
                engine_handle_next_action(reinit);
                /* get time */
                ms = get_time();
                /* delay next scroll step */
                if (scroll_vert || scroll_hori)
                {
                    if (scroll_time > ms)
                    {
                        set_delay(&scroll_delay, scroll_time);
                        scroll_block = 1;
                        scroll_vert = scroll_hori = SCROLL_NONE;
                    }
                    else
                        set_delay(&scroll_delay, 0);
                }
                if (timed_out(&scroll_delay, ms))
                    scroll_block = 0;
                /* update */
                engine_update(ms);
                engine_end_frame();
                /* short delay */
                SDL_Delay(5);
            }
            /* hide these windows, so the initial screen looks as original */
            frame_hide(gui.qinfo1, 1);
            frame_hide(gui.qinfo2, 1);
            label_hide(gui.label, 1);
            label_hide( gui.label2, 1 );
#endif
        }
#endif
        public static void engine_show_game_menu(int cx, int cy)
        {
			throw new NotImplementedException();
        }

#if TODO_RR
        public static void engine_update(int ms)
        {
            if (stateMachine != null)
            {
                stateMachine.Execute();
                Console.WriteLine("Game state: " + stateMachine.CurrentStateID);
            }
        }
#endif
        enum DrawStage { DrawTerrain, DrawUnits, DrawDangerZone };


        private static void list_transfer(List<Unit> src, List<Unit> dest, Unit item)
        {
            dest.Add(item);
            if (src.Contains(item))
                src.Remove(item);
        }

        /*
        ====================================================================
        Display final Scenario message (called when scen_check_result()
        returns True).
        ====================================================================
        */
        static void engine_show_final_message()
        {
#if TODO
            event_wait_until_no_input();
            SDL_FillRect(sdl.screen, 0, 0x0);
            gui.font_turn_info.align = ALIGN_X_CENTER | ALIGN_Y_CENTER;
            write_text(gui.font_turn_info, sdl.screen, sdl.screen.w / 2, sdl.screen.h / 2, scen_get_result_message(), 255);
            refresh_screen(0, 0, 0, 0);
            while (!engine_status_screen_dismissed())
            {
                SDL_PumpEvents();
                SDL_Delay(20);
            }
            event_clear();
#endif
        }

        /*
        ====================================================================
        Update the unit quick info and map tile info if map tile mx,my,
        region has the focus. Also update the cursor.
        ====================================================================
        */
        public static void engine_update_info(int mx, int my, REGION region)
        {
        }

        /*
        ====================================================================
        Unit is completely suppressed so check if it
          does nothing
          tries to move to a neighbored tile
          surrenders because it can't move away
        ====================================================================
        */
        enum UNIT_END_COMBAT
        {
            UNIT_STAYS = 0,
            UNIT_FLEES,
            UNIT_SURRENDERS
        };
#if TODO_RR
        static void engine_handle_suppr(Unit unit, out UNIT_END_COMBAT type, out int x, out int y)
        {
            int i, nx, ny;
            type = UNIT_END_COMBAT.UNIT_STAYS;
            x = 0;
            y = 0;
            if (unit.sel_prop.mov == 0) return;
            /* 80% chance that unit wants to flee */
            if (Misc.DICE(10) <= 8)
            {
                unit.cur_mov = 1;
                map.map_get_unit_move_mask(unit);
                /* get best close hex. if none: surrender */
                for (i = 0; i < 6; i++)
                    if (Misc.get_close_hex_pos(unit.x, unit.y, i, out nx, out ny))
                        if (map.mask[nx, ny].in_range != 0 && !map.mask[nx, ny].blocked)
                        {
                            type = UNIT_END_COMBAT.UNIT_FLEES;
                            x = nx; y = ny;
                            return;
                        }
                /* surrender! */
                type = UNIT_END_COMBAT.UNIT_SURRENDERS;
            }
        }
#endif
        /*
        ====================================================================
        Check if unit stays on top of an enemy flag and capture
        it. Return True if the flag was captured.
        ====================================================================
        */
#if TODO_RR
        public static bool engine_capture_flag(Unit unit)
        {
            if ((unit.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING)
                if ((unit.sel_prop.flags & UnitFlags.SWIMMING) != UnitFlags.SWIMMING)
                    if (Engine.map.map[unit.x, unit.y].nation != null)
                        if (!Player.player_is_ally(Engine.map.map[unit.x, unit.y].player, unit.player))
                        {
                            /* capture */
                            Engine.map.map[unit.x, unit.y].nation = unit.nation;
                            Engine.map.map[unit.x, unit.y].player = unit.player;
                            /* a conquered flag gets deploy ability again after some turns */
                            Engine.map.map[unit.x, unit.y].deploy_center = 3;
                            return true;
                        }
            return false;
        }
#endif
        /* functions needed for movement and combat phase */
#if TODO_RR
        private static void SendTimerEvent()
        {
            stateMachine.operation.Post(delegate
            {
                stateMachine.Send(EngineActionsTypes.TimerElapsed);
                stateMachine.Execute();
            }, null);
        }

#endif
#if TODO_RR
        public static void InitMove()
        {
            Console.WriteLine("InitMove State : {0}", stateMachine.CurrentStateID);
            if (move_unit.name.Contains("21"))
                Console.WriteLine("para");
            /* get move mask */
            map.map_get_unit_move_mask(move_unit);
            /* check if tile is in reach */
            if (map.mask[dest_x, dest_y].in_range == 0)
            {
                Console.WriteLine("{0},{1} out of reach for '{2}'\n", dest_x, dest_y, move_unit.name);
                Action.action_queue(EngineActionsTypes.ACTION_END_MOVE);
                //stateMachine.Send(EngineActionsTypes.ACTION_END_MOVE);
                return;
            }
            if (map.mask[dest_x, dest_y].blocked)
            {
                Console.WriteLine("{0},{1} is blocked ('{2}' wants to move there)\n", dest_x, dest_y, move_unit.name);
                Action.action_queue(EngineActionsTypes.ACTION_END_MOVE);
                //stateMachine.Send(EngineActionsTypes.ACTION_END_MOVE);
                return;
            }
            way = map.map_get_unit_way_points(move_unit, dest_x, dest_y, out way_length, out surp_unit);
            if (way == null)
            {
                Console.WriteLine("There is no way for unit '{0}' to move to {1},{2}\n",
                         move_unit.name, dest_x, dest_y);
                Action.action_queue(EngineActionsTypes.ACTION_END_MOVE);
                //stateMachine.Send(EngineActionsTypes.ACTION_END_MOVE);
                return;
            }
            /* remove unit influence */
            if (!Player.player_is_ally(move_unit.player, cur_player))
                map.map_remove_unit_infl(move_unit);
            /* backup the unit but only if this is not a fleeing unit! */
            if (fleeing_unit != 0)
                fleeing_unit = 0;
            else
                engine_backup_move(move_unit, dest_x, dest_y);
            /* if ground transporter needed mount unit */
            if (map.mask[dest_x, dest_y].mount != 0)
                move_unit.Mount();
            /* start at first way point */
            way_pos = 0;
            /* unit's used */
            move_unit.unused = false;
            /* artillery looses ability to attack */
            if ((move_unit.sel_prop.flags & UnitFlags.ATTACK_FIRST) == UnitFlags.ATTACK_FIRST)
                move_unit.cur_atk_count = 0;
            /* decrease moving points */
            /*                    if ( ( move_unit.sel_prop.flags & RECON ) && surp_unit == 0 )
                                    move_unit.cur_mov = mask[dest_x, dest_y].in_range - 1;
                                else*/
            move_unit.cur_mov = 0;
            if (move_unit.cur_mov < 0)
                move_unit.cur_mov = 0;
            /* no entrenchment */
            move_unit.entr = 0;
            /* build up the image */
            if (!blind_cpu_turn)
            {
                move_image = move_unit.sel_prop.icon;
#if TODO
                if (map.mask[move_unit.x, move_unit.y].fog)
                    image_hide(move_image, 1);
#endif
            }
            /* remove unit from map */
            //map.map_remove_unit(move_unit);
            int start_x, start_y;
            if (!blind_cpu_turn)
            {
                engine_get_screen_pos(move_unit.x, move_unit.y, out start_x, out start_y);
                start_x += ((terrain.hex_w - move_unit.sel_prop.icon_w) >> 1);
                start_y += ((terrain.hex_h - move_unit.sel_prop.icon_h) >> 1);
#if TODO
                image_move(move_image, start_x, start_y);
#endif
                draw_map = true;
            }
            /* animate */
            phase = PHASE.PHASE_START_SINGLE_MOVE;
            Action.action_queue(EngineActionsTypes.ACTION_START_SINGLE_MOVE);
            //stateMachine.Send(EngineActionsTypes.ACTION_START_SINGLE_MOVE);
            //remove stateMachine.scheduler.Start();

            /* play sound */
#if WITH_SOUND   
                    if ( !mask[move_unit.x, move_unit.y].fog )
                        wav_play( move_unit.sel_prop.wav_move );
#endif
            /* since it moves it is no longer assumed to be a guard */
            move_unit.is_guarding = false;
        }
#endif
#if TODO_RR
        public static void SingleMove()
        {
            Console.WriteLine("SingleMove State : {0}", stateMachine.CurrentStateID);

            bool enemy_spotted;
            int i;
            int start_x, start_y, end_x, end_y;

            map.map_remove_unit(move_unit);
            /* get next start way point */
            if (blind_cpu_turn)
            {
                way_pos = way_length - 1;
                /* quick move unit */
                for (i = 1; i < way_length; i++)
                {
                    move_unit.x = way[i].x; move_unit.y = way[i].y;
                    map.map_update_spot_mask(move_unit, out enemy_spotted);
                }
            }
            else
                if (!modify_fog)
                {
                    i = way_pos;
                    while (i + 1 < way_length &&
                        map.mask[way[i].x, way[i].y].fog &&
                        map.mask[way[i + 1].x, way[i + 1].y].fog)
                    {
                        i++;
                        /* quick move unit */
                        move_unit.x = way[i].x; move_unit.y = way[i].y;
                        map.map_update_spot_mask(move_unit, out enemy_spotted);
                    }
                    way_pos = i;
                }
            /* focus current way point */
            if (way_pos < way_length - 1)
                if (!blind_cpu_turn && (map.MAP_CHECK_VIS(way[way_pos].x, way[way_pos].y) ||
                    map.MAP_CHECK_VIS(way[way_pos + 1].x, way[way_pos + 1].y)))
                {
                    if (engine_focus(way[way_pos].x, way[way_pos].y, true))
                    {
                        engine_get_screen_pos(way[way_pos].x, way[way_pos].y, out start_x, out start_y);
                        start_x += ((terrain.hex_w - move_unit.sel_prop.icon_w) >> 1);
                        start_y += ((terrain.hex_h - move_unit.sel_prop.icon_h) >> 1);
#if TODO
                        image_move(move_image, start_x, start_y);
#endif
                    }

                }
            /* units looking direction */
            move_unit.AdjustOrient(way[way_pos].x, way[way_pos].y);
#if TODO
            if (!blind_cpu_turn)
                image_set_region(move_image, move_unit.icon_offset, 0,
                                  move_unit.sel_prop.icon_w, move_unit.sel_prop.icon_h);
#endif
            /* units position */
            move_unit.x = way[way_pos].x; move_unit.y = way[way_pos].y;
            map.map_insert_unit(move_unit);
            /* update spotting */
            map.map_update_spot_mask(move_unit, out enemy_spotted);
            if (modify_fog)
                map.map_set_fog(MAP_MASK.F_SPOT);
            if (enemy_spotted)
            {
                /* if you spotted an enemy it's not allowed to undo the turn */
                engine_clear_backup();
            }
            /* determine next step */
            if (way_pos == way_length - 1)
            {
                phase = PHASE.PHASE_CHECK_LAST_MOVE;
                Action.action_queue(EngineActionsTypes.ACTION_CHECK_LAST_MOVE);
                //stateMachine.Send(EngineActionsTypes.ACTION_CHECK_LAST_MOVE);
            }
            else
            {
                /* animate? */
                if (map.MAP_CHECK_VIS(way[way_pos].x, way[way_pos].y) ||
                    map.MAP_CHECK_VIS(way[way_pos + 1].x, way[way_pos + 1].y))
                {
                    engine_get_screen_pos(way[way_pos].x, way[way_pos].y, out start_x, out start_y);
                    start_x += ((terrain.hex_w - move_unit.sel_prop.icon_w) >> 1);
                    start_y += ((terrain.hex_h - move_unit.sel_prop.icon_h) >> 1);
                    engine_get_screen_pos(way[way_pos + 1].x, way[way_pos + 1].y, out end_x, out end_y);
                    end_x += ((terrain.hex_w - move_unit.sel_prop.icon_w) >> 1);
                    end_y += ((terrain.hex_h - move_unit.sel_prop.icon_h) >> 1);
                    unit_vector.x = start_x; unit_vector.y = start_y;
                    move_vector.x = end_x - start_x; move_vector.y = end_y - start_y;
                    float len = (float)Math.Sqrt(move_vector.x * move_vector.x + move_vector.y * move_vector.y);
                    move_vector.x /= len;
                    move_vector.y /= len;
#if TODO
                    image_move(move_image, (int)unit_vector.x, (int)unit_vector.y);
                    set_delay(&move_time, ((int)(len / move_vel)) / config.anim_speed);
#endif
                    draw_map = true;
                    form.Draw();
                }
#if TODO
                else
                    set_delay(&move_time, 0);
#endif
                phase = PHASE.PHASE_RUN_SINGLE_MOVE;
                Action.action_queue(EngineActionsTypes.ACTION_RUN_SINGLE_MOVE);
                //stateMachine.Send(EngineActionsTypes.ACTION_RUN_SINGLE_MOVE);
                //remove stateMachine.scheduler.Add(1, Config.schedulerTimeOut, new EngineStateMachine.SendTimerDelegate(SendTimerEvent));

#if TODO
                image_hide(move_image, 0);
#endif
            }

        }
#endif
#if TODO_RR
        public static void RunMove()
        {
            Console.WriteLine("State : {0}", stateMachine.CurrentStateID);

            /* next way point */
            way_pos++;
            /* next movement */
            phase = PHASE.PHASE_START_SINGLE_MOVE;
            Action.action_queue(EngineActionsTypes.ACTION_START_SINGLE_MOVE);
            //stateMachine.Send(EngineActionsTypes.ACTION_START_SINGLE_MOVE);
            //remove stateMachine.scheduler.Add(1, Config.schedulerTimeOut, new EngineStateMachine.SendTimerDelegate(SendTimerEvent));
        }
#endif
#if TODO_RR
        public static void CheckLastMove()
        {
            /* insert unit */
            //map.map_insert_unit(move_unit);
            /* capture flag if there is one */
            /* NOTE: only do it for AI. For the human player, it will
             * be done on deselecting the current unit to resemble
             * original Panzer General behaviour
             */
            if (cur_ctrl == PLAYERCONTROL.PLAYER_CTRL_CPU)
            {
                if (engine_capture_flag(move_unit))
                {
                    /* CHECK IF SCENARIO IS FINISHED */
                    if (Scenario.scen_check_result(false))
                    {
                        engine_finish_scenario();
                        return;
                    }
                }
            }
            /* add influence */
            if (!Player.player_is_ally(move_unit.player, cur_player))
                map.map_add_unit_infl(move_unit);
            /* update the visible units list */
            map.map_get_vis_units();
            map.map_set_vis_infl_mask();
            /* next phase */
            phase = PHASE.PHASE_END_MOVE;
            Action.action_queue(EngineActionsTypes.ACTION_END_MOVE);
            //stateMachine.Send(EngineActionsTypes.ACTION_END_MOVE);
        }
#endif
#if TODO_RR
        public static void EndMove()
        {
            /* fade out sound */
#if WITH_SOUND         
                    audio_fade_out( 0, 500 ); /* move sound channel */
#endif
            /* decrease fuel for way_pos hex tiles of movement */
            if (move_unit.CheckFuelUsage() && Config.supply)
            {
                move_unit.cur_fuel -= move_unit.CalcFuelUsage(  way_pos);
                if (move_unit.cur_fuel < 0)
                    move_unit.cur_fuel = 0;
            }
            /* clear move buffer image */
#if TODO
                    if ( !blind_cpu_turn )
                       image_delete( &move_image );
#endif
            /* run surprise contact */
            if (surp_unit != null)
            {
                cur_unit = move_unit;
                cur_target = surp_unit;
                surp_contact = true;
                surp_unit = null;
                if (engine_get_next_combatants())
                {
                    status = STATUS.STATUS_ATTACK;
                    phase = PHASE.PHASE_INIT_ATK;
                    if (!blind_cpu_turn)
                    {
#if TODO
                                image_hide( gui.cursors, 1 );
#endif
                        draw_map = true;
                        form.Draw();
                    }
                }
                return;
            }
            /* reselect unit -- cur_unit may differ from move_unit! */
            if (cur_ctrl == PLAYERCONTROL.PLAYER_CTRL_HUMAN)
                engine_select_unit(cur_unit);
            /* status */
            engine_set_status(STATUS.STATUS_NONE);
            phase = PHASE.PHASE_NONE;
            Action.action_queue(EngineActionsTypes.ACTION_NONE);
            //stateMachine.Send(EngineActionsTypes.ACTION_NONE);

            /* allow new human/cpu input */
            if (!blind_cpu_turn)
            {
                if (cur_ctrl == PLAYERCONTROL.PLAYER_CTRL_HUMAN)
                {
#if TODO
                            gui_set_cursor( CURSOR_STD );
                            image_hide( gui.cursors, 0 );
                            old_mx = old_my = -1;
#endif
                }
                draw_map = true;
                form.Draw();
            }
        }
#endif
#if TODO_RR
        public static void ActionAttack(object[] args)
        {
            Action action = (Action)args[0];
            if (!action.unit.CheckAttack( action.target, Unit.UNIT_ATTACK.UNIT_ACTIVE_ATTACK))
            {
                Console.WriteLine("'{0}' ({1},{2}) can not attack '{3}' ({4},{5})",
                         action.unit.name, action.unit.x, action.unit.y,
                         action.target.name, action.target.x, action.target.y);
                return;
            }
            if (!Engine.map.mask[action.target.x, action.target.y].spot)
            {
                Console.WriteLine("'%{0}' may not attack unit '{1}' (not visible)", action.unit.name, action.target.name);
                return;
            }
            Engine.cur_unit = action.unit;
            Engine.cur_target = action.target;
            Engine.cur_unit.GetDefensiveFireUnits(Engine.cur_target, Scenario.units, ref Engine.df_units);
            if (Engine.engine_get_next_combatants())
            {
                Engine.status = STATUS.STATUS_ATTACK;
                Engine.phase = PHASE.PHASE_INIT_ATK;
                Action.action_queue(EngineActionsTypes.ACTION_INIT_ATTACK);
                //stateMachine.Send(EngineActionsTypes.ACTION_INIT_ATTACK);
                Engine.engine_clear_danger_mask();
#if TODO
                if (Engine.cur_ctrl == PLAYER_CTRL_HUMAN)
                    image_hide(gui.cursors, 1);
#endif
            }
        }
#endif
#if TODO_RR
        public static void InitAttack()
        {
            //remove stateMachine.scheduler.Start();

            if (!blind_cpu_turn)
            {
                if (map.MAP_CHECK_VIS(cur_atk.x, cur_atk.y))
                {
                    int cx, cy;

                    /* show attacker cross */
                    engine_focus(cur_atk.x, cur_atk.y, true);
                    engine_get_screen_pos(cur_atk.x, cur_atk.y, out cx, out cy);
#if TODO
                    anim_move(terrain_icons.cross, cx, cy);
                    anim_play(terrain_icons.cross, 0);
#endif
                }
                phase = PHASE.PHASE_SHOW_ATK_CROSS;
                Action.action_queue(EngineActionsTypes.ACTION_COMBAT);
                //stateMachine.Send(EngineActionsTypes.ACTION_COMBAT);
#if TODO 
                label_hide(gui.label2, 1);
#endif
            }
            else
            {
                phase = PHASE.PHASE_COMBAT;
                Action.action_queue(EngineActionsTypes.ACTION_COMBAT);
                //stateMachine.Send(EngineActionsTypes.ACTION_COMBAT);
            }
            /* both units in a fight are no longer just guarding */
            cur_atk.is_guarding = false;
            cur_def.is_guarding = false;
        }
#endif
#if TODO_RR
        public static void ShowAttackCross()
        {
            /* backup old strength to see who needs and explosion */
            int old_atk_str = cur_atk.str;
            int old_def_str = cur_def.str;
            /* backup old suppression to calculate delta */
            int old_atk_suppr = cur_atk.suppr;
            int old_def_suppr = cur_def.suppr;
            int old_atk_turn_suppr = cur_atk.turn_suppr;
            int old_def_turn_suppr = cur_def.turn_suppr;
            /* take damage */
            if (surp_contact)
                atk_result = cur_atk.SurpriseAttack(  cur_def);
            else
            {
                if (df_units.Count > 0)
                    atk_result = cur_atk.NormalAttack(cur_def, Unit.UNIT_ATTACK.UNIT_DEFENSIVE_ATTACK);
                else
                    atk_result = cur_atk.NormalAttack(cur_def, Unit.UNIT_ATTACK.UNIT_ACTIVE_ATTACK);
            }
            /* calculate deltas */
            atk_damage_delta = old_atk_str - cur_atk.str;
            def_damage_delta = old_def_str - cur_def.str;
            atk_suppr_delta = cur_atk.suppr - old_atk_suppr;
            def_suppr_delta = cur_def.suppr - old_def_suppr;
            atk_suppr_delta += cur_atk.turn_suppr - old_atk_turn_suppr;
            def_suppr_delta += cur_def.turn_suppr - old_def_turn_suppr;
            if (blind_cpu_turn)
                phase = PHASE.PHASE_CHECK_RESULT;
            else
            {
                /* if rugged defense add a pause */
                if ((atk_result & Unit.FIGHT_TYPES.AR_RUGGED_DEFENSE) == Unit.FIGHT_TYPES.AR_RUGGED_DEFENSE)
                {
                    phase = PHASE.PHASE_RUGGED_DEF;
                    if ((cur_def.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
                        Console.WriteLine("Out Of The Sun!");
                    else
                        if ((cur_def.sel_prop.flags & UnitFlags.SWIMMING) == UnitFlags.SWIMMING)
                            Console.WriteLine("Surprise Contact!");
                        else
                            Console.WriteLine("Rugged Defense!");
                }
                else if ((atk_result & Unit.FIGHT_TYPES.AR_EVADED) == Unit.FIGHT_TYPES.AR_EVADED)
                {
                    /* if sub evaded give info */
                    Console.WriteLine("Submarine evades!");
                    phase = PHASE.PHASE_EVASION;
                }
                else
                    phase = PHASE.PHASE_PREP_EXPLOSIONS;
            }
            Action.action_queue(EngineActionsTypes.ACTION_CHECK_RESULT);
            //stateMachine.Send(EngineActionsTypes.ACTION_CHECK_RESULT);
        }
#endif
#if TODO_RR
        public static void CheckResult()
        {
            bool broken_up = false;
            bool reset = false;
            bool was_final_fight = false;
            bool surrender = false;
            try
            {
                UNIT_END_COMBAT type = UNIT_END_COMBAT.UNIT_STAYS;
                int dx, dy;

                surp_contact = false;

                /* check attack result */
                if ((atk_result & Unit.FIGHT_TYPES.AR_UNIT_KILLED) == Unit.FIGHT_TYPES.AR_UNIT_KILLED)
                {
                    Scenario.scen_inc_casualties_for_unit(cur_atk);
                    engine_remove_unit(cur_atk);
                    cur_atk = null;
                }
                if ((atk_result & Unit.FIGHT_TYPES.AR_TARGET_KILLED) == Unit.FIGHT_TYPES.AR_TARGET_KILLED)
                {
                    Scenario.scen_inc_casualties_for_unit(cur_def);
                    engine_remove_unit(cur_def);
                    cur_def = null;
                }
                /* CHECK IF SCENARIO IS FINISHED DUE TO UNITS_KILLED OR UNITS_SAVED */
                if (Scenario.scen_check_result(false))
                {
                    engine_finish_scenario();
                    return;
                }
                reset = true;
                if (df_units.Count > 0)
                {
                    if (((atk_result & Unit.FIGHT_TYPES.AR_TARGET_SUPPRESSED) == Unit.FIGHT_TYPES.AR_TARGET_SUPPRESSED) ||
                        ((atk_result & Unit.FIGHT_TYPES.AR_TARGET_KILLED) == Unit.FIGHT_TYPES.AR_TARGET_KILLED))
                    {
                        df_units.Clear();
                        if ((atk_result & Unit.FIGHT_TYPES.AR_TARGET_KILLED) == Unit.FIGHT_TYPES.AR_TARGET_KILLED)
                            cur_unit = null;
                        else
                        {
                            /* supressed unit looses its actions */
                            cur_unit.cur_mov = 0;
                            cur_unit.cur_atk_count = 0;
                            cur_unit.unused = false;
                            broken_up = true;
                        }
                    }
                    else
                    {
                        reset = false;
                        df_units.RemoveAt(0);
                    }
                }
                else
                    was_final_fight = true;
                if (!reset)
                {
                    /* continue fights */
                    if (engine_get_next_combatants())
                    {
                        status = STATUS.STATUS_ATTACK;
                        phase = PHASE.PHASE_INIT_ATK;
                    }
                    else
                        Console.WriteLine("Deadlock! No remaining combatants but supposed to continue fighting? How is this supposed to work????");
                }
                else
                {
                    /* clear suppression from defensive fire */
                    if (cur_atk != null)
                    {
                        cur_atk.suppr = 0;
                        cur_atk.unused = false;
                    }
                    if (cur_def != null)
                        cur_def.suppr = 0;
                    /* if this was the final fight between selected unit and selected target
                       check if one of these units was completely suppressed and surrenders
                       or flees */
                    if (was_final_fight)
                    {
                        engine_clear_backup(); /* no undo allowed after attack */
                        if (cur_atk != null && cur_def != null)
                        {
                            if ((atk_result & Unit.FIGHT_TYPES.AR_UNIT_ATTACK_BROKEN_UP) == Unit.FIGHT_TYPES.AR_UNIT_ATTACK_BROKEN_UP)
                            {
                                /* unit broke up the attack */
                                broken_up = true;
                            }
                            else
                                /* total suppression may only cause fleeing or 
                                   surrender if: both units are ground/naval units in
                                   close combat: the unit that causes suppr must
                                   have range 0 (melee)
                                   inf . fort (fort may surrender)
                                   fort . adjacent inf (inf will not flee) */
                                if (((cur_atk.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING) &&
                                    (cur_def.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING)
                                {
                                    if ((atk_result & Unit.FIGHT_TYPES.AR_UNIT_SUPPRESSED) == Unit.FIGHT_TYPES.AR_UNIT_SUPPRESSED &&
                                         ((atk_result & Unit.FIGHT_TYPES.AR_TARGET_SUPPRESSED) != Unit.FIGHT_TYPES.AR_TARGET_SUPPRESSED) &&
                                         cur_def.sel_prop.rng == 0)
                                    {
                                        /* cur_unit is suppressed */
                                        engine_handle_suppr(cur_atk, out type, out dx, out dy);
                                        if (type == UNIT_END_COMBAT.UNIT_FLEES)
                                        {
                                            status = STATUS.STATUS_MOVE;
                                            phase = PHASE.PHASE_INIT_MOVE;
                                            move_unit = cur_atk;
                                            fleeing_unit = 1;
                                            dest_x = dx; dest_y = dy;
                                            return;
                                        }
                                        else
                                            if (type == UNIT_END_COMBAT.UNIT_SURRENDERS)
                                            {
                                                surrender = true;
                                                surrender_unit = cur_atk;
                                            }
                                    }
                                    else
                                        if ((atk_result & Unit.FIGHT_TYPES.AR_TARGET_SUPPRESSED) == Unit.FIGHT_TYPES.AR_TARGET_SUPPRESSED &&
                                             (atk_result & Unit.FIGHT_TYPES.AR_UNIT_SUPPRESSED) != Unit.FIGHT_TYPES.AR_UNIT_SUPPRESSED &&
                                             cur_atk.sel_prop.rng == 0)
                                        {
                                            /* cur_target is suppressed */
                                            engine_handle_suppr(cur_def, out type, out dx, out dy);
                                            if (type == UNIT_END_COMBAT.UNIT_FLEES)
                                            {
                                                status = STATUS.STATUS_MOVE;
                                                phase = PHASE.PHASE_INIT_MOVE;
                                                move_unit = cur_def;
                                                fleeing_unit = 1;
                                                dest_x = dx; dest_y = dy;
                                                return;
                                            }
                                            else
                                                if (type == UNIT_END_COMBAT.UNIT_SURRENDERS)
                                                {
                                                    surrender = true;
                                                    surrender_unit = cur_def;
                                                }
                                        }
                                }
                        }
                        /* clear pointers */
                        if (cur_atk == null) cur_unit = null;
                        if (cur_def == null) cur_target = null;
                    }
                    if (broken_up)
                    {
                        phase = PHASE.PHASE_BROKEN_UP_MSG;
                        Console.WriteLine("Attack Broken Up!");
                        return;
                    }
                    if (surrender)
                    {
                        string msg = (surrender_unit.sel_prop.flags & UnitFlags.SWIMMING) == UnitFlags.SWIMMING ?
                            "Ship is scuttled!" : "Surrenders!";
                        phase = PHASE.PHASE_SURRENDER_MSG;
                        Console.WriteLine(msg);
                        return;
                    }
                    phase = PHASE.PHASE_END_COMBAT;
                }
            }
            finally
            {
                Action.action_queue(EngineActionsTypes.ACTION_END_COMBAT);
                //stateMachine.Send(EngineActionsTypes.ACTION_END_COMBAT);
            }
        }
#endif
#if TODO_RR
        public static void EndCombat()
        {
#if WITH_SOUND                
            audio_fade_out( 2, 1500 ); /* explosion sound channel */
#endif
            /* costs one fuel point for attacker */
            if (cur_unit != null && cur_unit.CheckFuelUsage() && cur_unit.cur_fuel > 0)
                cur_unit.cur_fuel--;
            /* update the visible units list */
            map.map_get_vis_units();
            map.map_set_vis_infl_mask();
            /* reselect unit */
            if (cur_ctrl == PLAYERCONTROL.PLAYER_CTRL_HUMAN)
                engine_select_unit(cur_unit);
            /* status */
            engine_set_status(STATUS.STATUS_NONE);
#if TODO
            label_hide(gui.label2, 1);
#endif
            phase = PHASE.PHASE_NONE;
            /* allow new human/cpu input */
            if (!blind_cpu_turn)
            {
                if (cur_ctrl == PLAYERCONTROL.PLAYER_CTRL_HUMAN)
                {
#if TODO
                    image_hide(gui.cursors, 0);
                    gui_set_cursor(CURSOR_STD);
#endif
                }
                draw_map = true;
            }
            Action.action_queue(EngineActionsTypes.ACTION_NONE);
            //stateMachine.Send(EngineActionsTypes.ACTION_NONE);
            //remove stateMachine.scheduler.Stop();
        }
#endif
        /*
        ====================================================================
        Show Scenario info window.
        ====================================================================
        */
#if TODO_RR
        public static void GuiShowScenInfo()
        {
            StringBuilder str = new StringBuilder();
            /* title */
            str.Append(Scenario.scen_info.name + "\n");
            /* desc */
            int len = 0;
            while (len < Scenario.scen_info.desc.Length - 40)
            {
                str.Append(Scenario.scen_info.desc.Substring(len, 40) + "\n");
                len += 40;
            }
            if (len < Scenario.scen_info.desc.Length)
                str.Append(Scenario.scen_info.desc.Substring(len) + "\n\n");
            else
                str.Append("\n\n");

            /* turn and date */
            str.Append(Scenario.scen_get_date() + "\n");
            if (Scenario.turn + 1 < Scenario.scen_info.turn_limit)
                str.Append("Turns Left:" + (Scenario.scen_info.turn_limit - Scenario.turn) + "\n");
            else
                str.Append("Turns Left: Last Turn\n");
            /* Scenario result at the end */
            if (Scenario.turn + 1 > Scenario.scen_info.turn_limit)
            {
                str.Append("Result: " + Scenario.scen_message + "\n");
            }
            /* players */
            if (Engine.cur_player != null)
            {
                str.Append("Current Player: " + Engine.cur_player.name + "\n");
                if (Player.players_test_next() != null)
                {
                    str.Append("Next Player:     " + Player.players_test_next().name + "\n");
                }
            }
            /* weather */
            str.Append("Weather:  " +
                    ((Scenario.turn < Scenario.scen_info.turn_limit) ?
                     terrain.weatherTypes[Scenario.scen_get_weather()].name : "n/a") + "\n");
            str.Append("Forecast: " +
                    ((Scenario.turn + 1 < Scenario.scen_info.turn_limit) ?
                     terrain.weatherTypes[Scenario.scen_get_forecast()].name : "n/a") + "\n");
            /* show */
            form.ShowMessageAndWait(str.ToString());
        }
#endif
        /*
        ====================================================================
        Show explicit victory conditions and use Scenario info window for
        this.
        ====================================================================
        */
#if TODO_RR
        public static void gui_render_subcond(VSubCond cond, StringBuilder str)
        {
            switch (cond.type)
            {
                case VSUBCOND.VSUBCOND_TURNS_LEFT:
                    str.Append(cond.count + " turns remaining\n");
                    break;
                case VSUBCOND.VSUBCOND_CTRL_ALL_HEXES:
                    str.Append(" control all victory hexes\n");
                    break;
                case VSUBCOND.VSUBCOND_CTRL_HEX:
                    str.Append(" control hex " + cond.x + ", " + cond.y + "\n");
                    break;
                case VSUBCOND.VSUBCOND_CTRL_HEX_NUM:
                    str.Append(" control at least " + cond.count + " vic hexes\n");
                    break;
                case VSUBCOND.VSUBCOND_UNITS_KILLED:
                    str.Append(" kill units with tag '" + cond.tag + "'\n");
                    break;
                case VSUBCOND.VSUBCOND_UNITS_SAVED:
                    str.Append(" save units with tag '" + cond.tag + "'\n");
                    break;
            }
        }
#endif
#if TODO_RR
        public static void gui_show_conds()
        {
            StringBuilder str = new StringBuilder();
            /* title */
            str.Append("Explicit Victory Conds (" +
                     ((Scenario.vcond_check_type == VCOND_CHECK.VCOND_CHECK_EVERY_TURN) ? "every turn" : "last turn") + ")\n");
            for (int i = 1; i < Scenario.vcond_count; i++)
            {
                str.Append("'" + Scenario.vconds[i].message + "':\n");
                for (int j = 0; j < Scenario.vconds[i].sub_and_count; j++)
                {
                    if (Scenario.vconds[i].subconds_and[j].player != null)
                        str.Append("AND " + Scenario.vconds[i].subconds_and[j].player.name);
                    else
                        str.Append("AND -- ");
                    gui_render_subcond(Scenario.vconds[i].subconds_and[j], str);
                }
                for (int j = 0; j < Scenario.vconds[i].sub_or_count; j++)
                {
                    if (Scenario.vconds[i].subconds_or[j].player != null)
                        str.Append("OR %.2s " + Scenario.vconds[i].subconds_or[j].player.name);
                    else
                        str.Append("OR -- ");
                    gui_render_subcond(Scenario.vconds[i].subconds_or[j], str);
                }
            }
            /* else condition */
            str.Append("else: '" + Scenario.vconds[0].message + "'\n");
            /* show */
            form.ShowMessageAndWait(str.ToString());
        }
#endif
    }
}

