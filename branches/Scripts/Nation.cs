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
		
#if TODO_RR
		public static void nationstofile(){
			for (int i=0; i<nations.Length;i++){
				Debug.Log(nations[i].name+" "+nations[i].flag_offset);
				SDL_Surface dest = new SDL_Surface();
				SDL_Surface.copy_image(dest,Nation.nation_flag_width,
					Nation.nation_flag_height,Nation.nation_flags,0,
					(Nation.nation_flags.h-nations[i].flag_offset-Nation.nation_flag_height),false);
				byte[] bytes = dest.bitmap.EncodeToPNG();
				File.WriteAllBytes(Application.dataPath + "/../Imagenes/"+nations[i].name+".png", bytes);
			}
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

        public static void nation_draw_flag(Nation nation, SDL_Surface surf)
        {
			if (surf==null){
				throw new Exception("the Surface is null");
			}
            SDL_Surface.copy_image(surf,Nation.nation_flags,20,1,Nation.nation_flag_width,
									Nation.nation_flag_height,0,(Nation.nation_flags.h-nation.flag_offset-Nation.nation_flag_height));
        }

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
