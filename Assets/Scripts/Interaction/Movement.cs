using UnityEngine;
using System.Collections;
using EngineApp;
using Miscellaneous;
using AI_Enemy;
using DataFile;
using System.Text;
using System.Threading;

public class Movement : MonoBehaviour
{
	
	private Ray ray;
	private RaycastHit hit;
	private RaycastHit hitSelected;
	private int width;
	private int height;
	private static int widthInfo = 250;
	private int windowType = 0;

	public static int WidthInfo {
		get{ return widthInfo;}
	}

	private int x, y;
	private bool first = true;
	private void OnClick ()
	{
		if (Input.GetMouseButtonDown (0) && Engine.map.isLoaded) {//TODO_RR if (Input.GetTouch(0).phase == TouchPhase.Began)){
			int mapx, mapy;
			Engine.REGION region;
			Vector3 clickedPosition = Camera.main.ScreenToWorldPoint (Input.mousePosition);
			if (Engine.engine_get_map_pos (hitSelected.transform.position.x, 
										   hitSelected.transform.position.z, out mapx, 
										   out mapy, out region, clickedPosition.z)) {
				if (Engine.cur_unit != null) {
					/* handle current unit */
					if (!(Engine.cur_unit.x == mapx && Engine.cur_unit.y == mapy &&
                            Engine.engine_get_prim_unit (mapx, mapy, region) == Engine.cur_unit)) {
						Unit unit = Engine.engine_get_target (mapx, mapy, region);
						if (unit != null) {
							//TODO_RR Action.action_queue_attack(Engine.cur_unit, unit);
							print ("Attack");
						} else {
							if (Engine.map.mask [mapx, mapy].in_range != 0 && !Engine.map.mask [mapx, mapy].blocked) {
								/*if (first){
									Draw ();
									Thread.Sleep(20);
									first = false;
								}*/
									Action.action_queue_move (Engine.cur_unit, mapx, mapy);						
							} else {
								if (Engine.map.mask [mapx, mapy].sea_embark) {
									if (Engine.cur_unit.embark == UnitEmbarkTypes.EMBARK_NONE)
										Action.action_queue_embark_sea (Engine.cur_unit, mapx, mapy);
									else
										Action.action_queue_debark_sea (Engine.cur_unit, mapx, mapy);
									Engine.engine_backup_move (Engine.cur_unit, mapx, mapy);
									Engine.draw_map = true;
									
								} else {
									unit = Engine.engine_get_select_unit (mapx, mapy, region);
									if (unit != null && Engine.cur_unit != unit) {
										if (Engine.cur_ctrl == PLAYERCONTROL.PLAYER_CTRL_HUMAN) {
											if (Engine.engine_capture_flag (Engine.cur_unit)) { 
												/* CHECK IF SCENARIO IS FINISHED */
												if (Scenario.scen_check_result (false)) {
													Engine.engine_finish_scenario ();
													return;
												}              
											}
										}
										Engine.engine_select_unit (unit);
										Engine.engine_clear_backup ();
										Engine.engine_update_info (mapx, mapy, region);
										Engine.draw_map = true;
									}
								}
							}
						}
					}
				} else {
					Unit unit = Engine.engine_get_select_unit (mapx, mapy, region);
					if (unit != null && Engine.cur_unit != unit) {
						/* select unit */
						Engine.engine_select_unit (unit);
						Engine.engine_update_info (mapx, mapy, region);
						Engine.draw_map = true;
#if WITH_SOUND
						wav_play( terrain_icons.wav_select );
#endif
					}
				}
				
			}
		}
		if (Engine.draw_map) {
			Draw ();
		}
	}
	
	private void OnMove ()
	{
		Color hover = Color.yellow;
		Color fogHover = new Color (0.3f, 0.3f, 0.3f, 1);
		int xS, yS;
		Engine.REGION aux_region;
		ray = Camera.main.ScreenPointToRay (Input.mousePosition); //ray = Camera.main.ScreenPointToRay(touch.position);
		if (Physics.Raycast (ray, out hit, 100)) {
			Engine.engine_get_map_pos(hit.transform.position.x,
									  hit.transform.position.z,out x,out y,
									  out aux_region,Input.mousePosition.z);
			if (!Engine.map.mask [x, y].fog) {
				hit.transform.renderer.material.color = hover;
				if (hitSelected.transform != null) {
					Engine.engine_get_map_pos (hitSelected.transform.position.x, 
										   hitSelected.transform.position.z,
										   out xS, out yS, out aux_region, Input.mousePosition.z);
					if (Engine.cur_unit != null && Engine.cur_unit.x == xS && Engine.cur_unit.y == yS) {
						hitSelected.transform.renderer.material.color = Color.green;
					} else if (xS == x && yS == y) {
						hitSelected.transform.renderer.material.color = hover;
					} else if (!Engine.map.mask [xS, yS].fog) {
						hitSelected.transform.renderer.material.color = Color.white;
					} else {
						hitSelected.transform.renderer.material.color = new Color (0.7f, 0.7f, 0.7f, 1);
						;
					}
					
				}
				hitSelected = hit;
			} else {
				hit.transform.renderer.material.color = fogHover;
				if (hitSelected.transform != null) {
					Engine.engine_get_map_pos (hitSelected.transform.position.x, 
											   hitSelected.transform.position.z, out xS, out yS,
											   out aux_region,Input.mousePosition.z);
					if (Engine.cur_unit != null && Engine.cur_unit.x == xS && Engine.cur_unit.y == yS) {
						hitSelected.transform.renderer.material.color = Color.green;
					} else if (xS == x && yS == y) {
						hitSelected.transform.renderer.material.color = fogHover;
					} else if (!Engine.map.mask [xS, yS].fog) {
						hitSelected.transform.renderer.material.color = Color.white;
					} else {
						hitSelected.transform.renderer.material.color = new Color (0.7f, 0.7f, 0.7f, 1);
						;
					}
				}
				hitSelected = hit;
			}
		}
	}
	
	public static void Draw ()
	{
		if (Engine.map.isLoaded) {
			Engine.status = STATUS.STATUS_NONE;
			bool use_frame = (Engine.cur_ctrl != PLAYERCONTROL.PLAYER_CTRL_CPU);
			GUIMap.Repaint(Engine.map,use_frame);
		}
		Engine.draw_map = false;
	}
	
	// Update is called once per frame
	void Update ()
	{
		OnMove ();
		OnClick ();
	}
	
	void OnGUI ()
	{
		if (Engine.map.isLoaded) {
			GUI.Window (windowType, new Rect (Screen.width - widthInfo, 0, widthInfo, Screen.height), ConfigWindow, "");
		}
	}
	
	void ConfigWindow (int windowID)
	{
		switch (windowID) {
		case 0:
			GeneralConfig ();
			break;
		case 1:
			DeployUnitsGUI ();
			break;
		case 2:
			VictCondGUI ();
			break;
		case 3:
			EndTurnGUI ();
			break;
		case 4:
			if (Engine.cur_unit!=null)
				ShowCUnitInfoGUI ();
			break;
		}		
		
	}
	
	private bool isVisibleUnit (int x, int y)
	{
		foreach (Unit unit in Scenario.vis_units) {
			if (unit.x == x && unit.y == y)
				return true;
		}
		return false;
	}
	
	private void GeneralConfig ()
	{
		/*string movement = "Movement: ";
		string spotting = "Spotting: ";
		string range = "Range: ";
		string initiative = "Initiative: ";
		string softat = "Soft At.: ";*/
		string turns = "Turn " + Scenario.turn + " of " + Scenario.scen_info.turn_limit;
		string prestige = "Prestige: " + Player.players_get_first ().prestige;
		string weather = "Weather:  " +
                    ((Scenario.turn < Scenario.scen_info.turn_limit) ?
                     Engine.terrain.weatherTypes [Scenario.scen_get_weather ()].name : "");
		string forecast = "Forecast: " +
                    ((Scenario.turn + 1 < Scenario.scen_info.turn_limit) ?
                     Engine.terrain.weatherTypes [Scenario.scen_get_forecast ()].name : "");
		GUI.Label (new Rect (83, 25, 75, 25), turns);
		GUI.Label (new Rect (83, 50, 100, 25), prestige);
		GUI.Label (new Rect (83, 75, 100, 25), weather);
		GUI.Label (new Rect (83, 100, 100, 25), forecast);
		if (GUI.Button (new Rect (15, 125, 90, 25), "Supply Units")) {
			SupplyUnitsGUI ();
		}
		if (GUI.Button (new Rect (145, 125, 90, 25), "Deploy Units")) {
			windowType = 1;
		}
		if (GUI.Button (new Rect (15, 155, 90, 25), "Vict. Cond")) {
			windowType = 2;
		}
		if (GUI.Button (new Rect (145, 155, 90, 25), "End Turn")) {
			windowType = 3;
		}
		string current_info = ""; 
		//Unit_Lib_Entry unit_info = null;
		if (Engine.cur_unit != null) {
			current_info = Engine.cur_unit.name + "\nFuel:" +
						   Engine.cur_unit.cur_fuel + " Ammo:" +
						   Engine.cur_unit.cur_ammo + " Ent:" + Engine.cur_unit.entr;
			/*string name = Unit.DeleteOrdinal(Engine.cur_unit.name);
			unit_info = DB.UnitLib.unit_lib_find_by_name(name);
			movement+=unit_info.mov+"("+DB.UnitLib.mov_types[unit_info.mov_type].name+")";
			spotting+=unit_info.spt;
			range+=unit_info.rng;
			initiative+=unit_info.ini;
			softat+=unit_info.atks[0];*/
		}
		if (GUI.Button (new Rect (62.5f, 185, 125, 25), "Show C. Unit Info")) {
			windowType = 4;
		}
		GUI.Box (new Rect (37.5f, 215, 175, 40), current_info);
		string hover_info = "";
		string terrain_info = "";
		if (hitSelected.transform != null) {
			terrain_info = Engine.map.map [x, y].name + "(" + x + "," + y + ")";
			if (Engine.map.map [x, y].a_unit != null && isVisibleUnit (x, y)) {
				hover_info = Engine.map.map [x, y].a_unit.name + "\nFuel:" +
						   	 Engine.map.map [x, y].a_unit.cur_fuel + " Ammo:" +
						   	 Engine.map.map [x, y].a_unit.cur_ammo + " Ent:" + Engine.map.map [x, y].a_unit.entr;
				/*Unit_Lib_Entry unit_info_hover = DB.UnitLib.unit_lib_find_by_name(Unit.DeleteOrdinal(Engine.map.map[x,y].a_unit.name));
				movement = getMovOfUnitHover(unit_info_hover,movement);
				spotting = getSpotOfUnitHover(unit_info_hover, spotting);
				range = getRangeOfUnitHover(unit_info_hover, range);
				initiative = getIniOfUnitHover(unit_info_hover, initiative);*/
				
			}
			if (Engine.map.map [x, y].g_unit != null && isVisibleUnit (x, y)) {
				hover_info = Engine.map.map [x, y].g_unit.name + "\nFuel:" +
						   	 Engine.map.map [x, y].g_unit.cur_fuel + " Ammo:" +
						   	 Engine.map.map [x, y].g_unit.cur_ammo + " Ent:" + Engine.map.map [x, y].g_unit.entr;
				/*Unit_Lib_Entry unit_info_hover = DB.UnitLib.unit_lib_find_by_name(Unit.DeleteOrdinal(Engine.map.map[x,y].g_unit.name));
				movement = getMovOfUnitHover(unit_info_hover,movement);
				spotting = getSpotOfUnitHover(unit_info_hover, spotting);
				range = getRangeOfUnitHover(unit_info_hover, range);
				initiative = getIniOfUnitHover(unit_info_hover, initiative);*/
			}
		}
		GUI.Box (new Rect (37.5f, 260, 175, 40), hover_info);
		GUI.Label (new Rect (83, 300, 200, 20), terrain_info);
		GUI.Label (new Rect (15, 320, 200, 20), "Expected losses");
		/*GUI.Label(new Rect(25,290,235,20),movement);
		GUI.Label(new Rect(25,310,235,20),spotting);
		GUI.Label(new Rect(25,330,235,20),range);
		GUI.Label(new Rect(25,350,235,20),initiative);
		GUI.Label(new Rect(25,370,235,20),softat);*/
	}
	
	private void SupplyUnitsGUI ()
	{
		if (Engine.map.isLoaded && Engine.cur_unit != null) {
			int ammo, fuel;
			if (Engine.cur_unit.CheckSupply (Unit.UNIT_SUPPLY.UNIT_SUPPLY_ANYTHING, out ammo, out fuel))
				Engine.cur_unit.Supply (Unit.UNIT_SUPPLY.UNIT_SUPPLY_ALL);
			Engine.engine_select_unit (Engine.cur_unit);
			Engine.draw_map = true;
		}
	}
	
	private void DeployUnitsGUI ()
	{
		if (GUILayout.Button ("OK"))
			windowType = 0;
	}
	
	private void VictCondGUI ()
	{
		string info = Engine.gui_show_conds ();
		GUILayout.Box (info);
		if (GUILayout.Button ("OK"))
			windowType = 0;
	}
	
	private void EndTurnGUI ()
	{
		GUILayout.Label("Do you really want to end your turn?");
		GUILayout.Label("End Your Turn #" + Scenario.turn, GUILayout.ExpandWidth(true));
		GUILayout.BeginHorizontal();
		if (GUILayout.Button ("YES")){
			Engine.engine_end_turn();
			if (!Engine.end_scen)
            	Engine.engine_begin_turn(null, false);
			if (Engine.draw_map)
            {
                    Draw();
            }
			while (Engine.cur_player.ctrl != PLAYERCONTROL.PLAYER_CTRL_HUMAN)
            {
            	Engine.engine_end_turn();
                if (!Engine.end_scen)
                	Engine.engine_begin_turn(null, false);
                if (Engine.draw_map)
                {
                        Draw();
                }
            }
			Scenario.turn+=1;
			windowType = 0;
		}
		if (GUILayout.Button ("NO")){
			windowType = 0;
		}	
		GUILayout.EndHorizontal();
	}
	
	private void ShowCUnitInfoGUI ()
	{
		if (Engine.map.isLoaded && Engine.cur_unit != null) {
			Unit unit = Engine.cur_unit;
			StringBuilder str = new StringBuilder ();
			str.Append ("Unit Name:\t" + unit.name);
			str.Append ("\nClass:\t\t" + DB.UnitLib.unit_classes [unit.prop.unit_class].name);
			str.Append ("\nMovement:\t" + DB.UnitLib.mov_types [unit.prop.mov_type].name);
			str.Append ("\nTarget:\t\t" + DB.UnitLib.trgt_types [unit.prop.trgt_type].name);
			/* ammo, fuel, spot, mov, ini, range */
			if (unit.prop.ammo == 0)
				str.Append ("\nAmmo:\t\tN.A.");
			else if (Engine.cur_player == null || Player.player_is_ally (Engine.cur_player, unit.player))
				str.Append ("\nAmmo:\t\t" + unit.cur_ammo + ", " + unit.prop.ammo);
			else
				str.Append ("\nAmmo:\t\t" + unit.prop.ammo);
			if (unit.prop.fuel == 0)
				str.Append ("\nFuel:\t\tN.A.");
			else if (Engine.cur_player == null || Player.player_is_ally (Engine.cur_player, unit.player))
				str.Append ("\nFuel:\t\t" + unit.cur_fuel + ", " + unit.prop.fuel);
			else
				str.Append ("\nFuel:\t\t" + unit.prop.fuel);
			str.Append ("\nSpotting:\t\t" + unit.prop.spt);
			str.Append ("\nMovement:\t" + unit.prop.mov);
			str.Append ("\nInitiative:\t\t" + unit.prop.ini);
			str.Append ("\nRange:\t\t" + unit.prop.rng);
			str.Append ("\nExperience:\t" + unit.exp);
			str.Append ("\nEntrenchment:\t" + unit.entr);
			/* attack/defense */
			for (int i = 0; i < DB.UnitLib.trgt_type_count; i++) {
				if (unit.prop.atks [i] < 0)
					str.Append ("\n" + DB.UnitLib.trgt_types [i].name + " Attack:\t" + -unit.prop.atks [i]);
				else
					str.Append ("\n" + DB.UnitLib.trgt_types [i].name + " Attack:\t" + unit.prop.atks [i]);
			}
			str.Append ("\nGround Defense:\t" + unit.prop.def_grnd);
			str.Append ("\nAir Defense:\t" + unit.prop.def_air);
			str.Append ("\nClose Defense:\t" + unit.prop.def_cls);
			str.Append ("\nSuppression:\t" + unit.turn_suppr);

			/* transporter */
			if (unit.trsp_prop != null) {
				/* icon */
				/* name & type */
				str.Append ("\nTransporter Name:\t\t" + unit.trsp_prop.name);
				str.Append ("\nTrans. Class:\t\t" + DB.UnitLib.unit_classes [unit.trsp_prop.unit_class].name);
				str.Append ("\nTrans. Movement:\t" + DB.UnitLib.mov_types [unit.trsp_prop.mov_type].name);
				str.Append ("\nTrans. Target:\t" + DB.UnitLib.trgt_types [unit.trsp_prop.trgt_type].name);
				/* spt, mov, ini, rng */
				str.Append ("\nTrans. Spotting:\t" + unit.trsp_prop.spt);
				str.Append ("\nTrans. Movement:\t" + unit.trsp_prop.mov);
				str.Append ("\nTrans. Initiative:\t" + unit.trsp_prop.ini);
				str.Append ("\nTrans. Range::\t" + unit.trsp_prop.rng);
				/* attack & defense */
				for (int i = 0; i < DB.UnitLib.trgt_type_count; i++) {
					str.Append ("\n " + DB.UnitLib.trgt_types [i].name + " Trans. Attack:\t" + unit.trsp_prop.atks [i]);
				}
				str.Append ("\nTrans. Ground Defense:\t" + unit.trsp_prop.def_grnd);
				str.Append ("\nTrans. Air Defense:\t" + unit.trsp_prop.def_air);
				str.Append ("\nTrans. Close Defense:\t" + unit.trsp_prop.def_cls);
			}
			/* show */
			GUILayout.Box (str.ToString ());
		}
		if (GUILayout.Button ("OK"))
			windowType = 0;
	}
	/*private string getMovOfUnitHover(Unit_Lib_Entry unit_info_hover, string movement){
		if (movement.Equals("Movement: ")){
			return movement+="\t\t\t\t"+unit_info_hover.mov+"("+DB.UnitLib.mov_types[unit_info_hover.mov_type].name+")";
		}
		else{
			return movement+="\t"+unit_info_hover.mov+"("+DB.UnitLib.mov_types[unit_info_hover.mov_type].name+")";
		}
	}
	private string getSpotOfUnitHover(Unit_Lib_Entry unit_info_hover, string spotting){
		if (spotting.Equals("Spotting: ")){
			return spotting+="\t\t\t\t\t"+unit_info_hover.spt;
		}
		else{
			return spotting+="\t\t\t\t\t\t"+unit_info_hover.spt;
		}
	}
	
	private string getRangeOfUnitHover(Unit_Lib_Entry unit_info_hover, string spotting){
		if (spotting.Equals("Range: ")){
			return spotting+="\t\t\t\t\t\t"+unit_info_hover.rng;
		}
		else{
			return spotting+="\t\t\t\t\t\t"+unit_info_hover.rng;
		}
	}
	
	private string getIniOfUnitHover(Unit_Lib_Entry unit_info_hover, string spotting){
		if (spotting.Equals("Initiative: ")){
			return spotting+="\t\t\t\t\t"+unit_info_hover.rng;
		}
		else{
			return spotting+="\t\t\t\t\t"+unit_info_hover.rng;
		}
	}*/
}
