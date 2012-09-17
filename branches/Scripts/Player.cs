/*
 * Creado por SharpDevelop.
 * Usuario: asantos
 * Fecha: 09/01/2009
 * Hora: 16:16
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Collections.Generic;

namespace Engine
{
    public delegate void AiInit();
    public delegate bool AiRun();
    public delegate void AiFinalize();
    /*
    ====================================================================
    Player info. The players are created by Scenario but managed
    by this module.
    ====================================================================
    */
    public enum PLAYERCONTROL
    {
        PLAYER_CTRL_NOBODY = 0,
        PLAYER_CTRL_HUMAN,
        PLAYER_CTRL_CPU
    };

    /// <summary>
    /// Player info. The players are created by Scenario but managed
    /// by this module.
    /// </summary>
    public class Player
    {
        public string id;           /* identification */
        public string name;         /* real name */
        public PLAYERCONTROL ctrl;            /* controlled by human or CPU */
        public string ai_fname;     /* dll with AI routines */
        public int strat;           /* strategy: -2 very defensive to 2 very aggressive */
        public Nation[] nations;   /* list of pointers to nations controlled by this player */
        public int nation_count;   /* number of nations controlled */
        public List<Player> allies;   /* list of the player's allies */
        public int air_trsp_count; /* number of air transporters */
        public int sea_trsp_count; /* number of sea transporters */
        public Unit_Lib_Entry air_trsp; /* default air transporter */
        public Unit_Lib_Entry sea_trsp; /* default sea transporter */
        public int air_trsp_used;  /* number of air transporters in use */
        public int sea_trsp_used;  /* dito */
        public UnitLookingDirection orient;         /* inital orientation */
        public int prestige;       /* current amount of prestige (loaded from start_prestige) */
        public int prestige_per_turn; /* amount added in the beginning of each turn (including first one) */
        public bool no_init_deploy; /* whether player may initially deploy */

        /* ai callbacks loaded from module ai_fname */
        public AiInit ai_init;
        public AiRun ai_run;
        public AiFinalize ai_finalize;


        /*
        ====================================================================
        Player stuff
        ====================================================================
        */
        public static List<Player> players;
        public static int cur_player_id = 0;

        public Player()
        {
        }

        /*
        ====================================================================
        Add player to player list.
        ====================================================================
        */
        public static void player_add(Player player)
        {
            if (players == null)
                players = new List<Player>();
            players.Add(player);
        }
        /*
        ====================================================================
        Delete player entry. 
        ====================================================================
        */
        public static void player_delete(Player player)
        {
        }

        /*
        ====================================================================
        Delete all players.
        ====================================================================
        */
        public static void players_delete()
        {
            if (players != null)
                players.Clear();
            players = null;
        }


        /*
        ====================================================================
        Get very first player.
        ====================================================================
        */
        public static Player players_get_first()
        {
            int dummy;
            cur_player_id = -1;
            return players_get_next(out dummy);
        }

        /*
        ====================================================================
        Get next player in the cycle and also set new_turn true if all
        players have finished their turn action and it's time for 
        a new turn.
        ====================================================================
        */
        public static Player players_get_next(out int new_turn)
        {
            Player player;
            new_turn = 0;
            do
            {
                cur_player_id++;
                if (cur_player_id == players.Count)
                {
                    if (Engine.deploy_turn)
                        Engine.deploy_turn = false;
                    else
                        new_turn = 1;
                    cur_player_id = 0;
                }
                player = players[cur_player_id];
                //printf("Pl: %s,%d\n",player->name,player->no_init_deploy);
            } while (Engine.deploy_turn  && player.no_init_deploy);
            return player;
        }

        /*
        ====================================================================
        Check who would be the next player but do not choose him.
        ====================================================================
        */
        public static Player players_test_next()
        {
            if (cur_player_id + 1 == players.Count)
                return players[0];
            return players[cur_player_id + 1];
        }

        /*
        ====================================================================
        Set and get player as current by index.
        ====================================================================
        */
        public static Player players_set_current(int index)
        {
            if (index < 0) index = 0;
            if (index > players.Count) index = 0;
            cur_player_id = index;
            return players[cur_player_id];
        }

        /*
        ====================================================================
        Check if these two players are allies.
        ====================================================================
        */
        public static bool player_is_ally(Player player, Player second)
        {
            if (player == second || player == null) return true;
            foreach (Player ally in player.allies)
                if (ally == second)
                    return true;
            return false;
        }

        /*
        ====================================================================
        Get the player who controls nation
        ====================================================================
        */
        public static Player player_get_by_nation(Nation nation)
        {
            foreach (Player player in players)
            {
                for (int i = 0; i < player.nation_count; i++)
                    if (nation == player.nations[i])
                        return player;
            }
            return null;
        }

        /*
        ====================================================================
        Get player with this id string.
        ====================================================================
        */
        public static Player player_get_by_id(string id)
        {
            foreach (Player player in players)
            {
                if (player.id == id)
                    return player;
            }
            return null;
        }

        /*
        ====================================================================
        Get player with this index
        ====================================================================
        */
        public static Player player_get_by_index(int index)
        {
            return players[index];
        }

        /*
        ====================================================================
        Get player index in list
        ====================================================================
        */
        public static int player_get_index(Player player)
        {
            int i = 0;
            foreach (Player entry in players)
            {
                if (player == entry)
                    return i;
                i++;
            }
            return 0;
        }
    }
}
