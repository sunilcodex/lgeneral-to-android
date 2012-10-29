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
using System.Xml.Serialization;
using EngineA;
using UnityEngine;
using System.IO;
using Miscellaneous;

namespace DataFile
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

	[Serializable]
    public class Trgt_Type
	{
		public string id;
		public string name;
	}
	
	[Serializable]
    public class Mov_Type
	{
		public string id;
		public string name;
#if WITH_SOUND    
	    public Wav wav_move;
#endif
	}
	
	[Serializable]
    public class Unit_Class
	{
		public string id;
		public string name;
	}


	/// <summary>
	/// Unit map tile info icons (strength, move, attack ...)
	/// </summary>
	[Serializable]
    public class Unit_Info_Icons
	{
		public string str_img_name, atk_img_name, mov_img_name, guard_img_name;
		public int str_w, str_h;
		[XmlIgnore]
		public SDL_Surface str;
		[XmlIgnore]
		public SDL_Surface atk;
		[XmlIgnore]
		public SDL_Surface mov;
		[XmlIgnore]
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
	[Serializable]
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
		[XmlIgnore]
        public SDL_Surface icon;      /* tactical icon */
        public string icon_img_name;
        [XmlIgnore]
        public SDL_Surface icon_tiny; /* half the size; used to display air and ground unit at one tile */
        public string icon_tiny_img_name;
		public UnitIconStyle icon_type;          /* either single or all_dirs */
		public int icon_w, icon_h;     /* single icon size */
		public int icon_tiny_w, icon_tiny_h; /* single icon size */
		public UnitFlags flags;
		public int start_year, start_month, end_year; /* time of usage */
		public int offset_img;
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
		
		public int UnitLibLoad(string fname){
			string path = "Assets/Resources/DB/"+fname;
			try{
				XmlSerializer SerializerObj = new XmlSerializer(typeof(Unit_Lib_Entry));
        		// Create a new file stream for reading the XML file
        		FileStream ReadFileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        		// Load the object saved above by using the Deserialize function
        		Unit_Lib_Entry LoadedObject = (Unit_Lib_Entry)SerializerObj.Deserialize(ReadFileStream);
				ReadFileStream.Close();
				this.ammo = LoadedObject.ammo;
				this.atks = LoadedObject.atks;
				this.atk_count = LoadedObject.atk_count;
				this.def_air = LoadedObject.def_air;
				this.def_cls = LoadedObject.def_cls;
				this.def_grnd = LoadedObject.def_grnd;
				this.end_year = LoadedObject.end_year;
				this.entr_rate = LoadedObject.entr_rate;
				this.eval_score = LoadedObject.eval_score;
				this.flags = LoadedObject.flags;
				this.fuel = LoadedObject.fuel;
				this.icon_img_name = LoadedObject.icon_img_name;
				this.icon_h = LoadedObject.icon_h;
				this.icon_tiny_img_name = LoadedObject.icon_tiny_img_name;
				this.icon_tiny_h = LoadedObject.icon_tiny_h;
				this.icon_tiny_w = LoadedObject.icon_tiny_w;
				this.icon_type = LoadedObject.icon_type;
				this.icon_w = LoadedObject.icon_w;
				this.id = LoadedObject.id;
				this.ini = LoadedObject.ini;
				this.mov = LoadedObject.mov;
				this.mov_type = LoadedObject.mov_type;
				this.mov_types = LoadedObject.mov_types;
				this.mov_type_count = LoadedObject.mov_type_count;
				this.name = LoadedObject.name;
				this.rng = LoadedObject.rng;
				this.spt = LoadedObject.spt;
				this.start_month = LoadedObject.start_month;
				this.start_year = LoadedObject.start_year;
				this.trgt_type = LoadedObject.trgt_type;
				this.trgt_types = LoadedObject.trgt_types;
				this.trgt_type_count = LoadedObject.trgt_type_count;
				this.unit_class = LoadedObject.unit_class;
				this.unit_classes = LoadedObject.unit_classes;
				this.unit_class_count = LoadedObject.unit_class_count;
				this.unit_info_icons = LoadedObject.unit_info_icons;
#if TODO_RR
				this.unit_info_icons = new Unit_Info_Icons();
				this.unit_info_icons.atk_img_name = LoadedObject.unit_info_icons.atk_img_name;
				this.unit_info_icons.guard_img_name = LoadedObject.unit_info_icons.guard_img_name;
				this.unit_info_icons.mov_img_name = LoadedObject.unit_info_icons.mov_img_name;
				this.unit_info_icons.str_img_name = LoadedObject.unit_info_icons.str_img_name;
				this.unit_info_icons.str_h = LoadedObject.unit_info_icons.str_h;
				this.unit_info_icons.str_w = LoadedObject.unit_info_icons.str_w;
#endif
				this.unit_lib = LoadedObject.unit_lib;
				this.unit_lib_main_loaded = true;
				this.offset_img = LoadedObject.offset_img;
				
				return 0;
			}catch(Exception ex){
				Debug.LogError("exception: "+ex.Message);
				return -1;
			}
			
		}
#if TODO_RR
		public void unitToFile ()
		{
			try {
				foreach (Unit_Lib_Entry unit in this.unit_lib) {
					int e = TextureTable.elegirImgUnit (unit);
					SDL_Surface sdl = SDL_Surface.LoadSurface ("Textures/units/pg1", false);
					SDL_Surface dest = new SDL_Surface ();
					if (e == 1) {
						int o = (sdl.h - unit.offset_img - unit.icon_h);
						if (o >= 0) {
							SDL_Surface.copy_image (dest, unit.icon_w, unit.icon_h, sdl, 0, o);
							byte[] bytes = dest.bitmap.EncodeToPNG ();
							File.WriteAllBytes (Application.dataPath + "/../Unit/" + unit.id + ".png", bytes);
							
						} else {
							Debug.Log (unit.name);
						}
					
					
					} else if (e == 2) {
					
						SDL_Surface sdl2 = SDL_Surface.LoadSurface ("Textures/units/pg2", false);
						int v = unit.offset_img + 2 - sdl.h + unit.icon_h;
						int v2 = sdl2.h - v;
						if (v2 >= 0) {
							SDL_Surface.copy_image (dest, unit.icon_w, unit.icon_h, sdl2, 0, v2);
							byte[] bytes = dest.bitmap.EncodeToPNG ();
							File.WriteAllBytes (Application.dataPath + "/../Unit/" + unit.id + ".png", bytes);
					
						} else {
							Debug.Log (unit.name);
						}
					
					
					}
				
				}
			} catch (Exception e) {
				Debug.LogError (e.Message);
			}
		}
#endif
		/*
        ====================================================================
        Delete unit library.
        ====================================================================
        */
		public void unit_lib_delete ()
		{
			int i;
			if (unit_lib != null) {
				unit_lib.Clear ();
				unit_lib = null;
			}
			if (trgt_types != null) {
				for (i = 0; i < trgt_type_count; i++) {
					if (!string.IsNullOrEmpty (trgt_types [i].id))
						trgt_types [i].id = null;
					if (!string.IsNullOrEmpty (trgt_types [i].name))
						trgt_types [i].name = null;
				}
				trgt_types = null;
				trgt_type_count = 0;
			}
			if (mov_types != null) {
				for (i = 0; i < mov_type_count; i++) {
					if (!string.IsNullOrEmpty (mov_types [i].id))
						mov_types [i].id = null;
					if (!string.IsNullOrEmpty (mov_types [i].name))
						mov_types [i].name = null;
#if WITH_SOUND
            if ( mov_types[i].wav_move )
                wav_free( mov_types[i].wav_move );
#endif
				}
				mov_types = null;
				mov_type_count = 0;
			}
			if (unit_classes != null) {
				for (i = 0; i < unit_class_count; i++) {
					if (!string.IsNullOrEmpty (unit_classes [i].id))
						unit_classes [i].id = null;
					if (!string.IsNullOrEmpty (unit_classes [i].name))
						unit_classes [i].name = null;
				}
				unit_classes = null;
				unit_class_count = 0;
			}
			if (unit_info_icons != null) {
				if (unit_info_icons.str != null)
					SDL_FreeSurface (unit_info_icons.str);
				if (unit_info_icons.atk != null)
					SDL_FreeSurface (unit_info_icons.atk);
				if (unit_info_icons.mov != null)
					SDL_FreeSurface (unit_info_icons.mov);
				if (unit_info_icons.guard != null)
					SDL_FreeSurface (unit_info_icons.guard);
				unit_info_icons = null;
			}
			unit_lib_main_loaded = false;
		}

		/*
        ====================================================================
        Find unit lib entry by id string.
        ====================================================================
        */
		public Unit_Lib_Entry unit_lib_find (string id)
		{
			foreach (Unit_Lib_Entry entry in unit_lib)
				if (entry.id == id)
					return entry;
			return null;
		}

		public Unit_Lib_Entry unit_lib_find_by_name (string name)
		{
			foreach (Unit_Lib_Entry entry in unit_lib)
				if (entry.name == name)
					return entry;
			return null;
		}
		/*
        ====================================================================
        Target types, movement types, unit classes
        These may only be loaded if unit_lib_main_loaded is False.
        ====================================================================
        */
		[XmlIgnore]
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
		[XmlIgnore]
		List<Unit_Lib_Entry> unit_lib = new List<Unit_Lib_Entry> ();

		public List<Unit_Lib_Entry> Unit_Lib {
			get { return unit_lib; }
			set { unit_lib = value; }
		}

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
		static void unit_lib_delete_entry (Unit_Lib_Entry ptr)
		{
			Unit_Lib_Entry entry = ptr;
			if (entry == null) return;
            if (string.IsNullOrEmpty(entry.id)) 
				entry.id = null;
            if (string.IsNullOrEmpty(entry.name)) 
				entry.name = null;
            if (entry.icon != null) 
				SDL_FreeSurface(entry.icon);
            if (entry.icon_tiny != null) 
				SDL_FreeSurface(entry.icon_tiny);
			
#if WITH_SOUND
    if ( entry.wav_alloc && entry.wav_move ) 
        wav_free( entry.wav_move );
#endif
			entry = null;
		}

		static void SDL_FreeSurface (SDL_Surface s)
		{
			throw new System.NotImplementedException ();
		}
		/*
        ====================================================================
        Evaluate the unit. This score will become a relative one whereas
        the best rating will be 1000. Each operational region 
        ground/sea/air will have its own reference.
        This evaluation is PG specific.
        ====================================================================
        */
		void unit_lib_eval_unit (Unit_Lib_Entry unit)
		{
			int attack = 0, defense = 0, misc = 0;
			/* The score is computed by dividing the unit's properties
               into categories. The subscores are added with different
               weights. */
			/* The first category covers the attack skills. */
			if ((unit.flags & UnitFlags.FLYING) == UnitFlags.FLYING) {
				attack = unit.atks [0] + unit.atks [1] + /* ground */
                         2 * Math.Max (unit.atks [2], Math.Abs (unit.atks [2]) / 2) + /* air */
                         unit.atks [3]; /* sea */
			} else {
				if ((unit.flags & UnitFlags.SWIMMING) == UnitFlags.SWIMMING) {
					attack = unit.atks [0] + unit.atks [1] + /* ground */
                             unit.atks [2] + /* air */
                             2 * unit.atks [3]; /* sea */
				} else {
					attack = 2 * Math.Max (unit.atks [0], Math.Abs (unit.atks [0]) / 2) + /* soft */
                             2 * Math.Max (unit.atks [1], Math.Abs (unit.atks [1]) / 2) + /* hard */
                             Math.Max (unit.atks [2], Math.Abs (unit.atks [2]) / 2) + /* air */
                             unit.atks [3]; /* sea */
				}
			}
			attack += unit.ini;
			attack += 2 * unit.rng;
			/* The second category covers defensive skills. */
			if ((unit.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
				defense = unit.def_grnd + 2 * unit.def_air;
			else {
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
				misc = Math.Min (12, unit.ammo) + Math.Min (unit.fuel, 80) / 5 + unit.mov / 2;
			else
				misc = Math.Min (12, unit.ammo) + Math.Min (unit.fuel, 60) / 4 + unit.mov;
			/* summarize */
			unit.eval_score = (2 * attack + 2 * defense + misc) / 5;
		}
		
		/// <summary>
		/// Uploads the unit info icons which had only route where he was in the XML file UnitDB
		/// </summary>
		public void load_unit_icons ()
		{
			this.unit_info_icons.str = SDL_Surface.LoadSurface (unit_info_icons.str_img_name, true);
			this.unit_info_icons.atk = SDL_Surface.LoadSurface (unit_info_icons.atk_img_name, true);
			this.unit_info_icons.mov = SDL_Surface.LoadSurface (unit_info_icons.mov_img_name, true);
			this.unit_info_icons.guard = SDL_Surface.LoadSurface (unit_info_icons.guard_img_name, true);
			foreach( Unit_Lib_Entry item in this.unit_lib){
				item.icon = SDL_Surface.LoadSurface(item.icon_img_name,true);
				item.icon_tiny = SDL_Surface.LoadSurface(item.icon_tiny_img_name,true);
			}
		}

	}
}
