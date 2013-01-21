using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EngineA;
using Miscellaneous;
using DataFile;

namespace AI_Enemy
{
    public enum AI_STATUS
    {
        AI_STATUS_INIT = 0, /* initiate turn */
        AI_STATUS_FINALIZE, /* finalize turn */
        AI_STATUS_DEPLOY,   /* deploy new units */
        AI_STATUS_SUPPLY,   /* supply units that need it */
        AI_STATUS_MERGE,    /* merge damaged units */
        AI_STATUS_GROUP,    /* group and handle other units */
        AI_STATUS_END       /* end ai turn */
    }

    public class AI
    {
        /*
====================================================================
Internal stuff
====================================================================
*/
        static AI_STATUS ai_status = AI_STATUS.AI_STATUS_INIT; /* current AI status */
        static List<Unit> ai_units; /* units that must be processed */
        static AI_Group ai_group; /* for now it's only one group */
        static bool finalized = true; /* set to true when finalized */

        static IEnumerator<Unit> avail_unitsIterator;
        static IEnumerator<Unit> ai_unitsIterator;
        /*
        ====================================================================
        Locals
        ====================================================================
        */


        /*
        ====================================================================
        Get the distance of 'unit' and position of object of a special
        type.
        ====================================================================
        */
        static bool ai_check_hex_type(Unit unit, AI_FIND type, int x, int y)
        {
            switch (type)
            {
                case AI_FIND.AI_FIND_DEPOT:
                    if (Engine.map.map_is_allied_depot(Engine.map.map[x, y], unit))
                        return true;
                    break;
                case AI_FIND.AI_FIND_ENEMY_OBJ:
                    if (!Engine.map.map[x, y].obj) return false;
                    break;
                case AI_FIND.AI_FIND_ENEMY_TOWN:
                    if (Engine.map.map[x, y].player != null && !Player.player_is_ally(unit.player, Engine.map.map[x, y].player))
                        return true;
                    break;
                case AI_FIND.AI_FIND_OWN_OBJ:
                    if (!Engine.map.map[x, y].obj) return false;
                    break;
                case AI_FIND.AI_FIND_OWN_TOWN:
                    if (Engine.map.map[x, y].player != null && Player.player_is_ally(unit.player, Engine.map.map[x, y].player))
                        return true;
                    break;
            }
            return false;
        }

        public static bool ai_get_dist(Unit unit, int x, int y, AI_FIND type, out int dx, out int dy, out int dist)
        {
            bool found = false;
            int length;
            int i, j;
            dist = 999;
            dx = dy = 0;
            for (i = 0; i < Engine.map.map_w; i++)
                for (j = 0; j < Engine.map.map_h; j++)
                    if (ai_check_hex_type(unit, type, i, j))
                    {
                        length = Misc.get_dist(i, j, x, y);
                        if (dist > length)
                        {
                            dist = length;
                            dx = i;
                            dy = j;
                            found = true;
                        }
                    }
            return found;
        }

        /*
        ====================================================================
        Approximate destination by best move position in range.
        ====================================================================
        */
        static bool ai_approximate(Unit unit, int dx, int dy, out int x, out int y)
        {
            int i, j, dist = Misc.get_dist(unit.x, unit.y, dx, dy) + 1;
            x = unit.x; y = unit.y;
            Engine.map.map_clear_mask(MAP_MASK.F_AUX);
            Engine.map.map_get_unit_move_mask(unit);
            for (i = 0; i < Engine.map.map_w; i++)
                for (j = 0; j < Engine.map.map_h; j++)
                    if (Engine.map.mask[i, j].in_range != 0 && !Engine.map.mask[i, j].blocked)
                        Engine.map.mask[i, j].aux = Misc.get_dist(i, j, dx, dy) + 1;
            for (i = 0; i < Engine.map.map_w; i++)
                for (j = 0; j < Engine.map.map_h; j++)
                    if (dist > Engine.map.mask[i, j].aux && Engine.map.mask[i, j].aux > 0)
                    {
                        dist = Engine.map.mask[i, j].aux;
                        x = i; y = j;
                    }
            return (x != unit.x || y != unit.y);
        }

        /*
        ====================================================================
        Evaluate all units by not only checking the props but also the 
        current values.
        ====================================================================
        */
        static int get_rel(int value, int limit)
        {
            return 1000 * value / limit;
        }
        static void ai_eval_units()
        {
            foreach (Unit unit in Scenario.units)
            {
                if (unit.killed != 0) continue;
                unit.eval_score = 0;
                if (unit.prop.ammo > 0)
                {
                    if (unit.cur_ammo >= 5)
                        /* it's extremly unlikely that there'll be more than
                           five attacks on a unit within one turn so we
                           can consider a unit with 5+ ammo 100% ready for 
                           battle */
                        unit.eval_score += 1000;
                    else
                        unit.eval_score += get_rel(unit.cur_ammo,
                                                     Math.Min(5, unit.prop.ammo));
                }
                if (unit.prop.fuel > 0)
                {
                    if ((((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) && unit.cur_fuel >= 20) ||
                         (((unit.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING) && unit.cur_fuel >= 10))
                        /* a unit with this range is considered 100% operable */
                        unit.eval_score += 1000;
                    else
                    {
                        if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
                            unit.eval_score += get_rel(unit.cur_fuel, Math.Min(20, unit.prop.fuel));
                        else
                            unit.eval_score += get_rel(unit.cur_fuel, Math.Min(10, unit.prop.fuel));
                    }
                }
                unit.eval_score += unit.exp_level * 250;
                unit.eval_score += unit.str * 200; /* strength is counted doubled */
                /* the unit experience is not counted as normal but gives a bonus
                   that may increase the evaluation */
                unit.eval_score /= 2 + (unit.prop.ammo > 0 ? 1 : 0) + (unit.prop.fuel > 0 ? 1 : 0);
                /* this value between 0 and 1000 indicates the readiness of the unit
                   and therefore the permillage of eval_score */
                unit.eval_score = unit.eval_score * unit.prop.eval_score / 1000;
            }
        }

        /*
        ====================================================================
        Set control mask for ground/air/sea for all units. (not only the 
        visible ones) Friends give positive and foes give negative score
        which is a relative the highest control value and ranges between
        -1000 and 1000.
        ====================================================================
        */
        public struct CtrlCtx
        {
            public Unit unit;
            public int score;
            public int op_region; /* 0 - ground, 1 - air, 2 - sea */
        }
        static bool ai_eval_ctrl(int x, int y, object _ctx)
        {
            CtrlCtx ctx = (CtrlCtx)_ctx;
            /* our main function ai_get_ctrl_masks() adds the score
               for all tiles in range and as we only want to add score
               once we have to check only tiles in attack range that
               are not situated in the move range */
            if (Engine.map.mask[x, y].in_range != 0)
                return true;
            /* okay, this is fire range but not move range */
            switch (ctx.op_region)
            {
                case 0: Engine.map.mask[x, y].ctrl_grnd += ctx.score; break;
                case 1: Engine.map.mask[x, y].ctrl_air += ctx.score; break;
                case 2: Engine.map.mask[x, y].ctrl_sea += ctx.score; break;
            }
            return true;
        }
		
#if TODO_RR
        void ai_get_ctrl_masks()
        {
            CtrlCtx ctx;
            int i, j;
            Engine.map.map_clear_mask(MAP_MASK.F_CTRL_GRND | MAP_MASK.F_CTRL_AIR | MAP_MASK.F_CTRL_SEA);
            foreach (Unit unit in Scenario.units)
            {
                if (unit.killed != 0) continue;
                Engine.map.map_get_unit_move_mask(unit);
                /* build context */
                ctx.unit = unit;
                ctx.score = (Player.player_is_ally(unit.player, Engine.cur_player)) ? unit.eval_score : -unit.eval_score;
                ctx.op_region = ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) ? 1 : ((unit.sel_prop.flags & UnitFlags.SWIMMING) == UnitFlags.SWIMMING) ? 2 : 0;
                /* work through move mask and modify ctrl mask by adding score
                   for all tiles in movement and attack range once */
                for (i = Math.Max(0, unit.x - unit.cur_mov);
                      i <= Math.Min(Engine.map.map_w - 1, unit.x + unit.cur_mov);
                      i++)
                    for (j = Math.Max(0, unit.y - unit.cur_mov);
                          j <= Math.Min(Engine.map.map_h - 1, unit.y + unit.cur_mov);
                          j++)
                        if (Engine.map.mask[i, j].in_range != 0)
                        {
                            switch (ctx.op_region)
                            {
                                case 0: Engine.map.mask[i, j].ctrl_grnd += ctx.score; break;
                                case 1: Engine.map.mask[i, j].ctrl_air += ctx.score; break;
                                case 2: Engine.map.mask[i, j].ctrl_sea += ctx.score; break;
                            }
                            ai_eval_hexes(i, j, Math.Max(1, unit.sel_prop.rng),
                                           new eval_func_delegate(ai_eval_ctrl), ctx);
                        }
            }
        }
#endif
        /*
        ====================================================================
        Exports
        ====================================================================
        */

        /*
        ====================================================================
        Initiate turn
        ====================================================================
        */
        public static void ai_init()
        {
            List<Unit> list; /* used to speed up the creation of ai_units */
#if DEBUG
            Console.WriteLine("AI Turn: {0:n}", Engine.cur_player.name);
#endif
            if (ai_status != AI_STATUS.AI_STATUS_INIT)
            {
#if DEBUG
                Console.WriteLine("Aborted: Bad AI Status: {0:n}", ai_status);
#endif
                return;
            }
            finalized = false;
            /* get all cur_player units, those with defensive fire come first */
            list = new List<Unit>();
            foreach (Unit unit in Scenario.units)
                if (unit.player == Engine.cur_player)
                    list.Add(unit);
            ai_units = new List<Unit>();
            for (int index = list.Count - 1; index >= 0; index--)
            {
                Unit unit = list[index];
                if ((unit.sel_prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY ||
                    (unit.sel_prop.flags & UnitFlags.AIR_DEFENSE) == UnitFlags.AIR_DEFENSE)
                {
                    ai_units.Add(unit);
                    list.Remove(unit);
                }
            }
            foreach (Unit unit in list)
            {
#if DEBUG
                if (unit.killed != 0) Console.WriteLine("!!Unit {0} is dead!!", unit.name);
#endif
                ai_units.Add(unit);
            }
            list.Clear();
#if DEBUG
            Console.WriteLine("Units: {0}", ai_units.Count);
#endif
            /* evaluate all units for strategic computations */
#if DEBUG
            Console.WriteLine("Evaluating units...");
#endif
            ai_eval_units();
            /* build control masks */
#if DEBUG
            Console.WriteLine("Building control mask...");
#endif
            //ai_get_ctrl_masks();
            /* check new units first */
            ai_status = AI_STATUS.AI_STATUS_DEPLOY;
            avail_unitsIterator = Scenario.avail_units.GetEnumerator();
#if DEBUG
            Console.WriteLine("AI Initialized");
            Console.WriteLine("*** DEPLOY ***");
#endif
        }

        /*
        ====================================================================
        Queue next actions (if these actions were handled by the engine
        this function is called again and again until the end_turn
        action is received).
        ====================================================================
        */

        public static bool ai_run()
        {
            bool result = false;
            Unit[] partners = new Unit[Map.MAP_MERGE_UNIT_LIMIT];
            int partner_count;
            int i, j, x = 0, y = 0, dx, dy, dist;
            bool found;
            Unit unit = null;
            Unit best;
            switch (ai_status)
            {
                case AI_STATUS.AI_STATUS_DEPLOY:
                    /* deploy unit? */
                    if (Scenario.avail_units.Count > 0 && avail_unitsIterator.MoveNext() && (unit = avail_unitsIterator.Current) != null)
                    {

                        if (Engine.deploy_turn)
                        {
                            x = unit.x; y = unit.y;
                            //assert(x >= 0 && y >= 0);
                            Engine.map.map_remove_unit(unit);
                            found = true;
                        }
                        else
                        {
                            Engine.map.map_get_deploy_mask(Engine.cur_player, unit, false);
                            Engine.map.map_clear_mask(MAP_MASK.F_AUX);
                            for (i = 0; i < Engine.map.map_w; i++)
                                for (j = 0; j < Engine.map.map_h; j++)
                                    if (Engine.map.mask[i, j].deploy)
                                        if (ai_get_dist(unit, i, j, AI_FIND.AI_FIND_ENEMY_OBJ, out x, out y, out dist))
                                            Engine.map.mask[i, j].aux = dist + 1;
                            dist = 1000; found = false;
                            for (i = 0; i < Engine.map.map_w; i++)
                                for (j = 0; j < Engine.map.map_h; j++)
                                    if (Engine.map.mask[i, j].aux > 0 && Engine.map.mask[i, j].aux < dist)
                                    {
                                        dist = Engine.map.mask[i, j].aux;
                                        x = i; y = j;
                                        found = true; /* deploy close to enemy */
                                    }
                        }
                        if (found)
                        {
                            Action.action_queue_deploy(unit, x, y);
                            avail_unitsIterator = Scenario.avail_units.GetEnumerator();
                            ai_units.Add(unit);
#if DEBUG
                            Console.WriteLine("{0} deployed to {1},{2}", unit.name, x, y);
#endif
                            return false;
                        }
                    }
                    else
                    {
                        ai_status = Engine.deploy_turn ? AI_STATUS.AI_STATUS_END : AI_STATUS.AI_STATUS_MERGE;
                        ai_unitsIterator = ai_units.GetEnumerator();
#if DEBUG
                        Console.WriteLine(Engine.deploy_turn ? "*** END TURN ***" : "*** MERGE ***");
#endif
                    }
                    break;
                case AI_STATUS.AI_STATUS_SUPPLY:
                    /* get next unit */
                    ai_unitsIterator.MoveNext();
                    unit = ai_unitsIterator.Current;
                    if (unit == null)
                    {
                        ai_status = AI_STATUS.AI_STATUS_GROUP;
                        /* build a group with all units, -1,-1 as destination means it will
                           simply attack/defend the nearest target. later on this should
                           split up into several groups with different target and strategy */
                        ai_group = AI_Group.ai_group_create(Engine.cur_player.strat, -1, -1);
                        ai_unitsIterator = ai_units.GetEnumerator();
                        while (ai_unitsIterator.MoveNext() && (unit = ai_unitsIterator.Current) != null)
                            AI_Group.ai_group_add_unit(ai_group, unit);
#if DEBUG
                        Console.WriteLine("*** MOVE & ATTACK ***");
#endif
                    }
                    else
                    {
                        /* check if unit needs supply and remove 
                           it from ai_units if so */
                        if ((unit.CheckLowFuel() || unit.CheckLowAmmo()))
                        {
                            if (unit.supply_level > 0)
                            {
                                Action.action_queue_supply(unit);
                                ai_units.Remove(unit);
#if DEBUG
                                Console.WriteLine("{0} supplies", unit.name);
#endif
                                break;
                            }
                            else
                            {
#if DEBUG
                                Console.WriteLine("{0} searches depot", unit.name);
#endif
                                if (ai_get_dist(unit, unit.x, unit.y, AI_FIND.AI_FIND_DEPOT,
                                                  out dx, out dy, out dist))
                                    if (ai_approximate(unit, dx, dy, out x, out y))
                                    {
                                        Action.action_queue_move(unit, x, y);
                                        ai_units.Remove(unit);
#if DEBUG
                                        Console.WriteLine("{0} moves to {1},{2}", unit.name, x, y);
#endif
                                        break;
                                    }
                            }
                        }
                    }
                    break;
                case AI_STATUS.AI_STATUS_MERGE:
                    if (ai_unitsIterator.MoveNext() && (unit = ai_unitsIterator.Current) != null)
                    {
                        Engine.map.map_get_merge_units(unit, out partners, out partner_count);
                        best = null; /* merge with the one that has the most strength points */
                        for (i = 0; i < partner_count; i++)
                            if (best == null)
                                best = partners[i];
                            else
                                if (best.str < partners[i].str)
                                    best = partners[i];
                        if (best != null)
                        {
#if DEBUG
                            Console.WriteLine("{0} merges with {1}", unit.name, best.name);
#endif
                            Action.action_queue_merge(unit, best);
                            /* both units are handled now */
                            ai_units.Remove(unit);
                            ai_units.Remove(best);
                        }
                    }
                    else
                    {
                        ai_status = AI_STATUS.AI_STATUS_SUPPLY;
                        ai_unitsIterator = ai_units.GetEnumerator();
#if DEBUG
                        Console.WriteLine("*** SUPPLY ***");
#endif
                    }
                    break;
                case AI_STATUS.AI_STATUS_GROUP:
                    if (!AI_Group.ai_group_handle_next_unit(ai_group))
                    {
                        AI_Group.ai_group_delete(ai_group);
                        ai_status = AI_STATUS.AI_STATUS_END;
#if DEBUG
                        Console.WriteLine("*** END TURN ***");
#endif
                    }
                    break;
                case AI_STATUS.AI_STATUS_END:
                    Action.action_queue_end_turn();
                    ai_status = AI_STATUS.AI_STATUS_FINALIZE;
                    result = true;
                    break;
            }
            return result;
        }

        /*
        ====================================================================
        Undo the steps (e.g. memory allocation) made in ai_init()
        ====================================================================
        */
        public static void ai_finalize()
        {
            Console.WriteLine("ai_finalize()");
            while (Engine.stateMachine.scheduler.IsRunning)
            {
                Action.CheckScheduler();
                System.Threading.Thread.Sleep(100);
            }

            if (finalized)
                return;
            Console.WriteLine("Really finalized");
            ai_units.Clear();
#if DEBUG
            Console.WriteLine("AI Finalized");
#endif
            ai_status = AI_STATUS.AI_STATUS_INIT;
            finalized = true;
        }

        struct AI_Pos
        {
            public int x, y;
        } ;

        /*
        ====================================================================
        Check the surrounding of a tile and apply the eval function to
        it. Results may be stored in the context.
        If 'eval_func' returns False the evaluation is broken up.
        ====================================================================
        */
        static AI_Pos ai_create_pos(int x, int y)
        {
            AI_Pos pos = new AI_Pos();
            pos.x = x; pos.y = y;
            return pos;
        }
        public delegate bool eval_func_delegate(int i, int j, object ctx);

        internal static void ai_eval_hexes(int x, int y, int range, eval_func_delegate eval_func, object ctx)
        {
            Queue<AI_Pos> list = new Queue<AI_Pos>();
            AI_Pos pos;
            int i, nx, ny;
            /* gather and handle all positions by breitensuche.
               use AUX mask to mark visited positions */
            Engine.map.map_clear_mask(MAP_MASK.F_AUX);
            list.Enqueue(ai_create_pos(x, y));
            Engine.map.mask[x, y].aux = 1;
            while (list.Count > 0)
            {
                pos = list.Dequeue();
                if (!eval_func(pos.x, pos.y, ctx))
                {
                    break;
                }
                for (i = 0; i < 6; i++)
                    if (Misc.get_close_hex_pos(pos.x, pos.y, i, out nx, out ny))
                        if (Engine.map.mask[nx, ny].aux == 0 && Misc.get_dist(x, y, nx, ny) <= range)
                        {
                            list.Enqueue(ai_create_pos(nx, ny));
                            Engine.map.mask[nx, ny].aux = 1;
                        }
            }
        }

    }
}
