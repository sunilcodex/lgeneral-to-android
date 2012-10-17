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
using UnityEngine;
using System.IO;
using DataFile;

namespace EngineA
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
        public static int nations_load(string fname){
			string path = "Assets/Resources/DB/"+fname;
			try{
				XmlSerializer SerializerObj = new XmlSerializer(typeof(Nation_DB_File));
		        // Create a new file stream for reading the XML file
		        FileStream ReadFileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		        // Load the object saved above by using the Deserialize function
		        Nation_DB_File nationDB = (Nation_DB_File)SerializerObj.Deserialize(ReadFileStream);
				ReadFileStream.Close();
				nationDB.nationDBTOnationatt();
				return 0;
			}
			catch (Exception e)
            {
                Debug.LogError("exception: " + e);
				return -1;
            }
            
		}

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
