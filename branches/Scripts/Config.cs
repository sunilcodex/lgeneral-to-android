using System;
using System.IO;

namespace Miscellaneous
{
    public class Config
    {
        public static int schedulerTimeOut = 20;

        /* directory to save config and saved games */
        public static string dir_name = ".";
#if TODO_RR
        public static string data_dir_name = ".." + Util.DirSep + "data";
#endif

        /* gfx options */
        public static bool grid; /* hex grid */
        public static int tran; /* transparancy */
        public static bool show_bar = true; /* show unit's life bar and icons */
        public static int width, height, fullscreen;
        public static int anim_speed; /* scale animations by this factor: 1=normal, 2=doubled, ... */

        /* game options */
        public static bool supply = true; /* units must supply */
        public static bool weather = true; /* does weather have influence? */
#if DEBUG
        public static bool fog_of_war = false; /* guess what? */
#else
        public static bool fog_of_war = true; /* guess what? */
#endif
        public static bool show_cpu_turn = true;
        public static bool deploy = true; /* allow deployment */

        /* audio stuff */
        public static int sound_on;
        public static int sound_volume;
        public static int music_on;
        public static int music_volume;
		public static int hex_w = 60;
		public static int hex_h = 50;
		public static int hex_x_offset = 45;
		public static int hex_y_offset = 25;

#if TODO_RR
        public static void check_config_dir_name()
        {
            DirectoryInfo dir = new DirectoryInfo(Config.data_dir_name);
            if (dir == null)
            {
                Console.WriteLine("Config: can't open directory '{0}' to read data", Config.data_dir_name);
                return;
            }
            Util.SetGamedir(dir.FullName);
        }
#endif
		
        /* set config to default */
        public static void reset_config()
        {
            /* gfx options */
            tran = 1;
            grid = false;
            show_bar = true;
            width = 640;
            height = 480;
            fullscreen = 0;
            anim_speed = 1;
            /* game options */
            supply = true;
            weather = true;
            fog_of_war = true;
            show_cpu_turn = true;
            deploy = true;
            /* audio stuff */
            sound_on = 1;
            sound_volume = 96;
            music_on = 1;
            music_volume = 96;
        }

        /* load config */
        public static void load_config()
        {
        }

        /* save config */
        public static void save_config()
        {
        }
    }
}
