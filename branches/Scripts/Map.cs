using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using DataFile;
using Miscellaneous;
using System.IO;
using UnityEngine;

namespace EngineA
{
	[Serializable]
    public class Way_Point
	{
		public int x, y;
	}

	public enum weather
	{
		FAIR = 0,
		CLOUDS,
		RAIN,
		SNOW
	};

	/// <summary>
	/// To determine various things of the map (deploy, spot, blocked ...)
	/// a map mask is used and these are the flags for it.
	/// </summary>
	[Flags]
    public enum MAP_MASK
	{
		F_FOG = (1 << 1),
		F_SPOT = (1 << 2),
		F_IN_RANGE = (1 << 3),
		F_MOUNT = (1 << 4),
		F_SEA_EMBARK = (1 << 5),
		F_AUX = (1 << 6),
		F_INFL = (1 << 7),
		F_INFL_AIR = (1 << 8),
		F_VIS_INFL = (1 << 9),
		F_VIS_INFL_AIR = (1 << 10),
		F_BLOCKED = (1 << 11),
		F_BACKUP = (1 << 12),
		F_MERGE_UNIT = (1 << 13),
		F_INVERSE_FOG = (1 << 14), /* inversion of F_FOG */
		F_DEPLOY = (1 << 15),
		F_CTRL_GRND = (1 << 17),
		F_CTRL_AIR = (1 << 18),
		F_CTRL_SEA = (1 << 19),
		F_MOVE_COST = (1 << 20),
		F_DANGER = (1 << 21),
		F_SPLIT_UNIT = (1 << 22),
		F_DISTANCE = (1 << 23)
	}
	
	[Serializable]
    public class Map_Tile
	{
		public string name;             /* name of this map tile */
		public Terrain_Type terrain;  /* terrain properties */
		[XmlIgnore]
		public int terrain_id;         /* id of terrain properties */
		[XmlIgnore]
		public int image_offset;       /* image offset in prop.image */
		public int strat_image_offset; /* offset in the list of strategic tiny terrain images */
		[XmlIgnore]
		public Nation nation;         /* nation that owns this flag (NULL == no nation) */
		[XmlIgnore]
		public Player player;         /* dito */
		[XmlIgnore]
		public bool obj;                /* military objective ? */
		[XmlIgnore]
		public int deploy_center;      /* deploy allowed? */
		[XmlIgnore]
		public Unit g_unit;           /* ground/naval unit pointer */
		[XmlIgnore]
		public Unit a_unit;           /* air unit pointer */
		[XmlIgnore]
		public Unit backupUnit;
	}


	/// <summary>
	/// Map mask tile.
	/// </summary>
	public class Mask_Tile
	{
		public bool fog; /* if true the engine covers this tile with fog. if ENGINE_MODIFY_FOG is set
                    this fog may change depending on the action (range of unit, merge partners
                    etc */
		public bool spot; /* true if any of your units observes this map tile; you can only attack units
                            on a map tile that you spot */
		/* used for a selected unit */
		public int in_range; /* this is used for pathfinding; it's -1 if tile isn't in range else it's set to the
                        remaining moving points of the unit; enemy influence is not included */
		public int distance; /* mere distance to current unit; used for danger mask */
		public int moveCost; /* total costs to reach this tile */
		public bool blocked; /* units can move over there tiles with an allied unit but they must not stop there;
                        so allow movment to a tile only if in_range and !blocked */
		public int mount; /* true if unit must mount to reach this tile */
		public bool sea_embark; /* sea embark possible? */
		public int infl; /* at the beginning of a player's turn this mask is set; each tile close to a
                    hostile unit gets infl increased; if influence is 1 moving costs are doubled; if influence is >=2
                    this tile is impassible (unit stops at this tile); if a unit can't see a tile with infl >= 2 and
                    tries to move there it will stop on this tile; independed from a unit's moving points passing an
                    influenced tile costs all mov-points */
		public int vis_infl; /* analogue to infl but only spotted units contribute to this mask; used to setup
                        in_range mask */
		public int air_infl; /* analouge for flying units */
		public int vis_air_infl;
		public int aux; /* used to setup any of the upper values */
		public bool backup; /* used to backup spot mask for undo unit move */
		public Unit merge_unit; /* if not NULL this is a pointer to a unit the one who called map_get_merge_units()
                         may merge with. you'll need to remember the other unit as it is not saved here */
		public Unit split_unit; /* target unit may transfer strength to */
		public bool split_okay; /* unit may transfer a new subunit to this tile */
		public bool deploy; /* deploy mask: "true": unit may deploy their, "false" unit may not deploy their; setup by deploy.c */
		public bool danger; /* true: mark this tile as being dangerous to enter */
		/* AI masks */
		public int ctrl_grnd; /* mask of controlled area for a player. own units give there positive combat
                      value in move+attack range while enemy scores are substracted. the final
                      value for each tile is relative to the highest absolute control value thus
                      it ranges from -1000 to 1000 */
		public int ctrl_air;
		public int ctrl_sea; /* each operational region has it's own control mask */
	}

	[Serializable]
    public class Map
	{
        #region Protected and Private
		public int map_w = 0, map_h = 0;
		[XmlIgnore]
		public Map_Tile[,] map;

		public Map_Tile[][] Tile {
			get {
				Map_Tile[][] arr = new Map_Tile[map_w][];
				for (int i = 0; i < map_w; i++) {
					arr [i] = new Map_Tile[map_h];
				}

				for (int i = 0; i < map_w; i++) {
					for (int j = 0; j < map_h; j++) {
						arr [i] [j] = map [i, j];
					}
				}
				return arr;
			}
			set {
				map = new Map_Tile[map_w, map_h];
				for (int i = 0; i < map_w; i++) {
					for (int j = 0; j < map_h; j++) {
						map [i, j] = value [i] [j];
					}
				}
			}
		}

		[XmlIgnore]
		public Mask_Tile[,] mask;
		public const short DIST_AIR_MAX = short.MaxValue;
		[XmlIgnore]
		public bool isLoaded = false;
		public string terrainDB;
		public struct MapCoord
		{
			public MapCoord (short px, short py)
			{
				x = px;
				y = py;
			}

			public short x, y;
		}
        #endregion

		static List<List<MapCoord>> deploy_fields;

		/// <summary>
		/// Check the surrounding tiles and get the one with the highest
		/// in_range value.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="next_x"></param>
		/// <param name="next_y"></param>
		static void map_get_next_unit_point (int x, int y, out int next_x, out int next_y)
		{

			int high_x, high_y;
			int i;
			high_x = x;
			high_y = y;
			for (i = 0; i < 6; i++)
				if (Misc.get_close_hex_pos (x, y, i, out next_x, out next_y))
				if (Engine.map.mask [next_x, next_y].in_range > Engine.map.mask [high_x, high_y].in_range) {
					high_x = next_x;
					high_y = next_y;
				}
			next_x = high_x;
			next_y = high_y;
		}

		/// <summary>
		/// Add a unit's influence to the (vis_)infl mask.
		/// </summary>
		/// <param name="unit"></param>
		static void map_add_vis_unit_infl (Unit unit)
		{
			int i, next_x, next_y;
			if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) {
				Engine.map.mask [unit.x, unit.y].vis_air_infl++;
				for (i = 0; i < 6; i++)
					if (Misc.get_close_hex_pos (unit.x, unit.y, i, out next_x, out next_y))
						Engine.map.mask [next_x, next_y].vis_air_infl++;
			} else {
				Engine.map.mask [unit.x, unit.y].vis_infl++;
				for (i = 0; i < 6; i++)
					if (Misc.get_close_hex_pos (unit.x, unit.y, i, out next_x, out next_y))
						Engine.map.mask [next_x, next_y].vis_infl++;
			}
		}

		/// <summary>
		/// Load map.
		/// </summary>
		/// <param name="fname">map name </param>
		/// <returns></returns>
		public int map_load (string fname)
		{
			string path = "Assets/Maps/" + fname;
			try {
				XmlSerializer SerializerObj = new XmlSerializer (typeof(Map));
				// Create a new file stream for reading the XML file
				FileStream ReadFileStream = new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.Read);
				// Load the object saved above by using the Deserialize function
				Map mapLoad = (Map)SerializerObj.Deserialize (ReadFileStream);
				// Cleanup
				ReadFileStream.Close ();
				/* map size */
				map_w = mapLoad.map_w;
				map_h = mapLoad.map_h;
				/* load terrains */
				terrainDB = mapLoad.terrainDB;
				Terrain terrain = Engine.terrain;
				if (terrain.Load (terrainDB) == -1)
					return -1;
				/* allocate map memory */
				this.mask = new Mask_Tile[map_w, map_h];
				this.map = mapLoad.map;
				for (int x = 0; x < map_w; x++)
					for (int y = 0; y < map_h; y++) {
						this.mask [x, y] = new Mask_Tile ();
					}
				for (int y = 0; y < map_h; y++)
					for (int x = 0; x < map_w; x++) {
						/* default is no flag */
						this.map [x, y].nation = null;
						this.map [x, y].player = null;
						this.map [x, y].deploy_center = 0;
						/* default is no mil target */
						this.map [x, y].obj = false;
						/* check tile type */
						for (int j = 0; j < terrain.terrainTypeCount; j++) {
							if (terrain.terrainTypes [j].id [0] == this.map [x, y].terrain.id [0]) {
								this.map [x, y].terrain = terrain.terrainTypes [j];
								this.map [x, y].terrain_id = j;
							}
						}
						/* tile not found, used first one */
						if (this.map [x, y].terrain == null)
							this.map [x, y].terrain = terrain.terrainTypes [0];
						
						if (this.map [x, y].terrain.id [0] == '?') {
							this.map [x, y].strat_image_offset = Misc.RANDOM (0,
							TextureTable.GetMaxTextureOf (map [x, y].terrain.name)) + 1;
						}
					}
				this.isLoaded = true;
				return 1;
			} catch (Exception ex) {
				Debug.LogError ("exception: " + ex.Message);
				return -1;
			}	
		}
		
		

		/// <summary>
		/// Delete map.
		/// </summary>
		public void map_delete ()
		{
			throw new System.NotImplementedException ();
		}

		/// <summary>
		/// Get tile at x,y
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns
		public Map_Tile map_tile (int x, int y)
		{
			if (x < 0 || y < 0 || x >= map_w || y >= map_h) {
				//throw new Exception( "map_tile: map tile at "+x+", "+y+" doesn't exist");
				return null;
			}
			return map [x, y];
		}

		public Mask_Tile map_mask_tile (int x, int y)
		{
			if (x < 0 || y < 0 || x >= map_w || y >= map_h) {
				//throw new Exception("map_tile: mask tile at "+x+", "+y+" doesn't exist");
				return null;
			}
			return mask [x, y];
		}

		/// <summary>
		/// Clear the passed map mask flags.
		/// </summary>
		/// <param name="flags"></param>
		public void map_clear_mask (MAP_MASK flags)
		{
			int i, j;
			for (i = 0; i < map_w; i++)
				for (j = 0; j < map_h; j++) {
					if ((flags & MAP_MASK.F_FOG) == MAP_MASK.F_FOG)
						mask [i, j].fog = true;
					if ((flags & MAP_MASK.F_INVERSE_FOG) == MAP_MASK.F_INVERSE_FOG)
						mask [i, j].fog = false;
					if ((flags & MAP_MASK.F_SPOT) == MAP_MASK.F_SPOT)
						mask [i, j].spot = false;
					if ((flags & MAP_MASK.F_IN_RANGE) == MAP_MASK.F_IN_RANGE)
						mask [i, j].in_range = 0;
					if ((flags & MAP_MASK.F_MOUNT) == MAP_MASK.F_MOUNT)
						mask [i, j].mount = 0;
					if ((flags & MAP_MASK.F_SEA_EMBARK) == MAP_MASK.F_SEA_EMBARK)
						mask [i, j].sea_embark = false;
					if ((flags & MAP_MASK.F_AUX) == MAP_MASK.F_AUX)
						mask [i, j].aux = 0;
					if ((flags & MAP_MASK.F_INFL) == MAP_MASK.F_INFL)
						mask [i, j].infl = 0;
					if ((flags & MAP_MASK.F_INFL_AIR) == MAP_MASK.F_INFL_AIR)
						mask [i, j].air_infl = 0;
					if ((flags & MAP_MASK.F_VIS_INFL) == MAP_MASK.F_VIS_INFL)
						mask [i, j].vis_infl = 0;
					if ((flags & MAP_MASK.F_VIS_INFL_AIR) == MAP_MASK.F_VIS_INFL_AIR)
						mask [i, j].vis_air_infl = 0;
					if ((flags & MAP_MASK.F_BLOCKED) == MAP_MASK.F_BLOCKED)
						mask [i, j].blocked = false;
					if ((flags & MAP_MASK.F_BACKUP) == MAP_MASK.F_BACKUP)
						mask [i, j].backup = false;
					if ((flags & MAP_MASK.F_MERGE_UNIT) == MAP_MASK.F_MERGE_UNIT)
						mask [i, j].merge_unit = null;
					if ((flags & MAP_MASK.F_DEPLOY) == MAP_MASK.F_DEPLOY)
						mask [i, j].deploy = false;
					if ((flags & MAP_MASK.F_CTRL_GRND) == MAP_MASK.F_CTRL_GRND)
						mask [i, j].ctrl_grnd = 0;
					if ((flags & MAP_MASK.F_CTRL_AIR) == MAP_MASK.F_CTRL_AIR)
						mask [i, j].ctrl_air = 0;
					if ((flags & MAP_MASK.F_CTRL_SEA) == MAP_MASK.F_CTRL_SEA)
						mask [i, j].ctrl_sea = 0;
					if ((flags & MAP_MASK.F_MOVE_COST) == MAP_MASK.F_MOVE_COST)
						mask [i, j].moveCost = 0;
					if ((flags & MAP_MASK.F_DISTANCE) == MAP_MASK.F_DISTANCE)
						mask [i, j].distance = -1;
					if ((flags & MAP_MASK.F_DANGER) == MAP_MASK.F_DANGER)
						mask [i, j].danger = false;
					if ((flags & MAP_MASK.F_SPLIT_UNIT) == MAP_MASK.F_SPLIT_UNIT) {
						mask [i, j].split_unit = null;
						mask [i, j].split_okay = false;
					}
				}
		}

		/// <summary>
		///         Swap units. Returns the previous unit or 0 if none.
		/// </summary>
		/// <param name="unit"></param>
		/// <returns></returns>
		public Unit map_swap_unit (Unit unit)
		{
			Unit old;
			if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) {
				old = map_tile (unit.x, unit.y).a_unit;
				map_tile (unit.x, unit.y).a_unit = unit;
			} else {
				old = map_tile (unit.x, unit.y).g_unit;
				map_tile (unit.x, unit.y).g_unit = unit;
			}
			unit.terrain = map [unit.x, unit.y].terrain;
			return old;
		}

		/// <summary>
		/// Insert, Remove unit pointer from map.
		/// </summary>
		/// <param name="unit"></param>
		public void map_insert_unit (Unit unit)
		{
			Unit old = map_swap_unit (unit);
			map_tile (unit.x, unit.y).backupUnit = old;
		}

		public void map_remove_unit (Unit unit)
		{

			if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
				map_tile (unit.x, unit.y).a_unit = null;
			else
				map_tile (unit.x, unit.y).g_unit = map_tile (unit.x, unit.y).backupUnit;
			map_tile (unit.x, unit.y).backupUnit = null;
		}

		/// <summary>
		/// Get neighbored tiles clockwise with id between 0 and 5.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public Map_Tile map_get_close_hex (int x, int y, int id)
		{
			int next_x, next_y;
			if (Misc.get_close_hex_pos (x, y, id, out next_x, out next_y))
				return map [next_x, next_y];
			return null;
		}

		private void map_add_unit_spot_mask_rec (Unit unit, int x, int y, int points)
		{
			int i, next_x, next_y;
			/* break if this tile is already spotted */
			if (mask [x, y].aux >= points)
				return;
			/* spot tile */
			mask [x, y].aux = points;
			/* substract points */
			points -= map [x, y].terrain.spt [Scenario.cur_weather];
			/* if there are points remaining continue spotting */
			if (points > 0)
				for (i = 0; i < 6; i++)
					if (Misc.get_close_hex_pos (x, y, i, out next_x, out next_y))
					if (!((map [next_x, next_y].terrain.flags [Scenario.cur_weather] & Terrain_flags.NO_SPOTTING) == Terrain_flags.NO_SPOTTING))
						map_add_unit_spot_mask_rec (unit, next_x, next_y, points);
		}

		/// <summary>
		/// Add/set spotting of a unit to auxiliary mask
		/// </summary>
		/// <param name="unit"></param>
		public void map_add_unit_spot_mask (Unit unit)
		{
			int i, next_x, next_y;
			if (unit.x < 0 || unit.y < 0 || unit.x >= map_w || unit.y >= map_h)
				return;
			mask [unit.x, unit.y].aux = unit.sel_prop.spt;
			for (i = 0; i < 6; i++)
				if (Misc.get_close_hex_pos (unit.x, unit.y, i, out next_x, out next_y))
					map_add_unit_spot_mask_rec (unit, next_x, next_y, unit.sel_prop.spt);
		}

		public void map_get_unit_spot_mask (Unit unit)
		{
			map_clear_mask (MAP_MASK.F_AUX);
			map_add_unit_spot_mask (unit);
		}

		/*
        ====================================================================
        Check whether unit can enter (x,y) provided it has 'points' move
        points remaining. 'mounted' means, to use the base cost for the 
        transporter. Return the fuel cost (<=points) of this. If entering
        is not possible, 'cost' is undefined.
        ====================================================================
        */
		public bool unit_can_enter_hex (Unit unit, int x, int y, bool is_close, int points, bool mounted, out int cost)
		{
			cost = 0;
			int baseCost = Terrain.GetMovementCost (map [x, y].terrain, unit.sel_prop.mov_type, Scenario.cur_weather);
			/* if we check the mounted case, we'll have to use the ground transporter's cost */
			if (mounted && (unit.trsp_prop != null) && !string.IsNullOrEmpty (unit.trsp_prop.id))
				baseCost = Terrain.GetMovementCost (map [x, y].terrain, unit.trsp_prop.mov_type, Scenario.cur_weather);
			/* allied bridge engineers on river? */
			if ((map [x, y].terrain.flags [Scenario.cur_weather] & Terrain_flags.RIVER) == Terrain_flags.RIVER)
			if (map [x, y].g_unit != null && (map [x, y].g_unit.sel_prop.flags & UnitFlags.BRIDGE_ENG) == UnitFlags.BRIDGE_ENG)
			if (Player.player_is_ally (unit.player, map [x, y].g_unit.player))
				baseCost = 1;
			/* impassable? */
			if (baseCost == 0)
				return false;
			/* cost's all but not close? */
			if (baseCost == -1 && !is_close)
				return false;
			/* not enough points left? */
			if (baseCost > 0 && points < baseCost)
				return false;
			/* you can move over allied units but then mask::blocked must be set
             * because you must not stop at this tile */
			if ((x != unit.x || y != unit.y) && mask [x, y].spot) {
				if (map [x, y].a_unit != null && (unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) {
					if (!Player.player_is_ally (unit.player, map [x, y].a_unit.player))
						return false;
					else
						map_mask_tile (x, y).blocked = true;
				}
				if (map [x, y].g_unit != null && !((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)) {
					if (!Player.player_is_ally (unit.player, map [x, y].g_unit.player))
						return false;
					else
						map_mask_tile (x, y).blocked = true;
				}
			}
			/* if we already have to spent all; we are done */
			if (baseCost == -1) {
				cost = points;
				return true;
			}
			/* entering an influenced tile costs all remaining points */
			cost = baseCost;
			if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) {
				if (mask [x, y].vis_air_infl > 0)
					cost = points;
			} else {
				if (mask [x, y].vis_infl > 0)
					cost = points;
			}
			return true;
		}

		/*
        ====================================================================
        Check whether hex (x,y) is reachable by the unit. 'distance' is the
        distance to the hex, the unit is standing on. 'points' is the number
        of points the unit has remaining before trying to enter (x,y).
        'mounted' means to re-check with the move mask of the transporter.
        And to set mask[x, y].mount if a tile came in reach that was 
        previously not.
        ====================================================================
        */
		void map_add_unit_move_mask_rec (Unit unit, int x, int y, int distance, int points, bool mounted)
		{
			int i, next_x, next_y, cost = 0;
			/* break if this tile is already checked */
			if (mask [x, y].in_range >= points)
				return;
			if (mask [x, y].sea_embark)
				return;
			/* the outer map tiles may not be entered */
			if (x <= 0 || y <= 0 || x >= map_w - 1 || y >= map_h - 1)
				return;
			/* can we enter? if yes, how much does it cost? */
			if (distance == 0 || unit_can_enter_hex (unit, x, y, (distance == 1), points, mounted, out cost)) {
				/* remember distance */
				if (mask [x, y].distance == -1 || distance < mask [x, y].distance)
					mask [x, y].distance = distance;
				distance = mask [x, y].distance;
				/* re-check: after substracting the costs, there must be more points left
                   than previously */
				if (mask [x, y].in_range >= points - cost)
					return;
				/* enter tile new or with more points */
				points -= cost;
				if (mounted && mask [x, y].in_range == -1) {
					mask [x, y].mount = 1;
					mask [x, y].moveCost = unit.trsp_prop.mov - points;
				}
				mask [x, y].in_range = points;
				/* get total move costs (basic unmounted) */
				if (!mounted)
					mask [x, y].moveCost = unit.sel_prop.mov - points;
			} else
				points = 0;
			/* all points consumed? if so, we can't go any further */
			if (points == 0)
				return;
			/* check whether close hexes in range */
			for (i = 0; i < 6; i++)
				if (Misc.get_close_hex_pos (x, y, i, out next_x, out next_y)) {
					if (distance == 0 && map_check_unit_embark (unit, next_x, next_y, UnitEmbarkTypes.EMBARK_SEA, false)) {
						/* unit may embark to sea transporter */
						mask [next_x, next_y].sea_embark = true;
						continue;
					}
					if (distance == 0 && map_check_unit_debark (unit, next_x, next_y, UnitEmbarkTypes.EMBARK_SEA, false)) {
						/* unit may debark from sea transporter */
						mask [next_x, next_y].sea_embark = true;
						continue;
					}
					map_add_unit_move_mask_rec (unit, next_x, next_y, distance + 1, points, mounted);
				}
		}

		/// <summary>
		/// Set movement range of a unit to in_range/sea_embark/mount.
		/// </summary>
		/// <param name="unit"></param>
		public void map_get_unit_move_mask (Unit unit)
		{
			int x, y;
			map_clear_unit_move_mask ();
			/* we keep the semantic change of in_range local by doing a manual adjustment */
			for (x = 0; x < map_w; x++)
				for (y = 0; y < map_h; y++)
					mask [x, y].in_range = -1;
			if (unit.embark == UnitEmbarkTypes.EMBARK_NONE &&
                unit.trsp_prop != null && !string.IsNullOrEmpty (unit.trsp_prop.id)) {
				/* this goes wrong if a transportable unit may move in intervals which 
                   is logically correct however (for that case automatic mount is not 
                   possible, the user'd have to explicitly mount/unmount before starting
                   to move); so all units should move in one go */

				//begin patch: we need to consider if our range is restricted by lack of fuel -trip
				//map_add_unit_move_mask_rec(unit,unit.x,unit.y,0,unit.prop.mov,0);
				//map_add_unit_move_mask_rec(unit,unit.x,unit.y,0,unit.trsp_prop.mov,1);
				int maxpoints = unit.prop.mov;
				if (unit.cur_fuel < maxpoints) {
					maxpoints = unit.cur_fuel;
					//printf("limiting movement because fuel = %d\n", unit.cur_fuel);
				}
				map_add_unit_move_mask_rec (unit, unit.x, unit.y, 0, maxpoints, false);
				/* fix for crashing when don't have enough fuel to use the land transport's full range -trip */
				maxpoints = unit.trsp_prop.mov;
				if (unit.cur_fuel < maxpoints) {
					maxpoints = unit.cur_fuel;
					//printf("limiting expansion of movement via transport because fuel = %d\n", unit.cur_fuel);
				}
				map_add_unit_move_mask_rec (unit, unit.x, unit.y, 0, maxpoints, true);
				//end of patch -trip

			} else
				map_add_unit_move_mask_rec (unit, unit.x, unit.y, 0, unit.cur_mov, false);
			for (x = 0; x < map_w; x++)
				for (y = 0; y < map_h; y++)
					mask [x, y].in_range++;
		}

		public void map_clear_unit_move_mask ()
		{
			map_clear_mask (MAP_MASK.F_IN_RANGE | MAP_MASK.F_MOUNT | MAP_MASK.F_SEA_EMBARK |
                           MAP_MASK.F_BLOCKED | MAP_MASK.F_AUX | MAP_MASK.F_MOVE_COST | MAP_MASK.F_DISTANCE);
		}


		/*
        ====================================================================
        Writes into the given array the coordinates of all friendly
        airports, returning the number of coordinates written.
        The array must be big enough to hold all the coordinates.
        The function is guaranteed to never write more than (map_w*map_h)
        entries.
        ====================================================================
        */
		public int map_write_friendly_depot_list (Unit unit, List<MapCoord> coords)
		{
			short x, y;
			MapCoord p;
			int count = 0;
			for (x = 0; x < map_w; x++)
				for (y = 0; y < map_h; y++)
					if (map_is_allied_depot (map [x, y], unit)) {
						p = new MapCoord ();
						p.x = x;
						p.y = y;
						coords.Add (p);
						count++;
					}
			return count;
		}

		/*
        ====================================================================
        Sets the distance mask beginning with the airfield at (ax, ay).
        ====================================================================
        */
		public void map_get_dist_air_mask (int ax, int ay, short[,] dist_air_mask)
		{
			int x, y;
			for (x = 0; x < map_w; x++)
				for (y = 0; y < map_h; y++) {
					short d = (short)(Misc.get_dist (ax, ay, x, y) - 1);
					if (d < dist_air_mask [x, y])
						dist_air_mask [x, y] = d;
				}
			dist_air_mask [ax, ay] = 0;
		}

		/// <summary>
		/// Recreates the danger mask for 'unit'.
		/// The fog must be set to the movement range of 'unit' for this
		/// function to work properly.
		/// The movement cost of the mask must have been set for 'unit'.
		/// Returns true when at least one tile's danger mask was set, otherwise false.
		/// </summary>
		/// <param name="unit"></param>
		/// <returns></returns>
#if TODO_RR
		public bool map_get_danger_mask (Unit unit)
		{
			int x, y;
			bool retval = false;
			short[,] dist_air_mask = new short[map_w, map_h];
			List<MapCoord> airfields = new List<MapCoord> ();
			int airfield_count = map_write_friendly_depot_list (unit, airfields);

			/* initialise masks */
			for (int i = 0; i < map_w; i++)
				for (int j = 0; j < map_h; j++)
					dist_air_mask [i, j] = DIST_AIR_MAX;

			/* gather distance mask considering all friendly airfields */
			for (int i = 0; i < airfield_count; i++)
				map_get_dist_air_mask (airfields [i].x, airfields [i].y, dist_air_mask);

			/* now mark as danger-zone any tile whose next friendly airfield is
               farther away than the fuel quantity left. */
			for (x = 0; x < map_w; x++)
				for (y = 0; y < map_h; y++)
					if (!mask [x, y].fog) {
						int left = unit.cur_fuel - unit.CalcFuelUsage (mask [x, y].distance);
						retval |=
                        mask [x, y].danger =
						/* First compare distance to prospected fuel qty. If it's
                             * too far away, it's dangerous.
                             */
                            dist_air_mask [x, y] > left
						/* Specifically allow supplied tiles.
                             */
                            && (map_get_unit_supply_level (x, y, unit) == 0);
					}
			return retval;
		}
#endif

		/// <summary>
		/// Get a list of way points the unit moves along to it's destination.
		/// This includes check for unseen influence by enemy units (e.g.
		/// Surprise Contact).
		/// </summary>
		/// <param name="unit"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="count"></param>
		/// <param name="ambush_unit"></param>
		/// <returns></returns>
		public Way_Point[] map_get_unit_way_points (Unit unit, int x, int y, out int count, out Unit ambush_unit)
		{
			count = 0;
			ambush_unit = null;
			Way_Point[] way = null, reverse = null;
			int i;
			int next_x, next_y;
			/* same tile ? */
			if (unit.x == x && unit.y == y)
				return null;
			/* allocate memory */
			way = new Way_Point[unit.cur_mov + 1];
			reverse = new Way_Point[unit.cur_mov + 1];
			/* it's easiest to get positions in reverse order */
			next_x = x;
			next_y = y;
			count = 0;
			while (next_x != unit.x || next_y != unit.y) {
				reverse [count] = new Way_Point ();
				reverse [count].x = next_x;
				reverse [count].y = next_y;
				map_get_next_unit_point (next_x, next_y, out next_x, out next_y);
				(count)++;
			}
			reverse [count] = new Way_Point ();
			reverse [count].x = unit.x;
			reverse [count].y = unit.y;
			(count)++;
			for (i = 0; i < count; i++) {
				way [i] = new Way_Point ();
				way [i].x = reverse [(count) - 1 - i].x;
				way [i].y = reverse [(count) - 1 - i].y;
			}
			/* debug way points
            printf( "'%s': %i,%i", unit.name, way[0].x, way[0].y );
            for ( i = 1; i < *count; i++ )
                printf( " . %i,%i", way[i].x, way[i].y );
            printf( "\n" ); */
			/* check for ambush and influence
             * if there is a unit in the way it must be an enemy (friends, spotted enemies are not allowed)
             * so cut down way to this way_point and set ambush_unit
             * if an unspotted tile does have influence >0 an enemy is nearby and our unit must stop
             */
			for (i = 1; i < count; i++) {
				/* check if on this tile a unit is waiting */
				/* if mask::blocked is set it's an own unit so don't check for ambush */
				if (!map_mask_tile (way [i].x, way [i].y).blocked) {
					if (map_tile (way [i].x, way [i].y).g_unit != null)
					if ((unit.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING) {
						ambush_unit = map_tile (way [i].x, way [i].y).g_unit;
						break;
					}
					if (map_tile (way [i].x, way [i].y).a_unit != null)
					if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) {
						ambush_unit = map_tile (way [i].x, way [i].y).a_unit;
						break;
					}
				}
				/* if we get here there is no unit waiting but maybe close too the tile */
				/* therefore check tile of moving unit if it is influenced by a previously unspotted unit */
				if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) {
					if (map_mask_tile (way [i - 1].x, way [i - 1].y).air_infl != 0 &&
                        map_mask_tile (way [i - 1].x, way [i - 1].y).vis_air_infl == 0)
						break;
				} else {
					if (map_mask_tile (way [i - 1].x, way [i - 1].y).infl != 0 &&
                        map_mask_tile (way [i - 1].x, way [i - 1].y).vis_infl == 0)
						break;
				}
			}
			if (i < count)
				count = i; /* enemy in the way; cut down */
			return way;
		}

		/*
        ====================================================================
        Backup/restore spot mask to/from backup mask. Used for Undo Turn.
        ====================================================================
        */
		public void map_backup_spot_mask ()
		{
			map_clear_mask (MAP_MASK.F_BACKUP);
			for (int x = 0; x < map_w; x++)
				for (int y = 0; y < map_h; y++)
					map_mask_tile (x, y).backup = map_mask_tile (x, y).spot;
		}

		public void map_restore_spot_mask ()
		{
			for (int x = 0; x < map_w; x++)
				for (int y = 0; y < map_h; y++)
					map_mask_tile (x, y).spot = map_mask_tile (x, y).backup;
			map_clear_mask (MAP_MASK.F_BACKUP);
		}

		/// <summary>
		/// Get unit's merge partners and set mask 'merge'.
		/// At maximum MAP_MERGE_UNIT_LIMIT units.
		/// All unused entries in partners are set 0.
		/// </summary>
		public const int MAP_MERGE_UNIT_LIMIT = 6;

		public void map_get_merge_units (Unit unit, out Unit[] partners, out int count)
		{
			Unit partner;
			int i, next_x, next_y;
			count = 0;
			map_clear_mask (MAP_MASK.F_MERGE_UNIT);
			partners = new Unit[MAP_MERGE_UNIT_LIMIT];
			/* check surrounding tiles */
			for (i = 0; i < 6; i++)
				if (Misc.get_close_hex_pos (unit.x, unit.y, i, out next_x, out next_y)) {
					partner = null;
					if (map [next_x, next_y].g_unit != null && unit.CheckMerge (map [next_x, next_y].g_unit))
						partner = map [next_x, next_y].g_unit;
					else if (map [next_x, next_y].a_unit != null && unit.CheckMerge (map [next_x, next_y].a_unit))
						partner = map [next_x, next_y].a_unit;
					if (partner != null) {
						partners [(count)++] = partner;
						mask [next_x, next_y].merge_unit = partner;
					}
				}
		}


		/// <summary>
		/// Check if unit may transfer strength to unit (if not NULL) or create
		/// a stand alone unit (if unit NULL) on the coordinates.
		/// </summary>
		/// <param name="unit"></param>
		/// <param name="str"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="dest"></param>
		/// <returns></returns>
		public bool map_check_unit_split (Unit unit, int str, int x, int y, Unit dest)
		{
			if (unit.str - str < 4)
				return false;
			if (dest != null) {
				int old_str;
				bool ret;
				old_str = unit.str;
				unit.str = str;
				ret = unit.CheckMerge (dest); /* is equal for now */
				unit.str = old_str;
				return ret;
			} else {
				if (str < 4)
					return false;
				if (!Misc.is_close (unit.x, unit.y, x, y))
					return false;
				if (((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) && map [x, y].a_unit != null)
					return false;
				if (((unit.sel_prop.flags & UnitFlags.FLYING) != UnitFlags.FLYING) && map [x, y].g_unit != null)
					return false;
				if (Terrain.GetMovementCost (map [x, y].terrain, unit.sel_prop.mov_type, Scenario.cur_weather) == 0)
					return false;
			}
			return true;
		}

		/*
        ====================================================================
        Get unit's split partners assuming unit wants to give 'str' strength
        points and set mask 'split'. At maximum MAP_SPLIT_UNIT_LIMIT units.
        All unused entries in partners are set 0. 'str' must be valid amount,
        this is not checked here.
        ====================================================================
        */
		public const int MAP_SPLIT_UNIT_LIMIT = 6;

		public void map_get_split_units_and_hexes (Unit unit, int str, out Unit[] partners, out int count)
		{
#if TODO
            Unit* partner;
            int i, next_x, next_y;
            count = 0;
            map_clear_mask(F_SPLIT_UNIT);
            partners = new Unit[MAP_SPLIT_UNIT_LIMIT];
            /* check surrounding tiles */
            for (i = 0; i < 6; i++)
                if (get_close_hex_pos(unit.x, unit.y, i, &next_x, &next_y))
                {
                    partner = 0;
                    if (map[next_x, next_y].g_unit && map_check_unit_split(unit, str, next_x, next_y, map[next_x, next_y].g_unit))
                        partner = map[next_x, next_y].g_unit;
                    else
                        if (map[next_x, next_y].a_unit && map_check_unit_split(unit, str, next_x, next_y, map[next_x, next_y].a_unit))
                            partner = map[next_x, next_y].a_unit;
                        else
                            if (map_check_unit_split(unit, str, next_x, next_y, 0))
                                mask[next_x, next_y].split_okay = 1;
                    if (partner)
                    {
                        partners[(count)++] = partner;
                        mask[next_x, next_y].split_unit = partner;
                    }
                }
#endif
			throw new NotImplementedException ();
		}

		/*
        ====================================================================
        Get a list (vis_units) of all visible units by checking spot mask.
        ====================================================================
        */
		public void map_get_vis_units ()
		{
			int x, y;
			for (x = 0; x < map_w; x++)
				for (y = 0; y < map_h; y++)
					if (mask [x, y].spot || (Engine.cur_player != null && Engine.cur_player.ctrl == PLAYERCONTROL.PLAYER_CTRL_CPU)) {
						if (map [x, y].g_unit != null)
							Scenario.vis_units.Add (map [x, y].g_unit);
						if (map [x, y].a_unit != null)
							Scenario.vis_units.Add (map [x, y].a_unit);
					}
		}

		/// <summary>
		/// Draw a map tile terrain to surface. (fogged if mask::fog is set)
		/// </summary>
		/// <param name="surf"></param>
		/// <param name="map_x"></param>
		/// <param name="map_y"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>

		public SDL_Surface map_draw_terrain (int map_x, int map_y)
		{
			int cur_weather = 0;
			int offset;
			string path;
			Map_Tile tile;
			if (map_x < 0 || map_y < 0 || map_x >= map_w || map_y >= map_h)
				throw new Exception ("Position of the tile out of the map");
			tile = map [map_x, map_y];
			if (tile.terrain.name.ToLower () == "mountain") {
				int numT = TextureTable.elegirImgTex (tile.strat_image_offset);
				path = Config.pathTexTerrain + tile.terrain.name.ToLower () + numT;
				offset = 0;
				if (numT == 1) {
					offset = tile.strat_image_offset * Config.hex_w - Config.hex_w;
				} else {
					offset = (tile.strat_image_offset - 39) * Config.hex_w - Config.hex_w;
				}
			
			} else {
				path = Config.pathTexTerrain + tile.terrain.name.ToLower ();
				offset = (tile.strat_image_offset * Config.hex_w) - Config.hex_w;			
			}
			SDL_Surface terraintex = SDL_Surface.LoadSurface (path, false);
			SDL_Surface hextex = new SDL_Surface ();
			SDL_Surface.copy_image (hextex, Config.hex_w, Config.hex_h, terraintex, offset, 0);
			//Add texture flag
			if (tile.nation != null) {
				hextex = Nation.nation_draw_flag (tile.nation, hextex);
			} 
			return hextex;
#if TODO_RR			
			/* terrain */
			if (mask [map_x, map_y].fog) {
				SDL_Surface sdl = SDL_Surface.LoadSurface(tile.terrain.images_fogged [cur_weather]);
				surf.surf.DrawImage (sdl.bitmap, x, y, new Rectangle (tile.image_offset, 0, hex_w, hex_h), GraphicsUnit.Pixel);
			} else {
				SDL_Surface sdl = tile.terrain.images [cur_weather];
				surf.surf.DrawImage (sdl.bitmap, x, y, new Rectangle (tile.image_offset, 0, hex_w, hex_h), GraphicsUnit.Pixel);
			}
			
			/* grid */
			if (Config.grid) {
				SDL_Surface.copy_image (surf, x, y, hex_w, hex_h, Engine.terrain.terrainIcons.grid, 0, 0);
			}
#endif
		}

		/// <summary>
		/// Draw tile units. If mask::fog is set no units are drawn.
		/// If 'ground' is True the ground unit is drawn as primary
		/// and the air unit is drawn small (and vice versa).
		/// If 'select' is set a selection frame is added.
		/// </summary>
		/// <param name="surf"></param>
		/// <param name="map_x"></param>
		/// <param name="map_y"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="ground"></param>
		/// <param name="select"></param>
		public SDL_Surface map_draw_units (SDL_Surface hexTex, int map_x, int map_y,bool ground)
		{
			Player cur_player = Engine.cur_player;
			Unit unit = null;
			Map_Tile tile;
			
			if (map_x < 0 || map_y < 0 || map_x >= map_w || map_y >= map_h)
				throw new Exception ("Position out of map");
			tile = map [map_x, map_y];
			/* units */
			if (MAP_CHECK_VIS (map_x, map_y)) {
				if (tile.g_unit != null) {
					unit = tile.g_unit;
					bool resize = (ground || tile.a_unit == null)?false:true;
					draw_unit_on_texture(hexTex,unit,resize);
				}
				if (tile.a_unit != null) {
					unit = tile.a_unit;
					bool resize = (!ground || tile.g_unit == null)?false:true;
					draw_unit_on_texture(hexTex,unit,resize);
				}
				/* unit info icons */
				if (unit != null && Config.show_bar) {
#if TODO_RR
					if ((cur_player != null) && Player.player_is_ally (cur_player, unit.player)){
#endif
					/* strength */
					if (unit.player.ctrl==PLAYERCONTROL.PLAYER_CTRL_HUMAN){
						string name = Unit.DeleteOrdinal (unit.name);
						SDL_Surface sdl_str = SDL_Surface.LoadSurface(DB.UnitLib.unit_info_icons.str_img_name,false);
						int offset = DB.UnitLib.unit_info_icons.str_h*(unit.str+15);
						offset = sdl_str.h-offset;
						SDL_Surface str = new SDL_Surface();
						SDL_Surface.copy_image(str,DB.UnitLib.unit_info_icons.str_w,
												DB.UnitLib.unit_info_icons.str_h,sdl_str,0,offset);
						int xdest = (Config.hex_w-DB.UnitLib.unit_info_icons.str_w)/2;
						SDL_Surface.copy_image(hexTex,str,xdest,3,DB.UnitLib.unit_info_icons.str_w,
											   DB.UnitLib.unit_info_icons.str_h,0,0);

						
					}
					else{
						string name = Unit.DeleteOrdinal (unit.name);
						SDL_Surface sdl_str = SDL_Surface.LoadSurface(DB.UnitLib.unit_info_icons.str_img_name,false);
						int offset = DB.UnitLib.unit_info_icons.str_h*(unit.str);
						offset = sdl_str.h-offset;
						SDL_Surface str = new SDL_Surface();
						SDL_Surface.copy_image(str,DB.UnitLib.unit_info_icons.str_w,
												DB.UnitLib.unit_info_icons.str_h,sdl_str,0,offset);
						int xdest = (Config.hex_w-DB.UnitLib.unit_info_icons.str_w)/2;
						SDL_Surface.copy_image(hexTex,str,xdest,3,DB.UnitLib.unit_info_icons.str_w,
											   DB.UnitLib.unit_info_icons.str_h,0,0);
						
					}
					/* for current player only */
#if TODO_RR
					if (unit.player == cur_player) {
#endif
					if (unit.player.ctrl==PLAYERCONTROL.PLAYER_CTRL_HUMAN){
						string name = Unit.DeleteOrdinal(unit.name);
						Unit_Lib_Entry entry = DB.UnitLib.unit_lib_find_by_name(name);
						/* attack */
#if TODO_RR
						if (unit.cur_atk_count > 0) {
#endif
						if (entry.atk_count>0){
							SDL_Surface atk = SDL_Surface.LoadSurface(DB.UnitLib.unit_info_icons.atk_img_name,false);
							SDL_Surface.copy_image_without_key(hexTex,atk,15,3,Color.black);
						}
						/* move */
						if (entry.mov > 0) {
							SDL_Surface mov = SDL_Surface.LoadSurface(DB.UnitLib.unit_info_icons.mov_img_name,false);
							SDL_Surface.copy_image_without_key(hexTex,mov,37,3,Color.black);		
						}
#if TODO_RR
						/* guarding */
						if (unit.is_guarding) {
							SDL_Surface.copy_image (surf.surf,
                             x + ((hex_w - DB.UnitLib.unit_info_icons.guard.w) >> 1),
                             y + hex_h - DB.UnitLib.unit_info_icons.guard.h,
                             DB.UnitLib.unit_info_icons.guard.w, DB.UnitLib.unit_info_icons.guard.h,
                             DB.UnitLib.unit_info_icons.guard, 0, 0);

						}
#endif
					}
				}
			}

#if TODO_RR
					}
				}
			}
			/* selection frame */
			if (select) {
				SDL_Surface.copy_image (surf.surf,
                                        x, y, hex_w, hex_h,
                                        Engine.terrain.terrainIcons.select, 0, 0);

			}
#endif
			return hexTex;
		}
		/// <summary>
		/// Draw danger tile. Expects 'surf' to contain a fully drawn tile at
		/// the given position which will be tinted by overlaying the danger
		/// terrain surface.
		/// </summary>
		/// <param name="surf"></param>
		/// <param name="map_x"></param>
		/// <param name="map_y"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
#if TODO_RR
		public void map_apply_danger_to_tile (SDL_Surface surf, int map_x, int map_y, int x, int y)
		{
			SDL_Surface.copy_image (surf, x, y, Engine.terrain.hex_w, Engine.terrain.hex_h,
                                   Engine.terrain.terrainIcons.danger, 0, 0, (int)FOG_ALPHA.DANGER_ALPHA);
		}
#endif

		/// <summary>
		/// Draw terrain and units.
		/// </summary>
		/// <param name="surf"></param>
		/// <param name="map_x"></param>
		/// <param name="map_y"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="ground"></param>
		/// <param name="select"></param>
#if TODO_RR
		public void map_draw_tile (SDL_Surface surf, int map_x, int map_y, int x, int y, bool ground, bool select)
		{
			map_draw_terrain (surf, map_x, map_y, x, y);
			map_draw_units (surf, map_x, map_y, x, y, ground, select);
		}
#endif
		/*
        ====================================================================
        Set/update spot mask by engine's current player or unit.
        The update adds the tiles seen by unit.
        ====================================================================
        */
#if TODO_RR
		public void map_set_spot_mask ()
		{
			int x, y, next_x, next_y;
			Unit unit;
			map_clear_mask (MAP_MASK.F_SPOT);
			map_clear_mask (MAP_MASK.F_AUX); /* buffer here first */
			/* get spot_mask for each unit and add to fog */
			/* use map::mask::aux as buffer */
			for (int i = 0; i < Scenario.units.Count; i++) {
				unit = Scenario.units [i];
				if (unit.killed != 0)
					continue;
				if (Player.player_is_ally (Engine.cur_player, unit.player)) /* it's your unit or at least it's allied... */
					map_add_unit_spot_mask (unit);
			}
			/* check all flags; if flag belongs to you or any of your partners you see the surrounding, too */
			for (x = 0; x < map_w; x++)
				for (y = 0; y < map_h; y++)
					if (map [x, y].player != null)
					if (Player.player_is_ally (Engine.cur_player, map [x, y].player)) {
						mask [x, y].aux = 1;
						for (int i = 0; i < 6; i++)
							if (Misc.get_close_hex_pos (x, y, i, out next_x, out next_y))
								mask [next_x, next_y].aux = 1;
					}
			/* convert aux to fog */
			for (x = 0; x < map_w; x++)
				for (y = 0; y < map_h; y++)
					if (mask [x, y].aux != 0 || !Config.fog_of_war)
						mask [x, y].spot = true;
			/* update the visible units list */
			map_get_vis_units ();
		}
#endif
		public void map_update_spot_mask (Unit unit, out bool enemy_spotted)
		{
			int x, y;
			enemy_spotted = false;
			if (Player.player_is_ally (Engine.cur_player, unit.player)) {
				/* it's your unit or at least it's allied... */
				map_get_unit_spot_mask (unit);
				for (x = 0; x < map_w; x++)
					for (y = 0; y < map_h; y++)
						if (mask [x, y].aux != 0) {
							/* if there is an enemy in this auxialiary mask that wasn't spotted before */
							/* set enemy_spotted true */
							if (!mask [x, y].spot) {
								if (map [x, y].g_unit != null && !Player.player_is_ally (unit.player, map [x, y].g_unit.player))
									enemy_spotted = true;
								if (map [x, y].a_unit != null && !Player.player_is_ally (unit.player, map [x, y].a_unit.player))
									enemy_spotted = true;
							}
							mask [x, y].spot = true;
						}
			}
		}


		/*
        ====================================================================
        Set mask::fog (which is the actual fog of the engine) to either
        spot mask, in_range mask (covers sea_embark), merge mask,
        deploy mask.
        ====================================================================
        */
		public void map_set_fog (MAP_MASK type)
		{
			int x, y;
			for (y = 0; y < map_h; y++)
				for (x = 0; x < map_w; x++) {
					switch (type) {
					case MAP_MASK.F_SPOT:
						mask [x, y].fog = !mask [x, y].spot;
						break;
					case MAP_MASK.F_IN_RANGE:
						mask [x, y].fog = ((mask [x, y].in_range == 0 && !mask [x, y].sea_embark) || mask [x, y].blocked);
						break;
					case MAP_MASK.F_MERGE_UNIT:
						mask [x, y].fog = (mask [x, y].merge_unit == null);
						break;
					case MAP_MASK.F_SPLIT_UNIT:
						mask [x, y].fog = (mask [x, y].split_unit == null) && !mask [x, y].split_okay;
						break;
					case MAP_MASK.F_DEPLOY:
						mask [x, y].fog = !mask [x, y].deploy;
						break;
					default:
						mask [x, y].fog = false;
						break;
					}
				}
		}
		/*
        ====================================================================
        Set the fog to players spot mask by using mask::aux (not mask::spot)
        ====================================================================
        */
#if TODO_RR
		public void map_set_fog_by_player (Player player)
		{
			int next_x, next_y;
			Unit unit;
			map_clear_mask (MAP_MASK.F_AUX); /* buffer here first */
			/* units */
			for (int i = 0; i < Scenario.units.Count; i++) {
				unit = Scenario.units [i];
				if (unit.killed != 0)
					continue;
				if (Player.player_is_ally (player, unit.player)) /* it's your unit or at least it's allied... */
					map_add_unit_spot_mask (unit);
			}
			/* check all flags; if flag belongs to you or any of your partners you see the surrounding, too */
			for (int x = 0; x < map_w; x++)
				for (int y = 0; y < map_h; y++)
					if (Engine.map.map [x, y].player != null)
					if (Player.player_is_ally (player, map [x, y].player)) {
						mask [x, y].aux = 1;
						for (int i = 0; i < 6; i++)
							if (Misc.get_close_hex_pos (x, y, i, out next_x, out next_y))
								mask [next_x, next_y].aux = 1;
					}
			/* convert aux to fog */
			for (int x = 0; x < map_w; x++)
				for (int y = 0; y < map_h; y++)
					if (mask [x, y].aux != 0 || !Config.fog_of_war)
						mask [x, y].fog = false;
					else
						mask [x, y].fog = true;
		}
#endif
		/*
        ====================================================================
        Check if this map tile is visible to the engine (isn't covered
        by mask::fog or mask::spot as modification is allowed and it may be
        another's player fog (e.g. one human against cpu))
        ====================================================================
        */
		// #define MAP_CHECK_VIS( mapx, mapy ) ( ( !modify_fog && !mask[mapx, mapy].fog ) || ( modify_fog && mask[mapx, mapy].spot ) )
		public bool MAP_CHECK_VIS (int mapx, int mapy)
		{
			return (!Engine.modify_fog && !mask [mapx, mapy].fog) || (Engine.modify_fog && mask [mapx, mapy].spot);
		}

		/*
        ====================================================================
        Modify the various influence masks.
        ====================================================================
        */
		public void map_add_unit_infl (Unit unit)
		{
			int next_x, next_y;
			if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) {
				mask [unit.x, unit.y].air_infl++;
				for (int i = 0; i < 6; i++)
					if (Misc.get_close_hex_pos (unit.x, unit.y, i, out next_x, out next_y))
						mask [next_x, next_y].air_infl++;
			} else {
				mask [unit.x, unit.y].infl++;
				for (int i = 0; i < 6; i++)
					if (Misc.get_close_hex_pos (unit.x, unit.y, i, out next_x, out next_y))
						mask [next_x, next_y].infl++;
			}
		}

		public void map_remove_unit_infl (Unit unit)
		{
			int next_x, next_y;
			if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) {
				mask [unit.x, unit.y].air_infl--;
				for (int i = 0; i < 6; i++)
					if (Misc.get_close_hex_pos (unit.x, unit.y, i, out next_x, out next_y))
						mask [next_x, next_y].air_infl--;
			} else {
				mask [unit.x, unit.y].infl--;
				for (int i = 0; i < 6; i++)
					if (Misc.get_close_hex_pos (unit.x, unit.y, i, out next_x, out next_y))
						mask [next_x, next_y].infl--;
			}
		}

		public void map_remove_vis_unit_infl (Unit unit)
		{
			int next_x, next_y;
			if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) {
				mask [unit.x, unit.y].vis_air_infl--;
				for (int i = 0; i < 6; i++)
					if (Misc.get_close_hex_pos (unit.x, unit.y, i, out next_x, out next_y))
						mask [next_x, next_y].vis_air_infl--;
			} else {
				mask [unit.x, unit.y].vis_infl--;
				for (int i = 0; i < 6; i++)
					if (Misc.get_close_hex_pos (unit.x, unit.y, i, out next_x, out next_y))
						mask [next_x, next_y].vis_infl--;
			}
		}

		public void map_set_infl_mask ()
		{
			map_clear_mask (MAP_MASK.F_INFL | MAP_MASK.F_INFL_AIR);
			/* add all hostile units influence */
			foreach (Unit unit in Scenario.units)
				if (unit.killed == 0 && !Player.player_is_ally (Engine.cur_player, unit.player))
					map_add_unit_infl (unit);
			/* visible influence must also be updated */
			map_set_vis_infl_mask ();
		}

		public void map_set_vis_infl_mask ()
		{
			map_clear_mask (MAP_MASK.F_VIS_INFL | MAP_MASK.F_VIS_INFL_AIR);
			/* add all hostile units influence */
			foreach (Unit unit in Scenario.units)
				if (unit.killed == 0 && !Player.player_is_ally (Engine.cur_player, unit.player))
				if (map_mask_tile (unit.x, unit.y).spot)
					map_add_vis_unit_infl (unit);
		}

		/*
        ====================================================================
        Check if unit may air/sea embark/debark at x,y.
        If 'init' != 0, used relaxed rules for deployment
        ====================================================================
        */
		public bool map_check_unit_embark (Unit unit, int x, int y, UnitEmbarkTypes type, bool init)
		{
			int i, nx, ny;
			if (x < 0 || y < 0 || x >= map_w || y >= map_h)
				return false;
			if (type == UnitEmbarkTypes.EMBARK_AIR) {
				if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
					return false;
				if ((unit.sel_prop.flags & UnitFlags.SWIMMING) == UnitFlags.SWIMMING)
					return false;
				if (Engine.cur_player.air_trsp == null)
					return false;
				if (unit.embark != UnitEmbarkTypes.EMBARK_NONE)
					return false;
				if (!init && map [x, y].a_unit == null)
					return false;
				if (unit.player.air_trsp_used >= unit.player.air_trsp_count)
					return false;
				if (!init && !unit.unused)
					return false;
				if (!init && ((map [x, y].terrain.flags [Scenario.cur_weather] & Terrain_flags.SUPPLY_AIR) != Terrain_flags.SUPPLY_AIR))
					return false;
				if (init && ((unit.sel_prop.flags & UnitFlags.PARACHUTE) != UnitFlags.PARACHUTE) && ((map [x, y].terrain.flags [Scenario.cur_weather] & Terrain_flags.SUPPLY_AIR) != Terrain_flags.SUPPLY_AIR))
					return false;
				if (((unit.sel_prop.flags & UnitFlags.AIR_TRSP_OK) != UnitFlags.AIR_TRSP_OK))
					return false;
				if (init && (unit.trsp_prop.flags & UnitFlags.TRANSPORTER) == UnitFlags.TRANSPORTER)
					return false;
				return true;
			}
			if (type == UnitEmbarkTypes.EMBARK_SEA) {
				if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
					return false;
				if ((unit.sel_prop.flags & UnitFlags.SWIMMING) == UnitFlags.SWIMMING)
					return false;
				if (Engine.cur_player.sea_trsp == null)
					return false;
				if (unit.embark != UnitEmbarkTypes.EMBARK_NONE || (!init && unit.sel_prop.mov == 0))
					return false;
				if (!init && map [x, y].g_unit != null)
					return false;
				if (unit.player.sea_trsp_used >= unit.player.sea_trsp_count)
					return false;
				if (!init && !unit.unused)
					return false;
				if (Terrain.GetMovementCost (map [x, y].terrain, unit.player.sea_trsp.mov_type, Scenario.cur_weather) == 0)
					return false;
				/* basically we must be close to an harbor but a town that is just
                   near the water is also okay because else it would be too
                   restrictive. */
				if (!init) {
					if ((map [x, y].terrain.flags [Scenario.cur_weather] & Terrain_flags.SUPPLY_GROUND) == Terrain_flags.SUPPLY_GROUND)
						return true;
					for (i = 0; i < 6; i++)
						if (Misc.get_close_hex_pos (x, y, i, out nx, out ny))
						if ((map [nx, ny].terrain.flags [Scenario.cur_weather] & Terrain_flags.SUPPLY_GROUND) == Terrain_flags.SUPPLY_GROUND)
							return true;
				}
				return init;
			}
			return false;
		}

		public bool map_check_unit_debark (Unit unit, int x, int y, UnitEmbarkTypes type, bool init)
		{
			if (x < 0 || y < 0 || x >= map_w || y >= map_h)
				return false;
			if (type == UnitEmbarkTypes.EMBARK_SEA) {
				if (unit.embark != UnitEmbarkTypes.EMBARK_SEA)
					return false;
				if (!init && map [x, y].g_unit != null)
					return false;
				if (!init && !unit.unused)
					return false;
				if (!init && Terrain.GetMovementCost (map [x, y].terrain, unit.prop.mov_type, Scenario.cur_weather) == 0)
					return false;
				return true;
			}
			if (type == UnitEmbarkTypes.EMBARK_AIR) {
				if (unit.embark != UnitEmbarkTypes.EMBARK_AIR)
					return false;
				if (!init && map [x, y].g_unit != null)
					return false;
				if (!init && !unit.unused)
					return false;
				if (!init && Terrain.GetMovementCost (map [x, y].terrain, unit.prop.mov_type, Scenario.cur_weather) == 0)
					return false;
				if (!init && ((map [x, y].terrain.flags [Scenario.cur_weather] & Terrain_flags.SUPPLY_AIR) != Terrain_flags.SUPPLY_AIR)
                    && ((unit.prop.flags & UnitFlags.PARACHUTE) != UnitFlags.PARACHUTE))
					return false;
				return true;
			}
			return false;
		}

		/*
        ====================================================================
        Embark/debark unit and return if an enemy was spotted.
        If 'enemy_spotted' is 0, don't recalculate spot mask.
        If unit's coordinates or x and y are out of bounds, the respective
        tile is not manipulated.
        ====================================================================
        */
		public void map_embark_unit (Unit unit, int x, int y, int type, out bool enemy_spotted)
		{
			throw new System.NotImplementedException ();
		}

		public void map_debark_unit (Unit unit, int x, int y, int type, out bool enemy_spotted)
		{
			throw new System.NotImplementedException ();
		}

		void map_add_default_deploy_fields (Player player, List<MapCoord> fields)
		{
			int i, j, next_x, next_y;
			bool okay;
			foreach (Unit unit in Scenario.units) {
				if (unit.player == player && unit.SupportsDeploy ()) {
					for (i = 0; i < 6; i++)
						if (Misc.get_close_hex_pos (unit.x, unit.y, i, out next_x, out next_y)) {
							okay = true;
							int x, y;
							for (j = 0; j < 6; j++)
								if (Misc.get_close_hex_pos (next_x, next_y, j, out x, out y))
								if (!mask [x, y].spot ||
                                        (map [x, y].a_unit != null && !Player.player_is_ally (player, map [x, y].a_unit.player)) ||
                                        (map [x, y].g_unit != null && !Player.player_is_ally (player, map [x, y].g_unit.player))) {
									okay = false;
									break;
								}
							if ((map [next_x, next_y].terrain.flags [Scenario.cur_weather] & Terrain_flags.RIVER) == Terrain_flags.RIVER)
								okay = false;
							mask [next_x, next_y].deploy = okay;
						}
				}
			}
			foreach (Unit unit in Scenario.units) {
				/* make sure all units can be re-deployed */
				if (unit.player == player && unit.SupportsDeploy ())
					mask [unit.x, unit.y].deploy = true;
			}
			map_add_deploy_centers_to_deploy_mask (player, null);
			for (short x = 0; x < map_w; x++)
				for (short y = 0; y < map_h; y++)
					if (mask [x, y].deploy)
						fields.Add (new MapCoord (x, y));
		}
		/*
        ====================================================================
        Set deploy mask by player's field list. If first entry is (-1,-1),
        create a default mask, using the initial layout of spotting and 
        units.
        ====================================================================
        */
		void map_set_initial_deploy_mask (Player player)
		{
			int i = Player.player_get_index (player);
			List<MapCoord> field_list;

			if (deploy_fields == null)
				return;
			/*
            list_reset(deploy_fields);
            while ((field_list = list_next(deploy_fields)) && i--) ;
            */
			field_list = deploy_fields [i];
			if (field_list == null)
				return;

			int j = 0;
			while (j < field_list.Count) {
				MapCoord pt = field_list [j];
				Mask_Tile tile = map_mask_tile (pt.x, pt.y);
				if (tile == null) {
					field_list.Remove (pt);
					j--;
					map_add_default_deploy_fields (player, field_list);
				} else {
					tile.deploy = true;
					tile.spot = true;
				}
				j++;
			}
		}


		/*
        ====================================================================
        Check whether (mx, my) can serve as a deploy center for the given
        unit (assuming it is close to it, which is not checked). If no unit
        is given, check whether it is any
        ====================================================================
        */
		bool map_check_deploy_center (Player player, Unit unit, int mx, int my)
		{
			if (map [mx, my].deploy_center != 1 || map [mx, my].player != null)
				return false;
			if (!Player.player_is_ally (map [mx, my].player, player))
				return false;
			if (unit != null) {
				if (Terrain.GetMovementCost (map [mx, my].terrain, unit.sel_prop.mov_type, Scenario.cur_weather) == 0)
					return false;
				if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)
				if ((map [mx, my].terrain.flags [Scenario.cur_weather] & Terrain_flags.SUPPLY_AIR) != Terrain_flags.SUPPLY_AIR)
					return false;
				if (map [mx, my].nation != unit.nation)
					return false;
			}
			return true;
		}


		/*
        ====================================================================
        Add any deploy center and its surrounding to the deploy mask, if it
        can supply 'unit'. If 'unit' is not set, add any deploy center.
        ====================================================================
        */
		void map_add_deploy_centers_to_deploy_mask (Player player, Unit unit)
		{
			int x, y, i, next_x, next_y;
			for (x = 0; x < map_w; x++)
				for (y = 0; y < map_h; y++)
					if (map_check_deploy_center (player, unit, x, y)) {
						mask [x, y].deploy = true;
						for (i = 0; i < 6; i++)
							if (Misc.get_close_hex_pos (x, y, i, out next_x, out next_y))
								mask [next_x, next_y].deploy = true;
					}
		}


		/*
        ====================================================================
        Set the deploy mask for this unit. If 'init', use the initial deploy
        mask (or a default one). If not, set the valid deploy centers. In a
        second run, remove any tile blocked by an own unit if 'unit' is set.
        ====================================================================
        */
		public void map_get_deploy_mask (Player player, Unit unit, bool init)
		{
			int x, y;

			map_clear_mask (MAP_MASK.F_DEPLOY);
			if (init)
				map_set_initial_deploy_mask (player);
			else
				map_add_deploy_centers_to_deploy_mask (player, unit);
			if (unit != null) {
				for (x = 0; x < map_w; x++)
					for (y = 0; y < map_h; y++)
						if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) {
							if (map [x, y].a_unit != null)
								mask [x, y].deploy = false;
						} else {
							if (map [x, y].g_unit != null && (!init || !map_check_unit_embark (unit, x, y, UnitEmbarkTypes.EMBARK_AIR, true)))
								mask [x, y].deploy = false;
						}
			}
		}

		/*
        ====================================================================
        Mark this field being a deployment-field for the given player.
        ====================================================================
        */
		public void map_set_deploy_field (int mx, int my, int player)
		{
			throw new System.NotImplementedException ();
		}

		/*
        ====================================================================
        Check whether this field is a deployment-field for the given player.
        ====================================================================
        */
		public int map_is_deploy_field (int mx, int my, int player)
		{
			throw new System.NotImplementedException ();
		}

		/*
        ====================================================================
        Check if unit may be deployed to mx, my or return undeployable unit
        there. If 'air_mode' is set the air unit is checked first.
        'player' is the index of the player.
        ====================================================================
        */
		public int map_check_deploy (Unit unit, int mx, int my, int player, int init, int air_mode)
		{
			throw new NotImplementedException ();

		}

		public Unit map_get_undeploy_unit (int x, int y, bool air_region)
		{
			if (air_region) {
				/* check air */
				if (map [x, y].a_unit != null && map [x, y].a_unit.fresh_deploy)
					return map [x, y].a_unit;
				else
                    /*if ( map[x, y].g_unit && map[x, y].g_unit.fresh_deploy )
                        return  map[x, y].g_unit;
                    else*/
					return null;
			} else {
				/* check ground */
				if (map [x, y].g_unit != null && map [x, y].g_unit.fresh_deploy)
					return map [x, y].g_unit;
				else
                    /*if ( map[x, y].a_unit &&  map[x, y].a_unit.fresh_deploy )
                        return  map[x, y].a_unit;
                    else*/
					return null;
			}
		}


		/*
        ====================================================================
        Check the supply level of tile (mx, my) in the context of 'unit'.
        (hex tiles with SUPPLY_GROUND have 100% supply)
        ====================================================================
        */
		public int map_get_unit_supply_level (int mx, int my, Unit unit)
		{
			int x, y, w, h, i, j;
			int flag_supply_level, supply_level;
			/* flying and swimming units get a 100% supply if near an airfield or a harbour */
			if (((unit.sel_prop.flags & UnitFlags.SWIMMING) == UnitFlags.SWIMMING)
                 || ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING)) {
				supply_level = map_supplied_by_depot (mx, my, unit) * 100;
			} else {
				/* ground units get a 100% close to a flag and looses about 10% for each title it gets away */
				/* test all flags within a region x-10,y-10,20,20 about their distance */
				/* get region first */
				x = mx - 10;
				y = my - 10;
				w = 20;
				h = 20;
				if (x < 0) {
					w += x;
					x = 0;
				}
				if (y < 0) {
					h += y;
					y = 0;
				}
				if (x + w > map_w)
					w = map_w - x;
				if (y + h > map_h)
					h = map_h - y;
				/* now check flags */
				supply_level = 0;
				for (i = x; i < x + w; i++)
					for (j = y; j < y + h; j++)
						if (map [i, j].player != null && Player.player_is_ally (unit.player, map [i, j].player)) {
							flag_supply_level = Misc.get_dist (mx, my, i, j);
							if (flag_supply_level < 2)
								flag_supply_level = 100;
							else {
								flag_supply_level = 100 - (flag_supply_level - 1) * 10;
								if (flag_supply_level < 0)
									flag_supply_level = 0;
							}
							if (flag_supply_level > supply_level)
								supply_level = flag_supply_level;
						}
			}
			/* air: if hostile influence is 1 supply is 50%, if influence >1 supply is not possible */
			/* ground: if hostile influence is 1 supply is at 75%, if influence >1 supply is at 50% */
			if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) {
				if (mask [mx, my].air_infl > 1 || mask [mx, my].infl > 1)
					supply_level = 0;
				else if (mask [mx, my].air_infl == 1 || mask [mx, my].infl == 1)
					supply_level = supply_level / 2;
			} else {
				if (mask [mx, my].infl == 1)
					supply_level = 3 * supply_level / 4;
				else if (mask [mx, my].infl > 1)
					supply_level = supply_level / 2;
			}
			return supply_level;
		}
		/*
        ====================================================================
        Check if this map tile is a supply point for the given unit.
        ====================================================================
        */
		public bool map_is_allied_depot (Map_Tile tile, Unit unit)
		{
			if (tile == null)
				return false;
			/* maybe it's an aircraft carrier */
			if (tile.g_unit != null)
			if ((tile.g_unit.sel_prop.flags & UnitFlags.CARRIER) == UnitFlags.CARRIER)
			if (Player.player_is_ally (tile.g_unit.player, unit.player))
			if ((unit.sel_prop.flags & UnitFlags.CARRIER_OK) == UnitFlags.CARRIER_OK)
				return true;
			/* check for depot */
			if (tile.player == null)
				return false;
			if (!Player.player_is_ally (unit.player, tile.player))
				return false;
			if ((unit.sel_prop.flags & UnitFlags.FLYING) == UnitFlags.FLYING) {
				if ((tile.terrain.flags [Scenario.cur_weather] & Terrain_flags.SUPPLY_AIR) != Terrain_flags.SUPPLY_AIR)
					return false;
			} else if ((unit.sel_prop.flags & UnitFlags.SWIMMING) == UnitFlags.SWIMMING) {
				if ((tile.terrain.flags [Scenario.cur_weather] & Terrain_flags.SUPPLY_SHIPS) != Terrain_flags.SUPPLY_SHIPS)
					return false;
			}
			return true;
		}

		/*
        ====================================================================
        Checks whether this hex (mx, my) is supplied by a depot in the
        context of 'unit'.
        ====================================================================
        */
		public int map_supplied_by_depot (int mx, int my, Unit unit)
		{
			int i;
			if (map_is_allied_depot (map [mx, my], unit))
				return 1;
			for (i = 0; i < 6; i++)
				if (map_is_allied_depot (map_get_close_hex (mx, my, i), unit))
					return 1;
			return 0;
		}

		/*
        ====================================================================
        Get drop zone for unit (all close hexes that are free).
        ====================================================================
        */
		public void map_get_dropzone_mask (Unit unit)
		{
			int i, x, y;
			map_clear_mask (MAP_MASK.F_DEPLOY);
			for (i = 0; i < 6; i++)
				if (Misc.get_close_hex_pos (unit.x, unit.y, i, out x, out y))
				if (map [x, y].g_unit == null)
				if (Terrain.GetMovementCost (map [x, y].terrain, unit.prop.mov_type, Scenario.cur_weather) != 0)
					mask [x, y].deploy = true;
			if (map [unit.x, unit.y].g_unit == null)
			if (Terrain.GetMovementCost (map [unit.x, unit.y].terrain, unit.prop.mov_type, Scenario.cur_weather) != 0)
				mask [unit.x, unit.y].deploy = true;
		}

		/*
        ====================================================================
        Check if units are close to each other. This means on neighbored
        hex tiles.
        ====================================================================
        */
		public bool unit_is_close (Unit unit, Unit target)
		{
			return Misc.is_close (unit.x, unit.y, target.x, target.y);
		}
		
		private void draw_unit_on_texture (SDL_Surface hexTex, Unit unit, bool resize)
		{
			string name = Unit.DeleteOrdinal (unit.name);
			Unit_Lib_Entry ulib = DB.UnitLib.unit_lib_find_by_name (name);
			int ntex = TextureTable.elegirImgUnit (ulib);
			SDL_Surface sdl = SDL_Surface.LoadSurface (ulib.icon_img_name + 1, false);
			SDL_Surface dest = new SDL_Surface ();
			if (ntex == 1) {
				int offset = (sdl.h - ulib.offset_img - ulib.icon_h);
				SDL_Surface.copy_image (dest, ulib.icon_w, ulib.icon_h, sdl, 0, offset);
				SDL_Surface.putPixelBlack (dest);
				if (unit.player.ctrl == PLAYERCONTROL.PLAYER_CTRL_CPU) {
					SDL_Surface aux = new SDL_Surface ();
					SDL_Surface.copy_image180 (aux, dest);
					dest = aux;
				}
				if (dest.w > 51) {
					SDL_Surface aux = new SDL_Surface ();
					aux.bitmap = new Texture2D (ulib.icon_tiny_w, ulib.icon_tiny_h, TextureFormat.RGB24, false);
					float scale = 1.5f;
					for (int i=0; i<ulib.icon_tiny_w; i++) {
						for (int j=0; j<ulib.icon_tiny_h; j++) {
							int x = (int)(scale * i);
							int y = (int)(scale * j);
							aux.bitmap.SetPixel (i, j, dest.bitmap.GetPixel (x, y));
						}
					}
					aux.bitmap.Apply ();
					aux.w = ulib.icon_tiny_w;
					aux.h = ulib.icon_tiny_h;
					aux.bitmapMaterial = new Material (Shader.Find ("Diffuse"));
					aux.bitmapMaterial.mainTexture = aux.bitmap;
					dest = aux;
				}
				int xdest = (Config.hex_w - dest.w) / 2;
				SDL_Surface.copy_image_without_key (hexTex, dest, xdest + 2, Nation.nation_flag_height + 5, Color.black);
			} else if (ntex == 2) {
				int offset = ulib.offset_img + ulib.icon_h;
				offset = offset - sdl.h;
				SDL_Surface sdl2 = SDL_Surface.LoadSurface (ulib.icon_img_name + 2, false);
				offset = sdl2.h - offset;
				SDL_Surface.copy_image (dest, ulib.icon_w, ulib.icon_h, sdl2, 0, offset);
				SDL_Surface.putPixelBlack (dest);
				if (unit.player.ctrl == PLAYERCONTROL.PLAYER_CTRL_CPU) {
					SDL_Surface aux = new SDL_Surface ();
					SDL_Surface.copy_image180 (aux, dest);
					dest = aux;
				}
				if (resize) {
					SDL_Surface aux = new SDL_Surface ();
					aux.bitmap = new Texture2D (ulib.icon_tiny_w, ulib.icon_tiny_h, TextureFormat.RGB24, false);
					float scale = 1.5f;
					for (int i=0; i<ulib.icon_tiny_w; i++) {
						for (int j=0; j<ulib.icon_tiny_h; j++) {
							int x = (int)(scale * i);
							int y = (int)(scale * j);
							aux.bitmap.SetPixel (i, j, dest.bitmap.GetPixel (x, y));
						}
					}
					aux.bitmap.Apply ();
					aux.w = ulib.icon_tiny_w;
					aux.h = ulib.icon_tiny_h;
					aux.bitmapMaterial = new Material (Shader.Find ("Diffuse"));
					aux.bitmapMaterial.mainTexture = aux.bitmap;
					dest = aux;
				}
				int xdest = (Config.hex_w - dest.w) / 2;
				int ydest = (resize)? Nation.nation_flag_height + 1: Nation.nation_flag_height + 5;
				SDL_Surface.copy_image_without_key (hexTex, dest, xdest + 2, ydest, Color.black);
			}
		}

	}
}
