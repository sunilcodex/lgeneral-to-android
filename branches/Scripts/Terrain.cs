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
	public class Weather_Type
	{
		public string id;
		public string name;
		public WEATHER_FLAGS flags;
	}


	/// <summary>
	/// Fog alpha
	/// </summary>
	public enum FOG_ALPHA
	{
		FOG_ALPHA = 64,
		DANGER_ALPHA = 128
	};


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
	public class Terrain_Type
	{
		public string id;
		public string name;
		//TODO_RR public SDL_Surface[] images;
		//TODO_RR public SDL_Surface[] images_fogged;
		public int[,] mov; //mov cost is array [ mov_type, weatherType ]
		public int[] spt; // spot cost is array [ weatherType ]
		public int min_entr;
		public int max_entr;
		public int max_ini;
		public Terrain_flags[] flags; // flags is array [ weatherType ]
	}


	/// <summary>
	/// Terrain icons
	/// </summary>
	public class Terrain_Icons
	{
		//TODO_RR public SDL_Surface fog;       /* mask used to create fog */
		//TODO_RR public SDL_Surface danger;    /* mask used to create danger zone */
		//TODO_RR public SDL_Surface grid;      /* contains the grid */
		//TODO_RR public SDL_Surface select;    /* selecting frame picture */
		//TODO_RR public SDL_Surface cross;     /* crosshair animation */
		//TODO_RR public Anim expl1, expl2;     /* explosion animation (attacker, defender)*/
#if WITH_SOUND
    Wav *wav_expl;   /* explosion */
    Wav *wav_select; /* tile selection */
#endif
	}

	public class Terrain
	{

		/*
        ====================================================================
        Load terrain types, weather information and hex tile icons.
        ====================================================================
        */
#if TODO_RR
        public int Load(string fname)
        {
            int mov_type_count = DB.UnitLib.mov_type_count;
            Mov_Type[] mov_types = DB.UnitLib.mov_types;

            string path = Util.MakeGamedirFile("maps", fname);
            Script script = Script.CreateScript(path);
            /* get weather */
            List<Block> entries = script.GetBlock("weather");
            if (entries.Count == 1)
            {
                weatherTypeCount = entries[0].Blocks.Count;
                weatherTypes = new Weather_Type[weatherTypeCount];
            }
            int i = 0;
            foreach (Block sub in entries[0].Blocks)
            {
                weatherTypes[i] = new Weather_Type();
                weatherTypes[i].id = sub.Name;
                weatherTypes[i].name = sub.GetProperty("name");
                string flags = sub.GetProperty("flags");
                foreach (string flag in flags.Split('°'))
                {
                    weatherTypes[i].flags |= (WEATHER_FLAGS)System.Enum.Parse(typeof(WEATHER_FLAGS), flag.ToUpper());
                }
                i++;
            }

            /* hex tile geometry */
            hex_w = int.Parse(script.GetProperty("hex_width"));
            hex_h = int.Parse(script.GetProperty("hex_height"));
            hex_x_offset = int.Parse(script.GetProperty("hex_x_offset"));
            hex_y_offset = int.Parse(script.GetProperty("hex_y_offset"));

            /* terrain icons */
            terrainIcons = new Terrain_Icons();
            string str = script.GetProperty("fog");
            if (str != null)
            {
                path = "terrain/" + str;
                terrainIcons.fog = SDL_Surface.LoadSurface(path, true); //SDL_SWSURFACE);
            }
            str = script.GetProperty("danger");
            if (str != null)
            {
                path = "terrain/" + str;
                terrainIcons.danger = SDL_Surface.LoadSurface(path, true); //SDL_SWSURFACE);
            }
            str = script.GetProperty("grid");
            if (str != null)
            {
                path = "terrain/" + str;
                terrainIcons.grid = SDL_Surface.LoadSurface(path, true); //SDL_SWSURFACE);
            }
            str = script.GetProperty("frame");
            if (str != null)
            {
                path = "terrain/" + str;
                terrainIcons.select = SDL_Surface.LoadSurface(path, true); //SDL_SWSURFACE);
            }
            str = script.GetProperty("crosshair");
            if (str != null)
            {
                path = "terrain/" + str;
                terrainIcons.cross = SDL_Surface.LoadSurface(path, true);
            }
            /*
           if (!parser_get_value(pd, "crosshair", &str, 0)) goto parser_failure;
           sprintf(path, "terrain/%s", str);
           if ((terrainIcons->cross = anim_create(LoadSurface(path, SDL_SWSURFACE), 1000 / config.anim_speed, hex_w, hex_h, sdl.screen, 0, 0)) == 0)
               goto failure;
           anim_hide(terrainIcons->cross, 1);
           if (!parser_get_value(pd, "explosion", &str, 0)) goto parser_failure;
           sprintf(path, "terrain/%s", str);
           if ((terrainIcons->expl1 = anim_create(LoadSurface(path, SDL_SWSURFACE), 50 / config.anim_speed, hex_w, hex_h, sdl.screen, 0, 0)) == 0)
               goto failure;
           anim_hide(terrainIcons->expl1, 1);
           if ((terrainIcons->expl2 = anim_create(LoadSurface(path, SDL_SWSURFACE), 50 / config.anim_speed, hex_w, hex_h, sdl.screen, 0, 0)) == 0)
               goto failure;
           anim_hide(terrainIcons->expl2, 1);
           */
            /* terrain types */
            entries = script.GetBlock("terrain");
            if (entries.Count == 1)
            {
                terrainTypeCount = entries[0].Blocks.Count;
                terrainTypes = new Terrain_Type[terrainTypeCount];
            }
            i = 0;
            foreach (Block sub in entries[0].Blocks)
            {
                terrainTypes[i] = new Terrain_Type();
                /* id */
                terrainTypes[i].id = sub.Name;
                /* name */
                terrainTypes[i].name = sub.GetProperty("name");
                /* each weather type got its own image -- if it's named 'default' we
                    point towards the image of weather_type 0 */
                terrainTypes[i].images = new SDL_Surface[weatherTypeCount];
                for (int j = 0; j < weatherTypeCount; j++)
                {
                    path = sub.GetBlock("image")[0].GetProperty(weatherTypes[j].id);
                    if (("default" == path) && j > 0)
                    {
                        /* just a pointer */
                        terrainTypes[i].images[j] = terrainTypes[i].images[0];
                    }
                    else
                    {
                        path = "terrain/" + path;
                        terrainTypes[i].images[j] = SDL_Surface.LoadSurface(path, true);
                        //TODO
                        //SDL_SetColorKey(terrainTypes[i].images[j], SDL_SRCCOLORKEY,
                        //                 GetPixel(terrainTypes[i].images[j], 0, 0));
                    }
                }
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
                /* spot cost */
                terrainTypes[i].spt = new int[weatherTypeCount];
                Block subsub = sub.GetBlock("spot_cost")[0];
                for (int j = 0; j < weatherTypeCount; j++)
                    terrainTypes[i].spt[j] = int.Parse(subsub.GetProperty(weatherTypes[j].id));
                /* mov cost */
                terrainTypes[i].mov = new int[weatherTypeCount, mov_type_count];
                subsub = sub.GetBlock("move_cost")[0];
                for (int k = 0; k < mov_type_count; k++)
                {
                    Block subsubsub = subsub.GetBlock(mov_types[k].id)[0];
                    for (int j = 0; j < weatherTypeCount; j++)
                    {
                        str = subsubsub.GetProperty(weatherTypes[j].id);
                        if (str[0] == 'X')
                            terrainTypes[i].mov[j, k] = 0; /* impassable */
                        else
                            if (str[0] == 'A')
                                terrainTypes[i].mov[j, k] = -1; /* costs all */
                            else
                                terrainTypes[i].mov[j, k] = int.Parse(str); /* normal cost */
                    }
                }
                /* entrenchment */
                terrainTypes[i].min_entr = int.Parse(sub.GetProperty("min_entr"));
                terrainTypes[i].max_entr = int.Parse(sub.GetProperty("max_entr"));
                /* initiative modification */
                terrainTypes[i].max_ini = int.Parse(sub.GetProperty("max_init"));
                /* flags */

                terrainTypes[i].flags = new Terrain_flags[weatherTypeCount];
                subsub = sub.GetBlock("flags")[0];
                for (int j = 0; j < weatherTypeCount; j++)
                {
                    string flags = subsub.GetProperty(weatherTypes[j].id);
                    foreach (string flag in flags.Split('°'))
                    {
                        terrainTypes[i].flags[j] |= (Terrain_flags)System.Enum.Parse(typeof(Terrain_flags), flag.ToUpper());
                    }
                }
                /* next terrain */
                i++;
            }
            return 1;
        }
#endif
		/*
        ====================================================================
        Delete terrain types & co
        ====================================================================
        */
		public void terrain_delete ()
		{
			throw new System.NotImplementedException ();
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
		public static int GetMovementCost (Terrain_Type type, int mov_type, int weather)
		{
			return type.mov [weather, mov_type];
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
        Geometry of a hex tile
        ====================================================================
        */
		public int hex_w, hex_h;
		public int hex_x_offset, hex_y_offset;

		/*
        ====================================================================
        Terrain types & co
        ====================================================================
        */
		public Terrain_Type[] terrainTypes;
		public int terrainTypeCount;
		public Weather_Type[] weatherTypes;
		public int weatherTypeCount;
		public Terrain_Icons terrainIcons;
	}
}
