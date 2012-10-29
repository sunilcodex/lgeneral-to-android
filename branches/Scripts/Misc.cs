/*
 * Creado por SharpDevelop.
 * Usuario: asantos
 * Fecha: 12/01/2009
 * Hora: 13:38
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;

namespace Miscellaneous
{
	using EngineA;

    /// <summary>
    /// Description of Misc.
    /// </summary>
    public class Misc
    {
        private static System.Random rand;
        public static int DICE(int maxeye)
        {
            return rand.Next(1, maxeye);
        }

        public static int RANDOM(int lower, int upper)
        {
            return rand.Next(lower, upper);
        }

        /// <summary>
        /// Init random seed by using a time-dependent default seed value 
        /// The default seed value is derived from the system
        /// clock and has finite resolution. 
        /// </summary>
        public static void set_random_seed()
        {
            rand = new Random();
        }

        /* get coordinates from string */
        public static void get_coord(string str, out int x, out int y)
        {
            int i;
            string cur_arg;

            x = y = 0;

            /* get position of comma */
            for (i = 0; i < str.Length; i++)
                if (str[i] == ',') break;
            if (i == str.Length)
            {
                Console.WriteLine("get_coord: no comma found in pair of coordinates '%s'\n", str);
                return; /* no comma found */
            }

            /* y */
            cur_arg = str.Substring(i + 1);
            if (cur_arg[0] == 0)
                Console.WriteLine("get_coord: warning: y-coordinate is empty (maybe you left a space between x and comma?)\n");
            y = int.Parse(cur_arg);
            /* x */
            cur_arg = str;
            x = int.Parse(cur_arg);
        }
        /*
        ====================================================================
        Get neighbored tile coords clockwise with id between 0 and 5.
        ====================================================================
        */
        public static bool get_close_hex_pos(int x, int y, int id, out int dest_x, out int dest_y)
        {
            dest_x = 0;
            dest_y = 0;

            if (id == 0)
            {
                dest_x = x;
                dest_y = y - 1;
            }
            else
                if (id == 1)
                {
                    if ((x & 1) != 0)
                    {
                        dest_x = x + 1;
                        dest_y = y;
                    }
                    else
                    {
                        dest_x = x + 1;
                        dest_y = y - 1;
                    }
                }
                else
                    if (id == 2)
                    {
                        if ((x & 1) != 0)
                        {
                            dest_x = x + 1;
                            dest_y = y + 1;
                        }
                        else
                        {
                            dest_x = x + 1;
                            dest_y = y;
                        }
                    }
                    else
                        if (id == 3)
                        {
                            dest_x = x;
                            dest_y = y + 1;
                        }
                        else
                            if (id == 4)
                            {
                                if ((x & 1) != 0)
                                {
                                    dest_x = x - 1;
                                    dest_y = y + 1;
                                }
                                else
                                {
                                    dest_x = x - 1;
                                    dest_y = y;
                                }
                            }
                            else
                                if (id == 5)
                                {
                                    if ((x & 1) != 0)
                                    {
                                        dest_x = x - 1;
                                        dest_y = y;
                                    }
                                    else
                                    {
                                        dest_x = x - 1;
                                        dest_y = y - 1;
                                    }
                                }
            if (dest_x <= 0 || dest_y <= 0 || dest_x >= Engine.map.map_w - 1 ||
                                    dest_y >= Engine.map.map_h - 1)
                return false;
            return true;
        }


        /* Convert grid coordinates into isometric (diagonal) coordinates. */
        public static void convert_coords_to_diag(ref int x, ref int y)
        {
            y += (x + 1) / 2;
        }

        /* return distance between to map positions */
        public static int get_dist(int x0, int y0, int x1, int y1)
        {
            int dx, dy;
            convert_coords_to_diag(ref x0, ref y0);
            convert_coords_to_diag(ref x1, ref y1);
            dx = Math.Abs(x1 - x0);
            dy = Math.Abs(y1 - y0);
            if ((y1 <= y0 && x1 >= x0) || (y1 > y0 && x1 < x0))
                return dx + dy;
            else if (dx > dy)
                return dx;
            else
                return dy;
        }

        /* check if number is odd or even */
        public static bool ODD(int x) { return (x & 1) != 0; }
        public static bool EVEN(int x) { return ((x & 1) == 0); }

        /*
        ====================================================================
        Check if these positions are adjacent to each other.
        ====================================================================
        */
        public static bool is_close(int x1, int y1, int x2, int y2)
        {
            int next_x, next_y;
            if (x1 == x2 && y1 == y2) return true;
            for (int i = 0; i < 6; i++)
                if (get_close_hex_pos(x1, y1, i, out next_x, out next_y))
                    if (next_x == x2 && next_y == y2)
                        return true;
            return false;
        }

		public static bool IsEven (int number)
	{
		if (number % 2 == 0) {
			return true;
		} else {
			return false;
		}
	}
    }
}
