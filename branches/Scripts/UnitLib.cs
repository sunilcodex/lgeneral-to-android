/*
 * Creado por SharpDevelop.
 * Usuario: asantos
 * Fecha: 09/01/2009
 * Hora: 16:19
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Collections.Generic;

namespace Engine
{

    /// <summary>
    /// Unit flags.
    /// </summary>
    [Flags]
    public enum UnitFlags
    {
        SWIMMING = (1 << 1),   /* ship */
        DIVING = (1 << 2),   /* aircraft */
        FLYING = (1 << 3),   /* submarine */
        PARACHUTE = (1 << 4),   /* may air debark anywhere */
        TRANSPORTER = (1 << 7),   /* is a transporter */
        RECON = (1 << 8),   /* multiple movements a round */
        ARTILLERY = (1 << 9),   /* defensive fire */
        INTERCEPTOR = (1 << 10),  /* protects close bombers */
        AIR_DEFENSE = (1 << 11),  /* defensive fire */
        BRIDGE_ENG = (1 << 12),  /* builds a bridge over rivers */
        INFANTRY = (1 << 13),  /* is infantry */
        AIR_TRSP_OK = (1 << 14),  /* may use air transporter */
        DESTROYER = (1 << 15),  /* may attack submarines */
        IGNORE_ENTR = (1 << 16),  /* ignore entrenchment of target */
        CARRIER = (1 << 17),  /* aircraft carrier */
        CARRIER_OK = (1 << 18),  /* may supply at aircraft carrier */
        BOMBER = (1 << 19),  /* receives protection by interceptors */
        ATTACK_FIRST = (1 << 20),  /* unit looses it's attack when moving */
        LOW_ENTR_RATE = (1 << 21),  /* entrenchment rate is 1 */
        TANK = (1 << 22),  /* is a tank */
        ANTI_TANK = (1 << 23),  /* anti-tank (bonus against tanks) */
        SUPPR_FIRE = (1 << 24),  /* unit primarily causes suppression when firing */
        TURN_SUPPR = (1 << 25),  /* causes lasting suppression */
        JET = (1 << 26),  /* airplane is a jet */
    }

    public class Trgt_Type
    {
        public string id;
        public string name;
    }

    public class Mov_Type
    {
        public string id;
        public string name;
#if WITH_SOUND    
	    public Wav wav_move;
#endif
    }

    public class Unit_Class
    {
        public string id;
        public string name;
    }


    /// <summary>
    /// Unit map tile info icons (strength, move, attack ...)
    /// </summary>
    public class Unit_Info_Icons
    {
        public int str_w, str_h;
        public SDL_Surface str;
        public SDL_Surface atk;
        public SDL_Surface mov;
        public SDL_Surface guard;
    }


    /// <summary>
    /// Unit icon styles.
    /// 	SINGLE:   unit looks left and is mirrored to look right
    ///		ALL_DIRS: unit provides an icon (horizontal arranged) for each
    ///               looking direction.
    /// </summary>
    public enum UnitIconStyle
    {
        UNIT_ICON_SINGLE = 0,
        UNIT_ICON_ALL_DIRS
    }

    /*
    ====================================================================
    Unit lib entry.
    ====================================================================
    */
    public class Unit_Lib_Entry
    {

        /*
        ====================================================================
        As we allow unit merging it must be possible to modify a shallow
        copy of Unit_Lib_Entry (id, name, icons, sounds are kept). This 
        means to have attacks as a pointer is quite bad. So we limit this
        array to a maxium target type count.
        ====================================================================
        */
        public static int TARGET_TYPE_LIMIT = 10;



        public string id;       /* identification of this entry */
        public string name;     /* name */
        public int unit_class;      /* unit class */
        public int trgt_type;  /* target type */
        public int ini;        /* inititative */
        public int mov;        /* movement */
        public int mov_type;   /* movement type */
        public int spt;        /* spotting */
        public int rng;        /* attack range */
        public int atk_count;  /* number of attacks per turn */
        public int[] atks; /* attack values (number is determined by global target_type_count) */
        public int def_grnd;   /* ground defense (non-flying units) */
        public int def_air;    /* air defense */
        public int def_cls;    /* close defense (infantry against non-infantry) */
        public int entr_rate;  /* default is 2, if flag LOW_ENTR_RATE is set it's only 1 and
                       if INFANTRY is set it's 3, this modifies the rugged defense
                       chance */
        public int ammo;       /* max ammunition */
        public int fuel;       /* max fuel (0 if not used) */
        public SDL_Surface icon;      /* tactical icon */
        public SDL_Surface icon_tiny; /* half the size; used to display air and ground unit at one tile */
        public UnitIconStyle icon_type;          /* either single or all_dirs */
        public int icon_w, icon_h;     /* single icon size */
        public int icon_tiny_w, icon_tiny_h; /* single icon size */
        public UnitFlags flags;
        public int start_year, start_month, end_year; /* time of usage */
#if WITH_SOUND
    int wav_alloc;          /* if this flag is set wav_move must be freed else it's a pointer */
    Wav *wav_move;  /* pointer to the unit class default sound if wav_alloc is not set */
#endif
        public int eval_score; /* between 0 - 1000 indicating the worth of the unit relative the
                       best one */


        /*
        ====================================================================
        Load a unit library. If UNIT_LIB_MAIN is passed target_types,
        mov_types and unit classes will be loaded (may only happen once)
        ====================================================================
        */
        public enum UNIT_LOAD
        {
            UNIT_LIB_ADD = 0,
            UNIT_LIB_MAIN
        };
#if TODO_RR
        public int UnitLibLoad(string fname, UNIT_LOAD main)
        {
            string str;

            /* there can be only one main library */
            if ((main == UNIT_LOAD.UNIT_LIB_MAIN) && unit_lib_main_loaded)
            {
                Console.WriteLine(fname + ": can't load as main unit library (which is already loaded): loading as 'additional' instead\n");
                main = UNIT_LOAD.UNIT_LIB_ADD;
            }

            try
            {
                string path = Util.MakeGamedirFile("units", fname);
                Script script = Script.CreateScript(path);
                if (main == UNIT_LOAD.UNIT_LIB_MAIN)
                {

                    /* target types */
                    List<Block> listblocks = script.GetBlock("target_types");

                    trgt_types = new Trgt_Type[listblocks[0].Blocks.Count];
                    int i = 0;
                    foreach (Block sub in listblocks[0].Blocks)
                    {
                        trgt_types[i] = new Trgt_Type();
                        trgt_types[i].id = sub.Name;
                        trgt_types[i].name = sub.GetProperty("name");
                        i++;
                    }
                    trgt_type_count = i;

                    /* movement types */
                    listblocks = script.GetBlock("move_types");
                    mov_types = new Mov_Type[listblocks[0].Blocks.Count];
                    mov_type_count = 0;
                    foreach (Block sub in listblocks[0].Blocks)
                    {
                        mov_types[mov_type_count] = new Mov_Type();
                        mov_types[mov_type_count].id = sub.Name;
                        mov_types[mov_type_count].name = sub.GetProperty("name");

#if WITH_SOUND   
                    string str = sub.GetProperty("sound");
                    if (str != null)
                    {
                        mov_types[mov_type_count].wav_move = wav_load(str, 0);
                    }
#endif
                        mov_type_count++;
                    }

                    /* unit classes */
                    listblocks = script.GetBlock("unit_classes");
                    unit_classes = new Unit_Class[listblocks[0].Blocks.Count];

                    unit_class_count = 0;
                    foreach (Block sub in listblocks[0].Blocks)
                    {
                        unit_classes[unit_class_count] = new Unit_Class();
                        unit_classes[unit_class_count].id = sub.Name;
                        unit_classes[unit_class_count++].name = sub.GetProperty("name");
                        /* ignore sounds so far */
                    }

                    /* unit map tile icons */
                    unit_info_icons = new Unit_Info_Icons();
                    path = "units/" + script.GetProperty("strength_icons");
                    unit_info_icons.str = SDL_Surface.LoadSurface(path, true);
                    unit_info_icons.str_w = int.Parse(script.GetProperty("strength_icon_width"));
                    unit_info_icons.str_h = int.Parse(script.GetProperty("strength_icon_height"));

                    str = script.GetProperty("attack_icon");
                    path = "units/" + str;
                    unit_info_icons.atk = SDL_Surface.LoadSurface(path, true);
                    str = script.GetProperty("move_icon");
                    path = "units/" + str;
                    unit_info_icons.mov = SDL_Surface.LoadSurface(path, true);
                    str = script.GetProperty("guard_icon");
                    if (string.IsNullOrEmpty(str))
                        str = "pg_guard.bmp";
                    path = "units/" + str;
                    unit_info_icons.guard = SDL_Surface.LoadSurface(path, true);

                }
                /* icons */
                str = script.GetProperty("icon_type");
                if (str == "single")
                    icon_type = UnitIconStyle.UNIT_ICON_SINGLE;
                else
                    icon_type = UnitIconStyle.UNIT_ICON_ALL_DIRS;
                str = script.GetProperty("icons");
                path = "units/" + str;
                Console.WriteLine("  Loading Tactical Icons");
                SDL_Surface icons = SDL_Surface.LoadSurface(path, true);
                /* unit classes */
                List<Block> unit_lib_block = script.GetBlock("unit_lib");
                foreach (Block sub in unit_lib_block[0].Blocks)
                {
                    Unit_Lib_Entry unit = new Unit_Lib_Entry();
                    /* identification */
                    unit.id = sub.Name;
                    /* name */
                    unit.name = sub.GetProperty("name");
                    /* class id */
                    unit.unit_class = 0;
                    str = sub.GetProperty("class");
                    for (int i = 0; i < unit_class_count; i++)
                        if (str == unit_classes[i].id)
                        {
                            unit.unit_class = i;
                            break;
                        }

                    /* target type id */
                    unit.trgt_type = 0;
                    str = sub.GetProperty("target_type");
                    for (int i = 0; i < trgt_type_count; i++)
                        if (str == trgt_types[i].id)
                        {
                            unit.trgt_type = i;
                            break;
                        }
                    /* initiative */
                    unit.ini = int.Parse(sub.GetProperty("initiative"));
                    /* spotting */
                    unit.spt = int.Parse(sub.GetProperty("spotting"));
                    /* movement */
                    unit.mov = int.Parse(sub.GetProperty("movement"));
                    /* move type id */
                    unit.mov_type = 0;
                    str = sub.GetProperty("move_type");
                    for (int i = 0; i < mov_type_count; i++)
                        if (str == mov_types[i].id)
                        {
                            unit.mov_type = i;
                            break;
                        }
                    /* fuel */
                    unit.fuel = int.Parse(sub.GetProperty("fuel"));
                    /* range */
                    unit.rng = int.Parse(sub.GetProperty("range"));
                    /* ammo */
                    unit.ammo = int.Parse(sub.GetProperty("ammo"));
                    Block attacks = sub.GetBlock("attack")[0];
                    /* attack count */
                    unit.atk_count = int.Parse(attacks.GetProperty("count"));
                    unit.atks = new int[trgt_type_count];
                    /* attack values */
                    for (int i = 0; i < trgt_type_count; i++)
                        unit.atks[i] = int.Parse(attacks.GetProperty(trgt_types[i].id));
                    /* ground defense */
                    unit.def_grnd = int.Parse(sub.GetProperty("def_ground"));
                    /* air defense */
                    unit.def_air = int.Parse(sub.GetProperty("def_air"));
                    /* close defense */
                    unit.def_cls = int.Parse(sub.GetProperty("def_close"));
                    /* flags */
                    string flags = sub.GetProperty("flags");
                    foreach (string flag in flags.Split('°'))
                    {
                        unit.flags |= (UnitFlags)System.Enum.Parse(typeof(UnitFlags), flag.ToUpper());
                    }

                    /* set the entrenchment rate */
                    unit.entr_rate = 2;
                    if ((unit.flags & UnitFlags.LOW_ENTR_RATE) == UnitFlags.LOW_ENTR_RATE)
                        unit.entr_rate = 1;
                    else
                        if ((unit.flags & UnitFlags.INFANTRY) == UnitFlags.INFANTRY)
                            unit.entr_rate = 3;
                    /* time period of usage */
                    unit.start_year = unit.start_month = unit.end_year = 0;
                    str = sub.GetProperty("start_year");
                    if (str != null) unit.start_year = int.Parse(str);
                    str = sub.GetProperty("start_month");
                    if (str != null) unit.start_month = int.Parse(str);
                    str = sub.GetProperty("expired");
                    if (str != null) unit.end_year = int.Parse(str);
                    /* icon */
                    int width, height, offset;
                    Int32 color_key;
                    /* icon id */
                    int icon_id = int.Parse(sub.GetProperty("icon_id"));
                    /* icon_type */
                    unit.icon_type = icon_type;
                    /* get position and size in icons surface */
                    unit_get_icon_geometry(icon_id, icons, out width, out height, out offset, out color_key);
                    /* picture is copied from unit_pics first
                     * if picture_type is not ALL_DIRS, picture is a single picture looking to the right;
                     * add a flipped picture looking to the left 
                     */

                    if (unit.icon_type == UnitIconStyle.UNIT_ICON_ALL_DIRS)
                    {
                        unit.icon = SDL_Surface.CreateSurface(width * 6, height, true);
                        unit.icon_w = width;
                        unit.icon_h = height;
                        SDL_Surface.full_copy_image(unit.icon, icons, 0, offset);
                        /* remove measure dots */
                        SDL_Surface.SetPixel(unit.icon, 0, 0, color_key);
                        SDL_Surface.SetPixel(unit.icon, 0, unit.icon_h - 1, color_key);
                        SDL_Surface.SetPixel(unit.icon, unit.icon_w - 1, 0, color_key);
                        /* set transparency */
                        SDL_Surface.SDL_SetColorKey(unit.icon, color_key);
                    }
                    else
                    {
                        /* set size */
                        unit.icon_w = width;
                        unit.icon_h = height;
                        /* create pic and copy first pic */
                        unit.icon = SDL_Surface.CreateSurface(unit.icon_w * 2, unit.icon_h, true);
                        SDL_Surface.copy_image(unit.icon, 0, 0, unit.icon_w, unit.icon_h, icons, 0, offset);
                        unit.icon.name = unit.icon.name + unit.name;
                        /* remove measure dots */
                        SDL_Surface.SetPixel(unit.icon, 0, 0, color_key);
                        SDL_Surface.SetPixel(unit.icon, 0, unit.icon_h - 1, color_key);
                        SDL_Surface.SetPixel(unit.icon, unit.icon_w - 1, 0, color_key);
                        /* get second by flipping first one */
                        SDL_Surface.copy_image180(unit.icon, unit.icon_w, 0, unit.icon_w, unit.icon_h, icons, 0, offset);
                        /* set transparency */
                        SDL_Surface.SDL_SetColorKey(unit.icon, color_key);
                    }
                    float scale = 1.5f;
                    unit.icon_tiny = SDL_Surface.CreateSurface((int)(unit.icon.w * (1.0 / scale)), (int)(unit.icon.h * (1.0 / scale)), true);
                    unit.icon_tiny_w = (int)((unit.icon_w * (1.0 / scale)));
                    unit.icon_tiny_h = (int)(unit.icon_h * (1.0 / scale));
                    for (int j = 0; j < unit.icon_tiny.h; j++)
                    {
                        for (int i = 0; i < unit.icon_tiny.w; i++)
                            SDL_Surface.SetPixel(unit.icon_tiny, i, j,
                                SDL_Surface.GetPixel(unit.icon, (int)(scale * i), (int)(scale * j)));
                    }
                    /* use color key of 'big' picture */
                    SDL_Surface.SDL_SetColorKey(unit.icon_tiny, color_key);
                    /* read sounds -- well as there are none so far ... */
#if WITH_SOUND
                    if ( parser_get_value( sub, "move_sound", &str, 0 ) ) {
                        // FIXME reloading the same sound more than once is a
                        // big waste of loadtime, runtime, and memory
                        if ( ( unit.wav_move = wav_load( str, 0 ) ) )
                            unit.wav_alloc = 1;
                        else {
                            unit.wav_move = mov_types[unit.mov_type].wav_move;
                            unit.wav_alloc = 0;
                        }
                    }
                    else {
                        unit.wav_move = mov_types[unit.mov_type].wav_move;
                        unit.wav_alloc = 0;
                    }
#endif
                    /* add unit to database */
                    unit_lib.Add(unit);
                    /* absolute evaluation */
                    unit_lib_eval_unit(unit);
                }

                /* relative evaluate of units */
                int best_air = 0;
                int best_sea = 0;
                int best_ground = 0;
                foreach (Unit_Lib_Entry unit in unit_lib)
                {
                    if ((unit.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
                        best_air = Math.Max(unit.eval_score, best_air);
                    else
                    {
                        if ((unit.flags & UnitFlags.SWIMMING) == UnitFlags.SWIMMING)
                            best_sea = Math.Max(unit.eval_score, best_sea);
                        else
                            best_ground = Math.Max(unit.eval_score, best_ground);
                    }
                }
                foreach (Unit_Lib_Entry unit in unit_lib)
                {
                    unit.eval_score *= 1000;
                    if ((unit.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
                        unit.eval_score /= best_air;
                    else
                    {
                        if ((unit.flags & UnitFlags.SWIMMING) == UnitFlags.SWIMMING)
                            unit.eval_score /= best_sea;
                        else
                            unit.eval_score /= best_ground;
                    }
                }

            }
            catch (Exception e)
            {
                Console.Error.WriteLine("exception: " + e);
            }
            return 0;
#if TODO
    int i, j, icon_id;
    SDL_Surface *icons = NULL;
    int icon_type;
    int width, height, offset;
    Uint32 color_key;
    int byte_size, y_offset;
    char *pix_buffer;
    Unit_Lib_Entry *unit;
    int best_ground = 0,
        best_air = 0,
        best_sea = 0; /* used to relativate evaluations */
    List *entries, *flags;
    PData *pd, *sub, *subsub;
    string path;
    string str, flag;
    string domain = 0;
    float scale;
    /* log info */
    int  log_dot_limit = 40; /* maximum of dots */
    int  log_dot_count = 0; /* actual number of dots displayed */
    int  log_units_per_dot = 0; /* number of units a dot represents */
    int  log_unit_count = 0; /* if > units_per_dot a new dot is added */
    string log_str;
    /* there can be only one main library */
    if ( main == UNIT_LIB_MAIN && unit_lib_main_loaded ) {
        fprintf( stderr, tr("%s: can't load as main unit library (which is already loaded): loading as 'additional' instead\n"),
                 fname );
        main = UNIT_LIB_ADD;
    }
    /* parse file */
    sprintf( path, "%s/units/%s", get_gamedir(), fname );
    sprintf( log_str, tr("  Parsing '%s'"), fname );
    write_line( sdl.screen, log_font, log_str, log_x, &log_y ); refresh_screen( 0, 0, 0, 0 );
    if ( ( pd = parser_read_file( fname, path ) ) == 0 ) goto parser_failure;
    domain = determine_domain(pd, fname);
    locale_load_domain(domain, 0/*FIXME*/);
    /* if main read target types & co */

            ...
            
    /* unit lib entries */
    if ( !parser_get_entries( pd, "unit_lib", &entries ) ) goto parser_failure;
      /* LOG INIT */
      log_units_per_dot = entries.count / log_dot_limit;
      log_dot_count = 0;
      log_unit_count = 0;
      /* (LOG) */
    list_reset( entries );
    while ( ( sub = list_next( entries ) ) ) {
...
        /* icon */
        /* icon id */
        if ( !parser_get_int( sub, "icon_id", &icon_id ) ) goto parser_failure;
        /* icon_type */
        unit.icon_type = icon_type;
        /* get position and size in icons surface */
        unit_get_icon_geometry( icon_id, icons, &width, &height, &offset, &color_key );
        /* picture is copied from unit_pics first
         * if picture_type is not ALL_DIRS, picture is a single picture looking to the right;
         * add a flipped picture looking to the left 
         */
        if ( unit.icon_type == UNIT_ICON_ALL_DIRS ) {
            unit.icon = create_surf( width * 6, height, SDL_SWSURFACE );
            unit.icon_w = width;
            unit.icon_h = height;
            FULL_DEST( unit.icon );
            SOURCE( icons, 0, offset );
            blit_surf();
            /* remove measure dots */
            set_pixel( unit.icon, 0, 0, color_key );
            set_pixel( unit.icon, 0, unit.icon_h - 1, color_key );
            set_pixel( unit.icon, unit.icon_w - 1, 0, color_key );
            /* set transparency */
            SDL_SetColorKey( unit.icon, SDL_SRCCOLORKEY, color_key );
        }
        else {
            /* set size */
            unit.icon_w = width;
            unit.icon_h = height;
            /* create pic and copy first pic */
            unit.icon = create_surf( unit.icon_w * 2, unit.icon_h, SDL_SWSURFACE );
            DEST( unit.icon, 0, 0, unit.icon_w, unit.icon_h );
            SOURCE( icons, 0, offset );
            blit_surf();
            /* remove measure dots */
            set_pixel( unit.icon, 0, 0, color_key );
            set_pixel( unit.icon, 0, unit.icon_h - 1, color_key );
            set_pixel( unit.icon, unit.icon_w - 1, 0, color_key );
            /* set transparency */
            SDL_SetColorKey( unit.icon, SDL_SRCCOLORKEY, color_key );
            /* get format info */
            byte_size = icons.format.BytesPerPixel;
            y_offset = 0;
            pix_buffer = calloc( byte_size, sizeof( char ) );
            /* get second by flipping first one */
            for ( j = 0; j < unit.icon_h; j++ ) {
                for ( i = 0; i < unit.icon_w; i++ ) {
                    memcpy( pix_buffer,
                            unit.icon.pixels +
                            y_offset +
                            ( unit.icon_w - 1 - i ) * byte_size,
                            byte_size );
                    memcpy( unit.icon.pixels +
                            y_offset +
                            unit.icon_w * byte_size +
                            i * byte_size,
                            pix_buffer, byte_size );
                }
                y_offset += unit.icon.pitch;
            }
            /* free mem */
            free( pix_buffer );
        }
        scale = 1.5;
        unit.icon_tiny = create_surf( unit.icon.w * ( 1.0 / scale ), unit.icon.h * ( 1.0 / scale ), SDL_SWSURFACE );
        unit.icon_tiny_w = unit.icon_w * ( 1.0 / scale ); unit.icon_tiny_h = unit.icon_h * ( 1.0 / scale );
        for ( j = 0; j < unit.icon_tiny.h; j++ ) {
            for ( i = 0; i < unit.icon_tiny.w; i++ )
                set_pixel( unit.icon_tiny,
                           i, j, 
                           get_pixel( unit.icon, scale * i, scale * j ) );
        }
        /* use color key of 'big' picture */
        SDL_SetColorKey( unit.icon_tiny, SDL_SRCCOLORKEY, color_key );
        /* read sounds -- well as there are none so far ... */
#if WITH_SOUND
        if ( parser_get_value( sub, "move_sound", &str, 0 ) ) {
            // FIXME reloading the same sound more than once is a
            // big waste of loadtime, runtime, and memory
            if ( ( unit.wav_move = wav_load( str, 0 ) ) )
                unit.wav_alloc = 1;
            else {
                unit.wav_move = mov_types[unit.mov_type].wav_move;
                unit.wav_alloc = 0;
            }
        }
        else {
            unit.wav_move = mov_types[unit.mov_type].wav_move;
            unit.wav_alloc = 0;
        }
#endif      
        /* add unit to database */
        list_add( unit_lib, unit );
        /* absolute evaluation */
        unit_lib_eval_unit( unit );
        /* LOG */
        log_unit_count++;
        if ( log_unit_count >= log_units_per_dot ) {
            log_unit_count = 0;
            if ( log_dot_count < log_dot_limit ) {
                log_dot_count++;
                strcpy( log_str, "  [                                        ]" );
                for ( i = 0; i < log_dot_count; i++ )
                    log_str[3 + i] = '*';
                write_text( log_font, sdl.screen, log_x, log_y, log_str, 255 );
                SDL_UpdateRect( sdl.screen, log_font.last_x, log_font.last_y, log_font.last_width, log_font.last_height );
            }
        }
    }
    parser_free( &pd );
    /* LOG */
    write_line( sdl.screen, log_font, log_str, log_x, &log_y ); refresh_screen( 0, 0, 0, 0 );
    /* relative evaluate of units */
    list_reset( unit_lib );
    while ( ( unit = list_next( unit_lib ) ) ) {
        if ( unit.flags & FLYING )
            best_air = MAXIMUM( unit.eval_score, best_air );
        else {
            if ( unit.flags & SWIMMING )
                best_sea = MAXIMUM( unit.eval_score, best_sea );
            else
                best_ground = MAXIMUM( unit.eval_score, best_ground );
        }
    }
    list_reset( unit_lib );
    while ( ( unit = list_next( unit_lib ) ) ) {
        unit.eval_score *= 1000;
        if ( unit.flags & FLYING )
            unit.eval_score /= best_air;
        else {
            if ( unit.flags & SWIMMING )
                unit.eval_score /= best_sea;
            else
                unit.eval_score /= best_ground;
        }
    }
    free(domain);
    SDL_FreeSurface(icons);
    return 1;
parser_failure:        
    fprintf( stderr, "%s\n", parser_get_error() );
failure:
    unit_lib_delete();
    if ( pd ) parser_free( &pd );
    free(domain);
    SDL_FreeSurface(icons);
    return 0;
#endif
        }
#endif

        /*
        ====================================================================
        Delete unit library.
        ====================================================================
        */
        public void unit_lib_delete()
        {
            int i;
            if (unit_lib != null)
            {
                unit_lib.Clear();
                unit_lib = null;
            }
            if (trgt_types != null)
            {
                for (i = 0; i < trgt_type_count; i++)
                {
                    if (!string.IsNullOrEmpty(trgt_types[i].id)) trgt_types[i].id = null;
                    if (!string.IsNullOrEmpty(trgt_types[i].name)) trgt_types[i].name = null;
                }
                trgt_types = null;
                trgt_type_count = 0;
            }
            if (mov_types != null)
            {
                for (i = 0; i < mov_type_count; i++)
                {
                    if (!string.IsNullOrEmpty(mov_types[i].id)) mov_types[i].id = null;
                    if (!string.IsNullOrEmpty(mov_types[i].name)) mov_types[i].name = null;
#if WITH_SOUND
            if ( mov_types[i].wav_move )
                wav_free( mov_types[i].wav_move );
#endif
                }
                mov_types = null; mov_type_count = 0;
            }
            if (unit_classes != null)
            {
                for (i = 0; i < unit_class_count; i++)
                {
                    if (!string.IsNullOrEmpty(unit_classes[i].id)) unit_classes[i].id = null;
                    if (!string.IsNullOrEmpty(unit_classes[i].name)) unit_classes[i].name = null;
                }
                unit_classes = null; unit_class_count = 0;
            }
            if (unit_info_icons != null)
            {
                if (unit_info_icons.str != null) SDL_FreeSurface(unit_info_icons.str);
                if (unit_info_icons.atk != null) SDL_FreeSurface(unit_info_icons.atk);
                if (unit_info_icons.mov != null) SDL_FreeSurface(unit_info_icons.mov);
                if (unit_info_icons.guard != null) SDL_FreeSurface(unit_info_icons.guard);
                unit_info_icons = null;
            }
            unit_lib_main_loaded = false;
        }

        /*
        ====================================================================
        Find unit lib entry by id string.
        ====================================================================
        */
        public Unit_Lib_Entry unit_lib_find(string id)
        {
            foreach (Unit_Lib_Entry entry in unit_lib)
                if (entry.id == id)
                    return entry;
            return null;
        }


        /*
        ====================================================================
        Target types, movement types, unit classes
        These may only be loaded if unit_lib_main_loaded is False.
        ====================================================================
        */
        bool unit_lib_main_loaded = false;
        public Trgt_Type[] trgt_types;
        public int trgt_type_count = 0;
        public Mov_Type[] mov_types;
        public int mov_type_count = 0;
        public Unit_Class[] unit_classes;
        public int unit_class_count = 0;



        /*
        ====================================================================
        Unit map tile icons (strength, move, attack ...)
        ====================================================================
        */
        public Unit_Info_Icons unit_info_icons;

        /*
        ====================================================================
        Unit library list which is created by the UNIT_LIB_MAIN call
        of UnitLibLoad().
        ====================================================================
        */
        List<Unit_Lib_Entry> unit_lib = new List<Unit_Lib_Entry>();

        /*
        ====================================================================
        Convertion table for string . flag.
        ====================================================================
        */
#if TODO
StrToFlag[] fct_units = new StrToFlag[]{
	new StrToFlag ( "swimming", UnitFlags.SWIMMING ),
    new StrToFlag ( "flying", FLYING ),               
    new StrToFlag ( "diving", DIVING ),               
    new StrToFlag ( "parachute", PARACHUTE ),         
    new StrToFlag ( "transporter", TRANSPORTER ),     
    new StrToFlag ( "recon", RECON ),                 
    new StrToFlag ( "artillery", ARTILLERY ),         
    new StrToFlag ( "interceptor", INTERCEPTOR ),     
    new StrToFlag ( "air_defense", AIR_DEFENSE ),     
    new StrToFlag ( "bridge_eng", BRIDGE_ENG ),       
    new StrToFlag ( "infantry", INFANTRY ),           
    new StrToFlag ( "air_trsp_ok", AIR_TRSP_OK ),     
    new StrToFlag ( "destroyer", DESTROYER ),         
    new StrToFlag ( "ignore_entr", IGNORE_ENTR ),     
    new StrToFlag ( "carrier", CARRIER ),             
    new StrToFlag ( "carrier_ok", CARRIER_OK ),       
    new StrToFlag ( "bomber", BOMBER ),               
    new StrToFlag ( "attack_first", ATTACK_FIRST ),
    new StrToFlag ( "low_entr_rate", LOW_ENTR_RATE ),
    new StrToFlag ( "tank", TANK ),
    new StrToFlag ( "anti_tank", ANTI_TANK ),
    new StrToFlag ( "suppr_fire", SUPPR_FIRE ),
    new StrToFlag ( "turn_suppr", TURN_SUPPR ),
    new StrToFlag ( "jet", JET ),
    new StrToFlag ( "X", 0)
};
#endif

        /*
        ====================================================================
        Locals
        ====================================================================
        */

        /*
        ====================================================================
        Get the geometry of an icon in surface 'icons' by using the three
        measure dots in the left upper, right upper, left lower corner.
        ====================================================================
        */
#if TODO_RR
        static void unit_get_icon_geometry(int icon_id, SDL_Surface icons, out int width, out int height, out int offset, out Int32 key)
        {
            Int32 mark;
            int y;
            int count = icon_id * 2; /* there are two pixels for one icon */

            /* nada white dot! take the first pixel found in the upper left corner as mark */
            mark = SDL_Surface.GetPixel(icons, 0, 0);
            /* compute offset */
            for (y = 0; y < icons.h; y++)
                if (SDL_Surface.GetPixel(icons, 0, y) == mark)
                {
                    if (count == 0) break;
                    count--;
                }
            offset = y;
            /* compute height */
            y++;
            while (y < icons.h && SDL_Surface.GetPixel(icons, 0, y) != mark)
                y++;
            height = y - offset + 1;
            /* compute width */
            y = offset;
            width = 1;
            while (SDL_Surface.GetPixel(icons, width, y) != mark)
                width++;
            width++;
            /* pixel beside left upper measure key is color key */
            key = SDL_Surface.GetPixel(icons, 1, offset);
        }
#endif
        /*
        ====================================================================
        Delete unit library entry.
        ====================================================================
        */
        static void unit_lib_delete_entry(Unit_Lib_Entry ptr)
        {
            Unit_Lib_Entry entry = ptr;
            if (entry == null) return;
            if (string.IsNullOrEmpty(entry.id)) entry.id = null;
            if (string.IsNullOrEmpty(entry.name)) entry.name = null;
            if (entry.icon != null) SDL_FreeSurface(entry.icon);
            if (entry.icon_tiny != null) SDL_FreeSurface(entry.icon_tiny);
#if WITH_SOUND
    if ( entry.wav_alloc && entry.wav_move ) 
        wav_free( entry.wav_move );
#endif
            entry = null;
        }

        static void SDL_FreeSurface(SDL_Surface s)
        {
            throw new System.NotImplementedException();
        }
        /*
        ====================================================================
        Evaluate the unit. This score will become a relative one whereas
        the best rating will be 1000. Each operational region 
        ground/sea/air will have its own reference.
        This evaluation is PG specific.
        ====================================================================
        */
        void unit_lib_eval_unit(Unit_Lib_Entry unit)
        {
            int attack = 0, defense = 0, misc = 0;
            /* The score is computed by dividing the unit's properties
               into categories. The subscores are added with different
               weights. */
            /* The first category covers the attack skills. */
            if ((unit.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
            {
                attack = unit.atks[0] + unit.atks[1] + /* ground */
                         2 * Math.Max(unit.atks[2], Math.Abs(unit.atks[2]) / 2) + /* air */
                         unit.atks[3]; /* sea */
            }
            else
            {
                if ((unit.flags & UnitFlags.SWIMMING) == UnitFlags.SWIMMING)
                {
                    attack = unit.atks[0] + unit.atks[1] + /* ground */
                             unit.atks[2] + /* air */
                             2 * unit.atks[3]; /* sea */
                }
                else
                {
                    attack = 2 * Math.Max(unit.atks[0], Math.Abs(unit.atks[0]) / 2) + /* soft */
                             2 * Math.Max(unit.atks[1], Math.Abs(unit.atks[1]) / 2) + /* hard */
                             Math.Max(unit.atks[2], Math.Abs(unit.atks[2]) / 2) + /* air */
                             unit.atks[3]; /* sea */
                }
            }
            attack += unit.ini;
            attack += 2 * unit.rng;
            /* The second category covers defensive skills. */
            if ((unit.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
                defense = unit.def_grnd + 2 * unit.def_air;
            else
            {
                defense = 2 * unit.def_grnd + unit.def_air;
                if ((unit.flags & UnitFlags.INFANTRY) == UnitFlags.INFANTRY)
                    /* hype infantry a bit as it doesn't need the
                       close defense value */
                    defense += 5;
                else
                    defense += unit.def_cls;
            }
            /* The third category covers miscellany skills. */
            if ((unit.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
                misc = Math.Min(12, unit.ammo) + Math.Min(unit.fuel, 80) / 5 + unit.mov / 2;
            else
                misc = Math.Min(12, unit.ammo) + Math.Min(unit.fuel, 60) / 4 + unit.mov;
            /* summarize */
            unit.eval_score = (2 * attack + 2 * defense + misc) / 5;
        }

    }
}
