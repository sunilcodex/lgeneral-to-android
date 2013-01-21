/*
 * Creado por SharpDevelop.
 * Usuario: asantos
 * Fecha: 09/01/2009
 * Hora: 16:01
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Collections.Generic;
using EngineA;
using Miscellaneous;

namespace AI_Enemy
{
    /// <summary>
    /// Engine actions
    /// </summary>
    public enum EngineActionsTypes
    {
        ACTION_NONE = 0,
        ACTION_END_TURN,
        ACTION_MOVE,
        ACTION_ATTACK,
        ACTION_SUPPLY,
        ACTION_EMBARK_SEA,
        ACTION_DEBARK_SEA,
        ACTION_EMBARK_AIR,
        ACTION_DEBARK_AIR,
        ACTION_MERGE,
        ACTION_SPLIT,
        ACTION_DISBAND,
        ACTION_DEPLOY,
        ACTION_DRAW_MAP,
        ACTION_SET_SPOT_MASK,
        ACTION_SET_VMODE,
        ACTION_QUIT,
        ACTION_RESTART,
        ACTION_LOAD,
        ACTION_OVERWRITE,
        ACTION_START_SCEN,
        ACTION_START_CAMP,
        //Added for C# version

        Dispose,
        TimerElapsed,

        // MOVE UNIT
        ACTION_END_MOVE,
        ACTION_START_SINGLE_MOVE,
        ACTION_RUN_SINGLE_MOVE,
        ACTION_CHECK_LAST_MOVE,

        // ATTACK UNIT
        ACTION_INIT_ATTACK,
        ACTION_COMBAT,
        ACTION_CHECK_RESULT,
        ACTION_END_COMBAT
    }

    /// <summary>
    /// Description of Unit.
    /// </summary>
    public class Action
    {
        static Queue<Action> actions = new Queue<Action>();

        public EngineActionsTypes type;       /* type as above */
        public Unit unit;     			/* unit performing the action */
        public Unit target;   			/* target if attack */
        public int x, y;       		/* dest position if movement */
        public int w, h, full; 		/* video mode settings */
        public int id;         		/* slot id if any */
        public int str;        /* strength of split */

        /*
        ====================================================================
        CCreate basic action.
        ====================================================================
        */
        public static Action CreateAction(EngineActionsTypes actionType)
        {
            Action action = new Action();
            action.type = actionType;
            return action;
        }


        /*
        ====================================================================
        CreateAction/delete engine action queue
        ====================================================================
        */
        public static void actions_create()
        {
        }

        public static void actions_delete()
        {
            if (actions != null)
            {
                actions.Clear();
                actions = null;
            }
        }
        /*
        ====================================================================
        Queue an action.
        ====================================================================
        */
        public static void action_queue(Action action)
        {
            actions.Enqueue(action);
            CheckScheduler();
        }
        public static void action_queue(EngineActionsTypes actionType)
        {
            actions.Enqueue(CreateAction(actionType));
            CheckScheduler();
        }

        /*
        ====================================================================
        Get next action or clear all actions. The returned action struct
        must be cleared by engine after usage.
        ====================================================================
        */
        public static Action actions_dequeue()
        {
            if (actions.Count != 0)
                return actions.Dequeue();
            else
                return null;
        }

        public static void actions_clear()
        {
            actions.Clear();
            CheckScheduler();
        }

        /*
        ====================================================================
        Returns topmost action in queue or 0 if none available.
        ====================================================================
        */
        public static Action actions_top()
        {
            return actions.Peek();
        }

        /*
        ====================================================================
        Remove the last action in queue (cancelled confirmation)
        ====================================================================
        */
        public static void action_remove_last()
        {
            Action action;
            if (actions.Count == 0) return;
            action = actions.Dequeue();
        }

        /*
        ====================================================================
        Get number of queued actions
        ====================================================================
        */
        public static int actions_count()
        {
            return actions.Count;
        }

        /*
        ====================================================================
        CreateAction an engine action and automatically queue it. The engine
        will perform security checks before handling an action to prevent
        illegal actions.
        ====================================================================
        */
#if TODO_RR
        public static void action_queue_none()
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_NONE);
            action_queue(action);
        }
#endif
        public static void action_queue_end_turn()
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_END_TURN);
            action_queue(action);
        }

        public static void action_queue_move(Unit unit, int x, int y)
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_MOVE);
            action.unit = unit;
            action.x = x;
            action.y = y;
            action_queue(action);
        }

        public static void action_queue_attack(Unit unit, Unit target)
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_ATTACK);
            action.unit = unit;
            action.target = target;
            action_queue(action);
        }

        public static void action_queue_supply(Unit unit)
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_SUPPLY);
            action.unit = unit;
            action_queue(action);
        }
#if TODO_RR
        public static void action_queue_embark_sea(Unit unit, int x, int y)
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_EMBARK_SEA);
            action.unit = unit;
            action.x = x; action.y = y;
            action_queue(action);
        }
#endif
        public static void action_queue_debark_sea(Unit unit, int x, int y)
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_DEBARK_SEA);
            action.unit = unit;
            action.x = x; action.y = y;
            action_queue(action);
        }
#if TODO_RR
        public static void action_queue_embark_air(Unit unit, int x, int y)
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_EMBARK_AIR);
            action.unit = unit;
            action.x = x; action.y = y;
            action_queue(action);
        }
        public static void action_queue_debark_air(Unit unit, int x, int y, int normal_landing)
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_DEBARK_AIR);
            action.unit = unit;
            action.x = x; action.y = y;
            action.id = normal_landing;
            action_queue(action);
        }
#endif
        public static void action_queue_merge(Unit unit, Unit partner)
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_MERGE);
            action.unit = unit;
            action.target = partner;
            action_queue(action);
        }
#if TODO_RR
        public static void action_queue_split(Unit unit, int str, int x, int y, Unit partner)
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_SPLIT);
            action.unit = unit;
            action.target = partner;
            action.x = x; action.y = y;
            action.str = str;
            action_queue(action);
        }
        public static void action_queue_disband(Unit unit)
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_DISBAND);
            action.unit = unit;
            action_queue(action);
        }
#endif
        public static void action_queue_deploy(Unit unit, int x, int y)
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_DEPLOY);
            action.unit = unit;
            action.x = x;
            action.y = y;
            action_queue(action);
        }
#if TODO_RR
        public static void action_queue_draw_map()
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_DRAW_MAP);
            action_queue(action);
        }
        public static void action_queue_set_spot_mask()
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_SET_SPOT_MASK);
            action_queue(action);
        }
        public static void action_queue_set_vmode(int w, int h, int fullscreen)
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_SET_VMODE);
            action.w = w; action.h = h;
            action.full = fullscreen;
            action_queue(action);
        }
        public static void action_queue_quit()
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_QUIT);
            action_queue(action);
        }
        public static void action_queue_restart()
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_RESTART);
            action_queue(action);
        }
        public static void action_queue_load(int id)
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_LOAD);
            action.id = id;
            action_queue(action);
        }
        public static void action_queue_overwrite(int id)
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_OVERWRITE);
            action.id = id;
            action_queue(action);
        }
        public static void action_queue_start_scen()
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_START_SCEN);
            action_queue(action);
        }
        public static void action_queue_start_camp()
        {
            Action action = CreateAction(EngineActionsTypes.ACTION_START_CAMP);
            action_queue(action);
        }
#endif

        /* functions needed for movement and combat phase */
        private static void ProcessQueue()
        {
            Action action = actions_dequeue();

            if (action == null)
            {
                CheckScheduler();
            }
            else
            {
                Engine.stateMachine.operation.Post(delegate
                {
                    Engine.stateMachine.Send(action.type, action);
                    Engine.stateMachine.Execute();
                    CheckScheduler();
                }, null);
                /*
                Engine.stateMachine.Send(action.type);
                Engine.stateMachine.Execute();
                CheckScheduler();
                 */
           }
        }

        public static void CheckScheduler()
        {
            if (actions.Count > 0)
            {
                if (!Engine.stateMachine.scheduler.IsRunning)
                    Engine.stateMachine.scheduler.Start();

                Engine.stateMachine.scheduler.Add(1, Config.schedulerTimeOut,
                                                  new EngineStateMachine.SendTimerDelegate(ProcessQueue));

            }
            else
                if (Engine.stateMachine.scheduler.IsRunning)
                {
                    Engine.stateMachine.scheduler.Stop();
                    Engine.stateMachine.scheduler.Clear();
                }

        }
    }
}
