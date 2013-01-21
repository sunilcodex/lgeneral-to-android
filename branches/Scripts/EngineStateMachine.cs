using System;
using System.ComponentModel;
using Sanford.StateMachineToolkit;
using Sanford.Threading;
using AI_Enemy;
using Miscellaneous;

namespace EngineA
{
    /* ACTION */
    public enum StateID
    {
        STATUS_NONE = 0,
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

        STATUS_INGAME,              /* actions that are divided into different phases
                                       have this status set */
        PHASE_NONE,

        Disposed,

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

    public class EngineStateMachine : PassiveStateMachine<StateID, EngineActionsTypes>, IDisposable
    {
        internal readonly AsyncOperation operation = AsyncOperationManager.CreateOperation(null);

        internal readonly DelegateScheduler scheduler = new DelegateScheduler();

        private bool isDisposed;

        public EngineStateMachine()
        {
            scheduler.PollingInterval = Config.schedulerTimeOut / 2;
            States[StateID.STATUS_INGAME].EntryHandler += StartNewScenarioGame;

            States[StateID.PHASE_INIT_MOVE].EntryHandler += Engine.InitMove;
            States[StateID.PHASE_START_SINGLE_MOVE].EntryHandler += Engine.SingleMove;
            States[StateID.PHASE_RUN_SINGLE_MOVE].EntryHandler += Engine.RunMove;
            States[StateID.PHASE_CHECK_LAST_MOVE].EntryHandler += Engine.CheckLastMove;
            States[StateID.PHASE_END_MOVE].EntryHandler += Engine.EndMove;

            States[StateID.PHASE_INIT_ATK].EntryHandler += Engine.InitAttack;
            States[StateID.PHASE_COMBAT].EntryHandler += Engine.ShowAttackCross;
            States[StateID.PHASE_CHECK_RESULT].EntryHandler += Engine.CheckResult;
            States[StateID.PHASE_END_COMBAT].EntryHandler += Engine.EndCombat;

            SetupSubstates(StateID.STATUS_INGAME, HistoryType.None,
                           StateID.PHASE_NONE,
                           StateID.PHASE_INIT_MOVE,
                           StateID.PHASE_START_SINGLE_MOVE,
                           StateID.PHASE_RUN_SINGLE_MOVE,
                           StateID.PHASE_CHECK_LAST_MOVE,
                           StateID.PHASE_END_MOVE);

            AddTransition(StateID.STATUS_NONE, EngineActionsTypes.ACTION_START_SCEN, StateID.STATUS_INGAME, StartNewScenario);

            /* DISPOSE */
            AddTransition(StateID.STATUS_NONE, EngineActionsTypes.Dispose, StateID.Disposed);
            AddTransition(StateID.STATUS_INGAME, EngineActionsTypes.Dispose, StateID.Disposed);

            /* MOVEMENT */
            AddTransition(StateID.STATUS_NONE, EngineActionsTypes.ACTION_MOVE, StateID.PHASE_INIT_MOVE, ActionMove);
            AddTransition(StateID.PHASE_INIT_MOVE, EngineActionsTypes.ACTION_END_MOVE, StateID.STATUS_NONE);
            AddTransition(StateID.PHASE_INIT_MOVE, EngineActionsTypes.ACTION_START_SINGLE_MOVE, StateID.PHASE_START_SINGLE_MOVE);
            AddTransition(StateID.PHASE_START_SINGLE_MOVE, EngineActionsTypes.ACTION_RUN_SINGLE_MOVE, StateID.PHASE_RUN_SINGLE_MOVE);
            AddTransition(StateID.PHASE_START_SINGLE_MOVE, EngineActionsTypes.ACTION_CHECK_LAST_MOVE, StateID.PHASE_CHECK_LAST_MOVE);
            AddTransition(StateID.PHASE_RUN_SINGLE_MOVE, EngineActionsTypes.ACTION_START_SINGLE_MOVE, StateID.PHASE_START_SINGLE_MOVE);
            AddTransition(StateID.PHASE_CHECK_LAST_MOVE, EngineActionsTypes.ACTION_END_MOVE, StateID.PHASE_END_MOVE);
            AddTransition(StateID.PHASE_END_MOVE, EngineActionsTypes.ACTION_NONE, StateID.STATUS_NONE);

            /* COMBAT */
            AddTransition(StateID.STATUS_NONE, EngineActionsTypes.ACTION_ATTACK, StateID.PHASE_INIT_ATK, Engine.ActionAttack);
            AddTransition(StateID.PHASE_INIT_ATK, EngineActionsTypes.ACTION_COMBAT, StateID.PHASE_COMBAT);
            AddTransition(StateID.PHASE_COMBAT, EngineActionsTypes.ACTION_CHECK_RESULT, StateID.PHASE_CHECK_RESULT);
            AddTransition(StateID.PHASE_CHECK_RESULT, EngineActionsTypes.ACTION_END_COMBAT, StateID.PHASE_END_COMBAT);
            AddTransition(StateID.PHASE_END_COMBAT, EngineActionsTypes.ACTION_NONE, StateID.STATUS_NONE);


            Initialize(StateID.STATUS_NONE);
        }

        #region Entry/Exit Methods

        private void StartNewScenarioGame()
        {
            if (this.IsInitialized)
                Console.WriteLine("Entering On state: " + this.CurrentStateID);
            Console.WriteLine("New Scenario Game.");
        }

        private void EntryDisposed()
        {
            scheduler.Dispose();

            operation.OperationCompleted();

            isDisposed = true;
        }

        #endregion

        #region Action Methods

        private void StartNewScenario(object[] args)
        {
            Console.WriteLine("Start a new Scenario.");

            ActionResult = "Start a new Scenario.";
        }

        private void ActionMove(object[] args)
        {
            AI_Enemy.Action action = (AI_Enemy.Action)args[0];
            Engine.cur_unit = action.unit;
            Console.WriteLine("ActionMove: " + ((action.unit == null) ? "null" : action.unit.name));
            Engine.move_unit = action.unit;
            if (Engine.move_unit.cur_mov == 0)
            {
                Console.WriteLine("'{0}' has no move points remaining\n", Engine.move_unit.name);
                return;
            }
            Engine.dest_x = action.x;
            Engine.dest_y = action.y;
            //status = STATUS_MOVE;
            //phase = PHASE_INIT_MOVE;
            Engine.engine_clear_danger_mask();
            //if (Engine.cur_ctrl == PLAYERCONTROL.PLAYER_CTRL_HUMAN)
            //    image_hide(gui.cursors, 1);
            Engine.draw_map = true;
        }


        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            #region Guard

            if (isDisposed)
            {
                return;
            }

            #endregion

            Send(EngineActionsTypes.Dispose);
        }

        #endregion

        #region Nested type: SendTimerDelegate

        internal delegate void SendTimerDelegate();

        #endregion
    }
}
