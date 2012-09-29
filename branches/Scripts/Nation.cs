/*
 * Creado por SharpDevelop.
 * Usuario: asantos
 * Fecha: 09/01/2009
 * Hora: 16:33
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Engine
{
    /// <summary>
    /// A nation provides a full name and a flag icon for units
    /// (which belong to a specific nation).
    /// </summary>
    [Serializable]
    public class Nation
    {
        string id;
        string name;
        int flag_offset;


        /*
        ====================================================================
        Nations
        ====================================================================
        */
        [XmlIgnore]
        public static Nation[] nations;
        [XmlIgnore]
        public static int nation_count = 0;
        [XmlIgnore]
        public static SDL_Surface nation_flags;
        [XmlIgnore]
        public static int nation_flag_width = 0;
        [XmlIgnore]
        public static int nation_flag_height = 0;

        public string Name {
            get {
                return name;
            }
            set
            {
                name = value;
            }
        }

        public string ID
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }

        public int Flag_offset
        {
            get
            {
                return flag_offset;
            }
            set
            {
                flag_offset = value;
            }
        }

        /// <summary>
        /// Read nations from SRCDIR/nations/fname.
        /// </summary>
#if TODO_RR
        public static int nations_load(string fname)
        {
            string path = Util.MakeGamedirFile("nations", fname);
            try
            {
                Script pd = Script.CreateScript(path);
                /* icon size */
                nation_flag_width = int.Parse(pd.GetProperty("icon_width"));
                nation_flag_height = int.Parse(pd.GetProperty("icon_height"));
                /* icons */
                string str = pd.GetProperty("icons");
                path ="flags/" + str;
                nation_flags = SDL_Surface.LoadSurface(path, false);
                /* nations */
                List<Block> entries = pd.GetBlock("nations")[0].Blocks;
                nation_count = entries.Count;
                nations = new Nation[nation_count];
                int i = 0;
                foreach (Block sub in entries)
                {
                    nations[i] = new Nation();
                    nations[i].id = sub.Name;
                    nations[i].name = sub.GetProperty("name");
                    nations[i].flag_offset  = int.Parse(sub.GetProperty("icon_id"));
                    nations[i].flag_offset *= nation_flag_height;
                    i++;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("exception: " + e);
            }
            return 0;
        }
#endif
        /// <summary>
        /// Delete nations.
        /// </summary>
        public static void nations_delete()
        {
            throw new System.NotImplementedException();
        }


        /*
        ====================================================================
        Search for a nation by id string. If this fails 0 is returned.
        ====================================================================
        */
        public static Nation nation_find(string id)
        {
            int i;
            if (id == null) return null;
            for (i = 0; i < nation_count; i++)
                if (id == nations[i].id)
                    return nations[i];
            return null;
        }


        /*
        ====================================================================
        Get nation index (position in list)
        ====================================================================
        */
        public static int nation_get_index(Nation nation)
        {
            int i;
            for (i = 0; i < nation_count; i++)
                if (nation == nations[i])
                    return i;
            return 0;
        }

        /*
        ====================================================================
        Draw flag icon to surface.
          NATION_DRAW_FLAG_NORMAL: simply draw icon.
          NATION_DRAW_FLAG_OBJ:    add a golden frame to mark as military
                                   objective
        ====================================================================
        */
        public enum NationDrawFlag
        {
            NATION_DRAW_FLAG_NORMAL = 0,
            NATION_DRAW_FLAG_OBJ
        }
#if TODO_RR
        public static void nation_draw_flag(Nation nation, SDL_Surface surf, int x, int y, bool isObj)
        {
            if (isObj)
            {
                //SDL_Surface.copy_image(surf, x, y, nation_flag_width, nation_flag_height,
                //DEST(surf, x, y, nation_flag_width, nation_flag_height);
                //fill_surf(0xffff00);
                SDL_Surface.copy_image(surf, x + 1, y + 1, nation_flag_width - 2, nation_flag_height - 2,
                    nation_flags, 1, nation.flag_offset + 1);
            }
            else
            {
                SDL_Surface.copy_image(surf, x, y, nation_flag_width, nation_flag_height,
                                       nation_flags, 0, nation.flag_offset);
            }
        }
#endif

        /*
		====================================================================
		Get a specific pixel value in a nation's flag.
		====================================================================
		*/
#if TODO_RR
        public int nation_get_flag_pixel(Nation nation, int x, int y)
        {
            return SDL_Surface.GetPixel(nation_flags, x, nation.flag_offset + y);

        }
#endif
    }
}
