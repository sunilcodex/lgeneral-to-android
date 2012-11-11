using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EngineA;
using DataFile;
using Miscellaneous;

namespace AI_Enemy
{
    public enum AI_FIND
    {
        AI_FIND_DEPOT,
        AI_FIND_OWN_TOWN,
        AI_FIND_ENEMY_TOWN,
        AI_FIND_OWN_OBJ,
        AI_FIND_ENEMY_OBJ
    };

    public enum GS
    {
        GS_ART_FIRE = 0,
        GS_MOVE
    };

    public class AI_Group
    {

        List<Unit> units;
        int order; /* -2: defend position by all means
                  -1: defend position
                   0: proceed to position
                   1: attack position
                   2: attack position by all means */
        int x, y; /* position that is this group's center of interest */
        GS state; /* state as listed above */
        /* counters for sorted unit list */
        int ground_count, aircraft_count, bomber_count;

        static int group_units_pos = 0;

        /*
        ====================================================================
        Check if there is an unspotted tile or an enemy within range 6
        that may move close enough to attack. Flying units are not counted
        as these may move very far anyway.
        ====================================================================
        */
        struct MountCtx
        {
            public MountCtx(Player pl, int x, int y, bool u)
            {
                player = pl;
                unit_x = x;
                unit_y = y;
                unsafe_ = u;
            }
            public Player player;
            public int unit_x, unit_y;
            public bool unsafe_;
        } ;

        static bool hex_is_safe(int x, int y, object _ctx)
        {
            MountCtx ctx = (MountCtx)_ctx;
            if (!Engine.map.mask[x, y].spot)
            {
                ctx.unsafe_ = true;
                return false;
            }
            if (Engine.map.map[x, y].g_unit != null)
                if (!Player.player_is_ally(ctx.player, Engine.map.map[x, y].g_unit.player))
                    if (Engine.map.map[x, y].g_unit.sel_prop.mov >= Misc.get_dist(ctx.unit_x, ctx.unit_y, x, y) - 1)
                    {
                        ctx.unsafe_ = true;
                        return false;
                    }
            return true;
        }

        static bool ai_unsafe__mount(Unit unit, int x, int y)
        {
            MountCtx ctx = new MountCtx(unit.player, x, y, false);
            AI.ai_eval_hexes(x, y, 6, new AI.eval_func_delegate(hex_is_safe), ctx);
            return ctx.unsafe_;
        }

        /*
        ====================================================================
        Count the number of defensive supporters.
        ====================================================================
        */
        class DefCtx
        {
            public Unit unit;
            public int count;
        } ;
        static bool hex_df_unit(int x, int y, object _ctx)
        {
            DefCtx ctx = (DefCtx)_ctx;
            if (Engine.map.map[x, y].g_unit != null)
            {
                if ((ctx.unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
                {
                    if ((Engine.map.map[x, y].g_unit.sel_prop.flags & UnitFlags.AIR_DEFENSE) == UnitFlags.AIR_DEFENSE)
                        ctx.count++;
                }
                else
                {
                    if ((Engine.map.map[x, y].g_unit.sel_prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY)
                        ctx.count++;
                }
            }
            return true;
        }
        static void ai_count_df_units(Unit unit, int x, int y, out int result)
        {
            DefCtx ctx = new DefCtx();
            ctx.unit = unit;
            ctx.count = 0;
            result = 0;
            if ((unit.sel_prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY)
                return;
            AI.ai_eval_hexes(x, y, 3, new AI.eval_func_delegate(hex_df_unit), ctx);
            /* only three defenders are taken in account */
            if (result > 3)
                result = 3;
        }

        /*
        ====================================================================
        Gather all valid targets of a unit.
        ====================================================================
        */
        class GatherCtx
        {
            public Unit unit;
            public List<Unit> targets;
        } ;

        static bool hex_add_targets(int x, int y, object _ctx)
        {
            GatherCtx ctx = (GatherCtx)_ctx;
            if (Engine.map.mask[x, y].spot)
            {
                if (Engine.map.map[x, y].a_unit != null &&
                    ctx.unit.CheckAttack(Engine.map.map[x, y].a_unit, Unit.UNIT_ATTACK.UNIT_ACTIVE_ATTACK))
                    ctx.targets.Add(Engine.map.map[x, y].a_unit);
                if (Engine.map.map[x, y].g_unit != null &&
                    ctx.unit.CheckAttack(Engine.map.map[x, y].g_unit, Unit.UNIT_ATTACK.UNIT_ACTIVE_ATTACK))
                    ctx.targets.Add(Engine.map.map[x, y].g_unit);
            }
            return true;
        }

        static List<Unit> ai_gather_targets(Unit unit, int x, int y)
        {
            GatherCtx ctx = new GatherCtx();
            ctx.unit = unit;
            ctx.targets = new List<Unit>();
            AI.ai_eval_hexes(x, y, unit.sel_prop.rng + 1, new AI.eval_func_delegate(hex_add_targets), ctx);
            return ctx.targets;
        }

        /*
        ====================================================================
        Evaluate a unit's attack against target.
          score_base: basic score for attacking
          score_rugged: score added for each rugged def point (0-100)
                        of target
          score_kill: score unit receives for each (expected) point of
                      strength damage done to target
          score_loss: score that is substracted per strength point
                      unit is expected to loose
        The final score is stored to 'result' and True if returned if the
        attack may be performed else False.
        ====================================================================
        */

        static bool unit_evaluate_attack(Unit unit, Unit target, int score_base, int score_rugged, int score_kill, int score_loss, out int result)
        {
            int unit_dam = 0, target_dam = 0, rugged_def = 0;
            result = 0;
            if (!unit.CheckAttack(  target, Unit.UNIT_ATTACK.UNIT_ACTIVE_ATTACK)) return false;
            Unit.GetExpectedLosses(unit, target, out unit_dam, out target_dam);
            if (unit.CheckRuggedDefense( target))
                rugged_def = unit.GetRuggedDefenseChance( target);
            if (rugged_def < 0) rugged_def = 0;
            result = score_base + rugged_def * score_rugged + target_dam * score_kill + unit_dam * score_loss;
#if DEBUG
            Console.WriteLine("  {0}: {1}: bas:{2}, rug:{3}, kil:{4}, los: {5} = {6}", unit.name, target.name,
            score_base,
            rugged_def * score_rugged,
            target_dam * score_kill,
            unit_dam * score_loss, result);
#endif
            /* if target is a df unit give a small bonus */
            if (((target.sel_prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY) ||
                ((target.sel_prop.flags & UnitFlags.AIR_DEFENSE) == UnitFlags.AIR_DEFENSE))
                result += score_kill;
            return true;
        }

        /*
        ====================================================================
        Get the best target for unit if any.
        ====================================================================
        */
        static bool ai_get_best_target(Unit unit, int x, int y, AI_Group group, out Unit target, out int score)
        {
            int old_x = unit.x, old_y = unit.y;
            int pos_targets;
            List<Unit> targets;
            int score_atk_base, score_rugged, score_kill, score_loss;

            /* scores */
            score_atk_base = 20 + group.order * 10;
            score_rugged = -1;
            score_kill = (group.order + 3) * 10;
            score_loss = (2 - group.order) * -10;

            unit.x = x; unit.y = y;
            target = null;
            score = -999999;
            /* if the transporter is needed attacking is suicide */
            if (Engine.map.mask[x, y].mount != 0 && !string.IsNullOrEmpty(unit.trsp_prop.id))
                return false;
            /* gather targets */
            targets = ai_gather_targets(unit, x, y);
            /* evaluate all attacks */
            if (targets != null)
            {
                foreach (Unit entry in targets)
                    if (!AI_Group.unit_evaluate_attack(unit, entry, score_atk_base, score_rugged, score_kill, score_loss, out entry.target_score))
                        targets.Remove(entry); /* erroneous entry: can't be attacked */
                /* check whether any positive targets exist */
                pos_targets = 0;
                foreach (Unit entry in targets)
                    if (entry.target_score > 0)
                    {
                        pos_targets = 1;
                        break;
                    }
                /* get best target */
                foreach (Unit entry in targets)
                {
                    /* if unit is on an objective or center of interest give a bonus
                    as this tile must be captured by all means. but only do so if there
                    is no other target with a positive value */
                    if (pos_targets == 0)
                        if ((entry.x == group.x && entry.y == group.y) || Engine.map.map[entry.x, entry.y].obj)
                        {
                            entry.target_score += 100;
#if DEBUG
                            Console.WriteLine("    + 100 for {0}: capture by all means", entry.name);
#endif
                        }

                    if (entry.target_score > score)
                    {
                        target = entry;
                        score = entry.target_score;
                    }
                }
                targets.Clear();
            }
            unit.x = old_x;
            unit.y = old_y;
            return (target != null);
        }
        /*
        ====================================================================
        Evaluate position for a unit by checking the group context. 
        Return True if this evaluation is valid. The results are stored
        to 'eval'.
        ====================================================================
        */
        struct AI_Eval
        {
            public Unit unit; /* unit that's checked */
            public AI_Group group;
            public int x, y; /* position that was evaluated */
            public int mov_score; /* result for moving */
            public Unit target; /* if set atk_result is relevant */
            public int atk_score; /* result including attack evaluation */
        }

        static AI_Eval ai_create_eval(Unit unit, AI_Group group, int x, int y)
        {
            AI_Eval eval = new AI_Eval();
            eval.unit = unit; eval.group = group;
            eval.x = x; eval.y = y;
            return eval;
        }

        static bool ai_evaluate_hex(ref AI_Eval eval)
        {
            int result;
            int i, nx, ny, ox, oy, odist, j, nx2, ny2;
            bool covered;
            eval.target = null;
            eval.mov_score = eval.atk_score = 0;
            /* terrain modifications which only apply for ground units */
            if ((eval.unit.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING)
            {
                /* entrenchment bonus. infantry receives more than others. */
                eval.mov_score += (((eval.unit.sel_prop.flags & UnitFlags.INFANTRY) == UnitFlags.INFANTRY) ? 2 : 1) *
                                   (Engine.map.map[eval.x, eval.y].terrain.min_entr * 2 +
                                     Engine.map.map[eval.x, eval.y].terrain.max_entr);
                /* if the unit looses initiative on this terrain we give a malus */
                if (Engine.map.map[eval.x, eval.y].terrain.max_ini < eval.unit.sel_prop.ini)
                    eval.mov_score -= 5 * (eval.unit.sel_prop.ini -
                                              Engine.map.map[eval.x, eval.y].terrain.max_ini);
                /* rivers should be avoided */
                if ((Engine.map.map[eval.x, eval.y].terrain.flags[Scenario.cur_weather] & Terrain_flags.RIVER) == Terrain_flags.RIVER)
                    eval.mov_score -= 50;
                if ((Engine.map.map[eval.x, eval.y].terrain.flags[Scenario.cur_weather] & Terrain_flags.SWAMP) == Terrain_flags.SWAMP)
                    eval.mov_score -= 30;
                /* inf_close_def will benefit an infantry while disadvantaging
                   other units */
                if ((Engine.map.map[eval.x, eval.y].terrain.flags[Scenario.cur_weather] & Terrain_flags.INF_CLOSE_DEF) == Terrain_flags.INF_CLOSE_DEF)
                {
                    if ((eval.unit.sel_prop.flags & UnitFlags.INFANTRY) == UnitFlags.INFANTRY)
                        eval.mov_score += 30;
                    else
                        eval.mov_score -= 20;
                }
                /* if this is a mount position and an enemy or fog is less than
                   6 tiles away we give a big malus */
                if (Engine.map.mask[eval.x, eval.y].mount != 0)
                    if (ai_unsafe__mount(eval.unit, eval.x, eval.y))
                        eval.mov_score -= 1000;
                /* conquering a flag gives a bonus */
                if (Engine.map.map[eval.x, eval.y].player != null)
                    if (!Player.player_is_ally(eval.unit.player, Engine.map.map[eval.x, eval.y].player))
                        if (Engine.map.map[eval.x, eval.y].g_unit == null)
                        {
                            eval.mov_score += 600;
                            if (Engine.map.map[eval.x, eval.y].obj)
                                eval.mov_score += 600;
                        }
                /* if this position allows debarking or is just one hex away
                   this tile receives a big bonus. */
                if (eval.unit.embark == UnitEmbarkTypes.EMBARK_SEA)
                {
                    if (Engine.map.map_check_unit_debark(eval.unit, eval.x, eval.y, UnitEmbarkTypes.EMBARK_SEA, false))
                        eval.mov_score += 1000;
                    else
                        for (i = 0; i < 6; i++)
                            if (Misc.get_close_hex_pos(eval.x, eval.y, i, out nx, out ny))
                                if (Engine.map.map_check_unit_debark(eval.unit, nx, ny, UnitEmbarkTypes.EMBARK_SEA, false))
                                {
                                    eval.mov_score += 500;
                                    break;
                                }
                }
            }
            /* modifications on flying units */
            if ((eval.unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
            {
                /* if interceptor covers an uncovered bomber on this tile give bonus */
                if ((eval.unit.sel_prop.flags & UnitFlags.INTERCEPTOR) == UnitFlags.INTERCEPTOR)
                {
                    for (i = 0; i < 6; i++)
                        if (Misc.get_close_hex_pos(eval.x, eval.y, i, out nx, out ny))
                            if (Engine.map.map[nx, ny].a_unit != null)
                                if (Player.player_is_ally(Engine.cur_player, Engine.map.map[nx, ny].a_unit.player))
                                    if ((Engine.map.map[nx, ny].a_unit.sel_prop.flags & UnitFlags.BOMBER) == UnitFlags.BOMBER)
                                    {
                                        covered = false;
                                        for (j = 0; j < 6; j++)
                                            if (Misc.get_close_hex_pos(nx, ny, j, out nx2, out ny2))
                                                if (Engine.map.map[nx2, ny2].a_unit != null)
                                                    if (Player.player_is_ally(Engine.cur_player, Engine.map.map[nx2, ny2].a_unit.player))
                                                        if ((Engine.map.map[nx2, ny2].a_unit.sel_prop.flags & UnitFlags.INTERCEPTOR) == UnitFlags.INTERCEPTOR)
                                                            if (Engine.map.map[nx2, ny2].a_unit != eval.unit)
                                                            {
                                                                covered = true;
                                                                break;
                                                            }
                                        if (!covered)
                                            eval.mov_score += 2000; /* 100 equals one tile of getting
                                                    to center of interest which must
                                                    be overcome */
                                    }
                }
            }
            /* each group has a 'center of interest'. getting closer
               to this center is honored. */
            if (eval.group.x == -1)
            {
                /* proceed to the nearest flag */
                if ((eval.unit.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING)
                {
                    if (eval.group.order > 0)
                    {
                        if (AI.ai_get_dist(eval.unit, eval.x, eval.y, AI_FIND.AI_FIND_ENEMY_OBJ, out ox, out oy, out odist))
                            eval.mov_score -= odist * 100;
                    }
                    else
                        if (eval.group.order < 0)
                        {
                            if (AI.ai_get_dist(eval.unit, eval.x, eval.y, AI_FIND.AI_FIND_OWN_OBJ, out ox, out oy, out odist))
                                eval.mov_score -= odist * 100;
                        }
                }
            }
            else
                eval.mov_score -= 100 * Misc.get_dist(eval.x, eval.y, eval.group.x, eval.group.y);
            /* defensive support */
            ai_count_df_units(eval.unit, eval.x, eval.y, out result);
            if (result > 2) result = 2; /* senseful limit */
            eval.mov_score += result * 10;
            /* check for the best target and save the result to atk_score */
            eval.atk_score = eval.mov_score;
            if (Engine.map.mask[eval.x, eval.y].mount == 0)
                if (((eval.unit.sel_prop.flags & UnitFlags.ATTACK_FIRST) != UnitFlags.ATTACK_FIRST) ||
                      eval.unit.x == eval.x && eval.unit.y == eval.y)
                    if (ai_get_best_target(eval.unit, eval.x, eval.y, eval.group, out eval.target, out result))
                        eval.atk_score += result;
            return true;
        }

        /*
        ====================================================================
        Choose and store the best tactical action of a unit (found by use of
        ai_evaluate_hex). If there is none AI_SUPPLY is stored.
        ====================================================================
        */
#if TODO_RR
        static void ai_handle_unit(Unit unit, AI_Group group)
        {
            int x, y, nx, ny, i, action = 0;
            Queue<AI_Eval> list = new Queue<AI_Eval>();
            AI_Eval eval;
            Unit target = null;
            int score = -999999;
            /* get move mask */
            Engine.map.map_get_unit_move_mask(unit);
            x = unit.x;
            y = unit.y;
            target = null;
            /* evaluate all positions */
            list.Enqueue(ai_create_eval(unit, group, unit.x, unit.y));
            while (list.Count > 0)
            {
                eval = list.Dequeue();
                if (ai_evaluate_hex(ref eval))
                {
                    /* movement evaluation */
                    if (eval.mov_score > score)
                    {
                        score = eval.mov_score;
                        target = null;
                        x = eval.x; y = eval.y;
                    }
                    /* movement + attack evaluation. ignore for attack_first
                     * units which already fired */
                    if ((unit.sel_prop.flags & UnitFlags.ATTACK_FIRST) != UnitFlags.ATTACK_FIRST)
                        if (eval.target != null && eval.atk_score > score)
                        {
                            score = eval.atk_score;
                            target = eval.target;
                            x = eval.x; y = eval.y;
                        }
                }
                /* store next hex tiles */
                for (i = 0; i < 6; i++)
                    if (Misc.get_close_hex_pos(eval.x, eval.y, i, out nx, out ny))
                        if ((Engine.map.mask[nx, ny].in_range != 0 && !Engine.map.mask[nx, ny].blocked) || Engine.map.mask[nx, ny].sea_embark)
                        {
                            Engine.map.mask[nx, ny].in_range = 0;
                            Engine.map.mask[nx, ny].sea_embark = false;
                            list.Enqueue(ai_create_eval(unit, group, nx, ny));
                        }
            }
            list.Clear();
            /* check result and store appropiate action */
            if (unit.x != x || unit.y != y)
            {
                if (Engine.map.map_check_unit_debark(unit, x, y, UnitEmbarkTypes.EMBARK_SEA, false))
                {
                    Action.action_queue_debark_sea(unit, x, y); action = 1;
#if DEBUG
                    Console.WriteLine("{0} debarks at {1},{2}", unit.name, x, y);
#endif
                }
                else
                {
                    Action.action_queue_move(unit, x, y); action = 1;
#if DEBUG
                    Console.WriteLine("{0} moves to {1},{2}", unit.name, x, y);
#endif
                }
            }
            if (target != null)
            {
                Action.action_queue_attack(unit, target); action = 1;
#if DEBUG
                Console.WriteLine("{0} attacks {1}", unit.name, target.name);
#endif
            }
            if (action == 0)
            {
                Action.action_queue_supply(unit);
#if DEBUG
                Console.WriteLine("{0} supplies: {1},{2}", unit.name, unit.cur_ammo, unit.cur_fuel);
#endif
            }
        }


        /*
        ====================================================================
        Get the best target and attack by range. Do not try to move the 
        unit yet. If there is no target at all do nothing.
        ====================================================================
        */

        static void ai_fire_artillery(Unit unit, AI_Group group)
        {
            AI_Eval eval = ai_create_eval(unit, group, unit.x, unit.y);
            if (ai_evaluate_hex(ref eval) && eval.target != null)
            {
                Action.action_queue_attack(unit, eval.target);
#if DEBUG
                Console.WriteLine("{0} attacks first {1}", unit.name, eval.target.name);
#endif
            }
        }
#endif
        /*
        ====================================================================
        PUBLICS
        ====================================================================
        */

        /*
        ====================================================================
        CreateAction/Delete a group
        ====================================================================
        */
        public static AI_Group ai_group_create(int order, int x, int y)
        {
            AI_Group group = new AI_Group();
            group.state = GS.GS_ART_FIRE;
            group.order = order;
            group.x = x; group.y = y;
            group.units = new List<Unit>();
            return group;
        }

        public static void ai_group_add_unit(AI_Group group, Unit unit)
        {
            /* sort unit into units list in the order in which units are
             * supposed to move. artillery fire is first but moves last
             * thus artillery comes last in this list. that it fires first
             * is handled by ai_group_handle_next_unit. list order is: 
             * bombers, ground units, fighters, artillery */
            if (((unit.prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY) ||
                 ((unit.prop.flags & UnitFlags.AIR_DEFENSE) == UnitFlags.AIR_DEFENSE))
                group.units.Add(unit);
            else
                if (unit.prop.unit_class == 9 || unit.prop.unit_class == 10)
                {
                    /* tactical and high level bomber */
                    group.units.Insert(0, unit);
                    group.bomber_count++;
                }
                else
                    if ((unit.prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
                    {
                        /* airborne ground units are not in this section ... */
                        group.units.Insert(group.bomber_count + group.ground_count, unit);
                        group.aircraft_count++;
                    }
                    else
                    {
                        /* everything else: ships and ground units */
                        group.units.Insert(group.bomber_count, unit);
                        group.ground_count++;
                    }
            /* HACK: set hold_pos flag for those units that should not move due
               to high entrenchment or being close to artillery and such; but these
               units will attack, too and may move if's really worth it */
            if ((unit.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING)
                if ((unit.sel_prop.flags & UnitFlags.SWIMMING) != UnitFlags.SWIMMING)
                {
                    int i, nx, ny;
                    bool no_move = false;
                    if (Engine.map.map[unit.x, unit.y].obj) no_move = true;
                    if (group.order < 0)
                    {
                        if (Engine.map.map[unit.x, unit.y].nation != null) no_move = true;
                        if (unit.entr >= 6) no_move = true;
                        if ((unit.sel_prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY) no_move = true;
                        if ((unit.sel_prop.flags & UnitFlags.AIR_DEFENSE) == UnitFlags.AIR_DEFENSE) no_move = true;
                        for (i = 0; i < 6; i++)
                            if (Misc.get_close_hex_pos(unit.x, unit.y, i, out nx, out ny))
                                if (Engine.map.map[nx, ny].g_unit != null)
                                {
                                    if ((Engine.map.map[nx, ny].g_unit.sel_prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY)
                                    {
                                        no_move = true;
                                        break;
                                    }
                                    if ((Engine.map.map[nx, ny].g_unit.sel_prop.flags & UnitFlags.AIR_DEFENSE) == UnitFlags.AIR_DEFENSE)
                                    {
                                        no_move = true;
                                        break;
                                    }
                                }
                    }
                    if (no_move)
                        unit.cur_mov = 0;
                }
        }

        public static void ai_group_delete_unit(AI_Group group, Unit unit)
        {
            /* remove unit */
            bool contained_unit = (group.units[group_units_pos] == unit);
            group.units.Remove(unit);
            if (contained_unit) return;

            /* update respective counter */
            if (((unit.prop.flags & UnitFlags.ARTILLERY) == UnitFlags.ARTILLERY) ||
                (unit.prop.flags & UnitFlags.AIR_DEFENSE) == UnitFlags.AIR_DEFENSE)
                /* nothing to be done */
                ;
            else
                if (unit.prop.unit_class == 9 || unit.prop.unit_class == 10)
                {
                    /* tactical and high level bomber */
                    group.bomber_count--;
                }
                else
                    if ((unit.prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
                    {
                        /* airborne ground units are not in this section ... */
                        group.aircraft_count--;
                    }
                    else
                    {
                        /* everything else: ships and ground units */
                        group.ground_count--;
                    }
        }

        public static void ai_group_delete(AI_Group ptr)
        {
            AI_Group group = ptr;
            if (group != null)
            {
                if (group.units != null)
                    group.units.Clear();
            }
        }
        /*
        ====================================================================
        Handle next unit of a group to follow order. Stores all nescessary 
        unit actions. If group is completely handled, it returns False.
        ====================================================================
        */
#if TODO_RR
        public static bool ai_group_handle_next_unit(AI_Group group)
        {

            Unit unit = null;
            if (group_units_pos < group.units.Count)
                unit = group.units[group_units_pos++];

            if (unit == null)
            {
                if (group.state == GS.GS_MOVE)
                    return false;
                else
                {
                    group.state = GS.GS_MOVE;
                    group_units_pos = 0;
                    if (group_units_pos < group.units.Count)
                        unit = group.units[group_units_pos++];
                    else
                        return false;
                }
            }
            if (unit == null)
            {
                Console.WriteLine("ERROR: ai_group_handle_next_unit: null unit detected");
                return false;
            }
            /* Unit is dead? Can only be attacker that was killed by defender */
            if (unit.killed != 0)
            {
                Console.WriteLine("Removingkilled attacker %s(%d,%d) from group\n", unit.name, unit.x, unit.y);
                ai_group_delete_unit(group, unit);
                return ai_group_handle_next_unit(group);
            }
            if (group.state == GS.GS_ART_FIRE)
            {
                if ((unit.sel_prop.flags & UnitFlags.ATTACK_FIRST) == UnitFlags.ATTACK_FIRST)
                    ai_fire_artillery(unit, group); /* does not check optimal 
                                                 movement but simply 
                                                 fires */
            }
            else
                ai_handle_unit(unit, group); /* checks to the full tactical
                                          extend */
            return true;
        }
#endif
    }
}
