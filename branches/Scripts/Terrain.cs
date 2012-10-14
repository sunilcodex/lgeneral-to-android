/*
 * Creado por SharpDevelop.
 * Usuario: asantos
 * Fecha: 12/01/2009
 * Hora: 12:56
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using DataFile;
using System.IO;
using UnityEngine;

namespace Engine
{

    /// <summary>
    /// Weather flags.
    /// </summary>
    [Flags]
    public enum WEATHER_FLAGS
    {
        NONE = 0,
        NO_AIR_ATTACK = (1 << 1),    /* flying units can't and can't be attacked */
        DOUBLE_FUEL_COST = (1 << 2), /* guess what! */
        CUT_STRENGTH = (1 << 3),     /* cut strength in half due to bad weather */
        BAD_SIGHT = (1 << 4),        /* ranged attack is harder */
    }

	
    /// <summary>
    /// Weather type.
    /// </summary>
    [Serializable]
    public class Weather_Type
    {
        public string id;
        public string name;
        public WEATHER_FLAGS flags;
    }


    /// <summary>
    /// Fog alpha
    /// </summary>
    public enum FOG_ALPHA { FOG_ALPHA = 64, DANGER_ALPHA = 128 };


    /// <summary>
    /// Terrain flags
    /// </summary>
    [Flags]
    public enum Terrain_flags
    {
        NONE = 0,
        INF_CLOSE_DEF = (1 << 1), /* if there's a fight inf against non-inf
                                     on this terrain the non-inf unit must use
                                     it's close defense value */
        NO_SPOTTING = (1 << 2), /* you can't see on this tile except being on it
                                     or close to it */
        RIVER = (1 << 3), /* engineers can build a bridge over this tile */
        SUPPLY_AIR = (1 << 4), /* flying units may supply */
        SUPPLY_GROUND = (1 << 5), /* ground units may supply */
        SUPPLY_SHIPS = (1 << 6), /* swimming units may supply */
        BAD_SIGHT = (1 << 7), /* ranged attack is harder */
        SWAMP = (1 << 8)  /* attack penalty */
    }

	
    /// <summary>
    /// Terrain type.
    /// In this case the id is a single character used to determine the
    /// terrain type in a map.
    /// mov, spt and flags may be different for each weather type
    /// </summary>
    [Serializable]
    public class Terrain_Type
    {
        public string id;
        [XmlIgnore]
        public string name;
        [XmlIgnore]
        public SDL_Surface[] images;
        [XmlIgnore]
        public SDL_Surface[] images_fogged;
        [XmlIgnore]
        public int[,] mov; //mov cost is array [ mov_type, weatherType ]
        [XmlIgnore]
        public int[] spt; // spot cost is array [ weatherType ]
        [XmlIgnore]
        public int min_entr;
        [XmlIgnore]
        public int max_entr;
        [XmlIgnore]
        public int max_ini;
        [XmlIgnore]
        public Terrain_flags[] flags; // flags is array [ weatherType ]
    }

    /// <summary>
    /// Terrain icons
    /// </summary>
    public class Terrain_Icons
    {
        public SDL_Surface fog;       /* mask used to create fog */
        public SDL_Surface danger;    /* mask used to create danger zone */
        public SDL_Surface grid;      /* contains the grid */
        public SDL_Surface selectP;    /* selecting frame picture */
        public SDL_Surface cross;     /* crosshair animation */
        public Anim expl1, expl2;     /* explosion animation (attacker, defender)*/
#if WITH_SOUND
    Wav *wav_expl;   /* explosion */
    Wav *wav_select; /* tile selection */
#endif
    }
	[Serializable]
    public class Terrain
    {

        /*
        ====================================================================
        Load terrain types, weather information and hex tile icons.
        ====================================================================
        */
        public int Load(string fname)
        {
			try{
				
	            int mov_type_count = DB.UnitLib.mov_type_count;
	            Mov_Type[] mov_types = DB.UnitLib.mov_types;
				string path = "Assets/Resources/DB/"+fname;
				XmlSerializer SerializerObj = new XmlSerializer(typeof(TerrainDB));
	        	// Create a new file stream for reading the XML file
	        	FileStream ReadFileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
	        	// Load the object saved above by using the Deserialize function
	       	 	TerrainDB terrainDB = (TerrainDB)SerializerObj.Deserialize(ReadFileStream);
	        	// Cleanup
	        	ReadFileStream.Close();
				/* get weather */
				this.weatherTypes = terrainDB.weatherTypes;
				this.weatherTypeCount = terrainDB.weatherTypeCount;
				if (terrainDB.weatherTypes.Length == 0)
	            {
	                this.weatherTypeCount = 1;
	                this.weatherTypes = new Weather_Type[weatherTypeCount];
	            }
				/* terrain icons */
	            this.terrainIcons = new Terrain_Icons();
	            if (terrainDB.fog != null)
	            {
	                this.terrainIcons.fog = SDL_Surface.LoadSurface(terrainDB.fog, true);
	            }
	            if (terrainDB.danger != null)
	            {
	                this.terrainIcons.danger = SDL_Surface.LoadSurface(terrainDB.danger, true);
	            }
	            if (terrainDB.grid != null)
	            {
	                this.terrainIcons.grid = SDL_Surface.LoadSurface(terrainDB.grid, true);
	            }
	            if (terrainDB.selectP != null)
	            {
	                this.terrainIcons.selectP = SDL_Surface.LoadSurface(terrainDB.selectP, true);
	            }
	            if (terrainDB.cross!= null)
	            {
	                terrainIcons.cross = SDL_Surface.LoadSurface(terrainDB.cross, true);
	            }
				/* terrain types */
				if (terrainDB.terrainTypeCount==0){
					this.terrainTypeCount = 1;
					this.terrainTypes = new Terrain_Type[this.terrainTypeCount];
				}
				this.terrainTypeCount = terrainDB.terrainTypeCount;
				this.terrainTypes = new Terrain_Type[this.terrainTypeCount];
				for (int i=0; i<this.terrainTypeCount; i++){
					this.terrainTypes[i] = new Terrain_Type();
					/* id */
					this.terrainTypes[i].id = terrainDB.terrainTypes[i].id;
					/* name */
					this.terrainTypes[i].name = terrainDB.terrainTypes[i].name;
					/* each weather type got its own image -- if it's named 'default' we
                    point towards the image of weather_type 0 */
                	terrainTypes[i].images = new SDL_Surface[weatherTypeCount];
					for (int j = 0; j < weatherTypeCount; j++)
                	{
						terrainTypes[i].images[j] = 
							SDL_Surface.LoadSurface(terrainDB.terrainTypes[i].images_name[j], true);
					}
#if TODO_RR
				/* fog image */
                terrainTypes[i].images_fogged = new SDL_Surface[weatherTypeCount];
                for (int j = 0; j < weatherTypeCount; j++)
                {
                    if (terrainTypes[i].images[j] == terrainTypes[i].images[0] && j > 0)
                    {
                        /* just a pointer */
                        terrainTypes[i].images_fogged[j] = terrainTypes[i].images_fogged[0];
                    }
                    else
                    {
                        terrainTypes[i].images_fogged[j] = SDL_Surface.CreateSurface(terrainTypes[i].images[j].w, terrainTypes[i].images[j].h, true);
                        SDL_Surface.full_copy_image(terrainTypes[i].images_fogged[j], terrainTypes[i].images[j], 0, 0);
                        int count = terrainTypes[i].images[j].w / hex_w;
                        for (int k = 0; k < count; k++)
                        {
                            SDL_Surface.copy_image(terrainTypes[i].images_fogged[j], k * hex_w, 0, hex_w, hex_h,
                                                    terrainIcons.fog, 0, 0, (int)FOG_ALPHA.FOG_ALPHA);
                            //alpha_blit_surf(FOG_ALPHA);
                        }
                        SDL_Surface.SDL_SetColorKey(terrainTypes[i].images_fogged[j], SDL_Surface.GetPixel(terrainTypes[i].images[j], 0, 0));
                    }
                }
#endif
					/* spot cost */
                	this.terrainTypes[i].spt = terrainDB.terrainTypes[i].spt;
					/* mov cost */
                	this.terrainTypes[i].mov = terrainDB.terrainTypes[i].mov;
					 /* entrenchment */
	                this.terrainTypes[i].min_entr = terrainDB.terrainTypes[i].min_entr;
	                this.terrainTypes[i].max_entr = terrainDB.terrainTypes[i].max_entr;
	                /* initiative modification */
	                this.terrainTypes[i].max_ini = terrainDB.terrainTypes[i].max_ini;
					/* flags */
					this.terrainTypes[i].flags = terrainDB.terrainTypes[i].flags;
				}
            	return 1;
			} 
			catch(Exception e)
			{
				Debug.LogError("exception: "+ e.Message);
				return -1;
			}
        }

        /*
        ====================================================================
        Delete terrain types & co
        ====================================================================
        */
        public void terrain_delete()
        {
            throw new System.NotImplementedException();
        }

        /*
        ====================================================================
        Get the movement cost for a terrain type by passing movement
        type id and weather id in addition.
        Return -1 if all movement points are consumed
        Return 0  if movement is impossible
        Return cost else.
        ====================================================================
        */
        public static int GetMovementCost(Terrain_Type type, int mov_type, int weather)
        {
            return type.mov[weather, mov_type];
        }


        /*
       ====================================================================
       Flag conversion table
       ====================================================================
       */
#if TODO
private StrToFlag[] fct_terrain = new StrToFlag[]{
    { "no_air_attack", WEATHER_FLAGS.NO_AIR_ATTACK },         
    { "double_fuel_cost", WEATHER_FLAGS.DOUBLE_FUEL_COST },   
    { "supply_air", SUPPLY_AIR },               
    { "supply_ground", SUPPLY_GROUND },         
    { "supply_ships", SUPPLY_SHIPS },           
    { "river", RIVER },
    { "no_spotting", NO_SPOTTING },             
    { "inf_close_def", INF_CLOSE_DEF },         
    { "cut_strength", WEATHER_FLAGS.CUT_STRENGTH },
    { "bad_sight", WEATHER_FLAGS.BAD_SIGHT },
    { "swamp", SWAMP },
    { "X", 0}    
};
#endif


        /*
        ====================================================================
        Terrain types & co
        ====================================================================
        */
        public Terrain_Type[] terrainTypes;
		[XmlIgnore]
        public int terrainTypeCount;
		[XmlIgnore]
        public Weather_Type[] weatherTypes;
		[XmlIgnore]
        public int weatherTypeCount;
		[XmlIgnore]
        public Terrain_Icons terrainIcons;
    }
}
